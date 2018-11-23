using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;

[RequireComponent (typeof(ParticleSystem))]
public class WaterSystem : MonoBehaviour {
	[Range(1f, 10f)]
	public float restDensity = 3f;

	[Range(0f, 500f)]
	public float pressureConstant = 250f;

	[Range(0f, 10f)]
	public float viscosity = 1f;

	[Range(0f, 10f)]
	public float kinematicViscosity = 10f;

	[Range(0.05f, 1f)]
	public float radius = 1f;

	public float skippingFrames = 0f;
	public const float H = 1f;
	public const float H2 = H * H;
	public const float H3 = H * H2;
	public const float H6 = H3 * H3;
	public const float H9 = H3 * H6;
	public const float Wpoly6 = 315f / (64f * Mathf.PI * H9);
	public const float gradientWspiky = -45 / (Mathf.PI * H6);
	public const float laplacianWviscosity = 45 / (Mathf.PI * H6);
	public const float DT = 0.016f;
	public const int MaxKinematicParticles = 1000;

	private List<Vector3> particlesToEmit = new List<Vector3>();
	private int skippedFrames = 0;
	private ParticleSystem waterSystem;
	private ParticleSystem.Particle[] particles;

	private int numParticlesAlive;
	private int numWaterParticlesAlive;
	private NativeArray<float2> positions;
	private NativeArray<float2> velocities;
	private NativeMultiHashMap<int, int> hashMap;
	private NativeArray<float> densities;
	private NativeArray<float> pressures;
	private NativeArray<float2> forces;
	private int numKinematicParticlesAlive;
	private NativeArray<float2> kinematicPositions;
	private NativeArray<float2> kinematicVelocities;
	private NativeMultiHashMap<int, int> kinematicHashMap;

	private JobHandle jobHandle;
	void Start () {
		Debug.Log("Wpoly6 " + Wpoly6);
		Debug.Log("gradientWspiky " + gradientWspiky);
		Debug.Log("laplacianWviscosity " + laplacianWviscosity);
		waterSystem = GetComponent<ParticleSystem>();
        particles = new ParticleSystem.Particle[waterSystem.main.maxParticles];
		numParticlesAlive = 0;

		numWaterParticlesAlive = 0;
		positions = new NativeArray<float2>(waterSystem.main.maxParticles, Allocator.Persistent);
		velocities = new NativeArray<float2>(waterSystem.main.maxParticles, Allocator.Persistent);
		hashMap = new NativeMultiHashMap<int, int>(waterSystem.main.maxParticles, Allocator.Persistent);
		densities = new NativeArray<float>(waterSystem.main.maxParticles, Allocator.Persistent);
		pressures = new NativeArray<float>(waterSystem.main.maxParticles, Allocator.Persistent);
		forces = new NativeArray<float2>(waterSystem.main.maxParticles, Allocator.Persistent);

		numKinematicParticlesAlive = 0;
		kinematicPositions = new NativeArray<float2>(MaxKinematicParticles, Allocator.Persistent);
		kinematicVelocities = new NativeArray<float2>(MaxKinematicParticles, Allocator.Persistent);
		kinematicHashMap = new NativeMultiHashMap<int, int>(MaxKinematicParticles, Allocator.Persistent);
	}
	
	[BurstCompile]
	struct ApplyMultiplier : IJobParallelFor
	{
		[ReadOnly] public float multiplier;
		public NativeArray<float2> positions;
		public NativeArray<float2> velocities;

		public void Execute(int index)
		{
			positions[index] = positions[index] * multiplier;
			velocities[index] = velocities[index] * multiplier;
		}
	}

	[BurstCompile]
	struct DeapplyMultiplier : IJobParallelFor
	{
		[ReadOnly] public float multiplier;
		public NativeArray<float2> positions;
		public NativeArray<float2> velocities;

		public void Execute(int index)
		{
			positions[index] = positions[index] / multiplier;
			velocities[index] = velocities[index] / multiplier;
		}
	}

	[BurstCompile]
	struct HashPositions : IJobParallelFor
	{
		[ReadOnly] public NativeArray<float2> positions;
		[WriteOnly] public NativeMultiHashMap<int, int>.Concurrent hashMap;

		public void Execute(int index)
		{
			int hash = Hash(positions[index], H);
			hashMap.Add(hash, index);
		}
	}

	[BurstCompile]
	struct CalculatePressure : IJobParallelFor
	{
		[ReadOnly] public float restDensity;
		[ReadOnly] public float pressureConstant;
		[ReadOnly] public NativeArray<float2> positions;
		[ReadOnly] public NativeMultiHashMap<int, int> hashMap;
		public NativeArray<float> densities;
		public NativeArray<float> pressures;

		public void Execute(int index)
		{
			densities[index] = 0;
			pressures[index] = 0;

			float2 position = positions[index];
			int otherIndex;
            var iterator = new NativeMultiHashMapIterator<int>();
			int hash;
			for (int i = -1; i < 2; ++i) {
				for (int j = -1; j < 2; ++j) {
					hash = Hash(position + new float2(i * H, j * H), H);
					if (hashMap.TryGetFirstValue(hash, out otherIndex, out iterator)) {
						do {
							Calculate(index, otherIndex, position);
						} while (hashMap.TryGetNextValue(out otherIndex, ref iterator));
					}
				}
			}
			densities[index] = math.max(restDensity, densities[index]);
			//  k * (density-p – rest-density)
			//pressures[index] = pressureConstant * (densities[index] - restDensity);
			float p = densities[index] / restDensity;
			pressures[index] = pressureConstant * (p * p - 1);
		}

		private void Calculate(int index, int otherIndex, float2 position)
		{
			float2 r = positions[otherIndex] - position;
			float r2 = math.lengthsq(r);
			if (r2 < H2) {
				// add mass-n * W_poly6(r, h) to density of particle
				// mass = 1
				densities[index] += Wpoly6 * math.pow(H2 - r2, 3);
			}
		}
	}

	[BurstCompile]
	struct CalculateForce : IJobParallelFor
	{
		[ReadOnly] public float viscosity;
		[ReadOnly] public NativeArray<float2> positions;
		[ReadOnly] public NativeArray<float2> velocities;
		[ReadOnly] public NativeArray<float> densities;
		[ReadOnly] public NativeArray<float> pressures;
		[ReadOnly] public NativeMultiHashMap<int, int> hashMap;
		[ReadOnly] public float kinematicViscosity;
		[ReadOnly] public NativeArray<float2> kinematicPositions;
		[ReadOnly] public NativeArray<float2> kinematicVelocities;
		[ReadOnly] public NativeMultiHashMap<int, int> kinematicHashMap;
		public NativeArray<float2> forces;

		public void Execute(int index)
		{
			forces[index] = float2.zero;

			float2 position = positions[index];
			int otherIndex;
            var iterator = new NativeMultiHashMapIterator<int>();
			int hash;
			float density_p = densities[index];
			float pressure_p = pressures[index];
			for (int i = -1; i < 2; ++i) {
				for (int j = -1; j < 2; ++j) {
					hash = Hash(position + new float2(i * H, j * H), H);
					if (hashMap.TryGetFirstValue(hash, out otherIndex, out iterator)) {
						do {
							Calculate(index, otherIndex, position, pressure_p);
						} while (hashMap.TryGetNextValue(out otherIndex, ref iterator));
					}
					if (kinematicHashMap.TryGetFirstValue(hash, out otherIndex, out iterator)) {
						do {
							CalculateKinematic(index, otherIndex, position, density_p);
						} while (kinematicHashMap.TryGetNextValue(out otherIndex, ref iterator));
					}
				}
			}
			forces[index] = forces[index] / density_p;
		}

		private void Calculate(int index, int otherIndex, float2 position, float pressure_p)
		{
			if (index == otherIndex) {
				return;
			}
			float2 _r = positions[otherIndex] - position;
			float r = math.max(0.001f, math.length(_r));
			if (r < H) {
				float density_n = densities[otherIndex];
				float pressure_n = pressures[otherIndex];

				//add mass-n * (pressure-p + pressure-n) / (2 * density-n) * gradient-W-spiky(r, h) to pressure-force of particle
				// mass = 1
				forces[index] += (_r / r) * ((pressure_p + pressure_n) / (2 * density_n) * gradientWspiky * math.pow(H - r, 2));

				float2 _v = velocities[otherIndex] - velocities[index];

				//add viscosity * mass-n * (velocity of neighbour – velocity of particle) / density-n * laplacian-W-viscosity(r, h) to viscosity-force of particle
				// mass = 1
				forces[index] +=  _v * (viscosity / density_n * laplacianWviscosity * (H - r));
			}
		}
		private void CalculateKinematic(int index, int otherIndex, float2 position, float density_p)
		{
			if (index == otherIndex) {
				return;
			}
			float2 _r = kinematicPositions[otherIndex] - position;
			float r = math.max(0.001f, math.length(_r));
			if (r < H) {
				float2 _v = kinematicVelocities[otherIndex] - velocities[index];
				forces[index] +=  _v * (kinematicViscosity / density_p * laplacianWviscosity * (H - r));
			}
		}
	}

	[BurstCompile]
	struct WaterVelocityChanges : IJobParallelFor
	{
		[ReadOnly] public NativeArray<float2> positions;
		[ReadOnly] public NativeArray<float2> forces;
		public NativeArray<float2> velocities;

		public void Execute(int index)
		{
			//float2 gravity = new float2(0, -9.81f);
			float2 gravity = -9.81f * math.normalizesafe(positions[index]);
			velocities[index] += (forces[index] + gravity) * DT;
		}
	}

	private void Update() {
		if(numParticlesAlive > 0) {
			if (!jobHandle.IsCompleted && skippedFrames < 3) {
				skippedFrames++;
				return;
			}
			skippingFrames = Mathf.Lerp(skippingFrames, 1f * skippedFrames, 0.01f);
			skippedFrames = 0;
			jobHandle.Complete();

			float2 v;
			for(int i = 0; i < numWaterParticlesAlive; i++)
			{
				v = velocities[i];
				particles[i].velocity = new Vector3(v.x, v.y, 0);
			}
			waterSystem.SetParticles(particles, numWaterParticlesAlive);
		}

		foreach (var position in particlesToEmit)
		{
			var emitParams = new ParticleSystem.EmitParams();
			emitParams.position = position;
			waterSystem.Emit(emitParams, 1);
		}
		particlesToEmit.Clear();

		waterSystem.Simulate(DT, true, false, false);

		float multiplier = H / radius;
		
		SetupWaterParticles();
		SetupKinematicParticles();

		numParticlesAlive = numWaterParticlesAlive + numKinematicParticlesAlive;

		if (numParticlesAlive == 0) {
			return;
		}

		hashMap.Clear();
		kinematicHashMap.Clear();

		var applyMultiplier = new ApplyMultiplier()
		{
			multiplier = multiplier,
			positions = positions,
			velocities = velocities
		};
		var hashPositions = new HashPositions()
		{
			positions = positions,
			hashMap = hashMap.ToConcurrent()
		};
		var kinematicApplyMultiplier = new ApplyMultiplier()
		{
			multiplier = multiplier,
			positions = kinematicPositions,
			velocities = kinematicVelocities
		};
		var kinematicHashPositions = new HashPositions()
		{
			positions = kinematicPositions,
			hashMap = kinematicHashMap.ToConcurrent()
		};
		var calculatePressure = new CalculatePressure()
		{
			restDensity = restDensity,
			pressureConstant = pressureConstant,
			positions = positions,
			hashMap = hashMap,
			densities = densities,
			pressures = pressures
		};
		var calculateForce = new CalculateForce()
		{
			viscosity = viscosity,
			positions = positions,
			velocities = velocities,
			densities = densities,
			pressures = pressures,
			hashMap = hashMap,
			kinematicViscosity = kinematicViscosity,
			kinematicPositions = kinematicPositions,
			kinematicVelocities = kinematicVelocities,
			kinematicHashMap = kinematicHashMap,
			forces = forces
		};
		var velocityChanges = new WaterVelocityChanges()
		{
			positions = positions,
			forces = forces,
			velocities = velocities
		};
		var deapplyMultiplier = new DeapplyMultiplier()
		{
			multiplier = multiplier,
			positions = positions,
			velocities = velocities
		};

		var applyMultiplierHandle = applyMultiplier.Schedule(numParticlesAlive, 64);
		var hashMapHandle = hashPositions.Schedule(numParticlesAlive, 64, applyMultiplierHandle);

		var kinematicApplyMultiplierHandle = kinematicApplyMultiplier.Schedule(numKinematicParticlesAlive, 64);
		var kinematicHashPositionsHandle = kinematicHashPositions.Schedule(numKinematicParticlesAlive, 64, kinematicApplyMultiplierHandle);

		var calculatePressureHandle = calculatePressure.Schedule(numParticlesAlive, 64, hashMapHandle);
		var calculateForceHandle = calculateForce.Schedule(numParticlesAlive, 64, JobHandle.CombineDependencies(calculatePressureHandle, kinematicHashPositionsHandle));

		var waterVelocityChangesHandle = velocityChanges.Schedule(numWaterParticlesAlive, 64, calculateForceHandle);
		jobHandle = deapplyMultiplier.Schedule(numWaterParticlesAlive, 64, waterVelocityChangesHandle);

		JobHandle.ScheduleBatchedJobs();
	}

	private void OnDestroy() {
		if(numParticlesAlive > 0) {
			jobHandle.Complete();
		}

		positions.Dispose();
		velocities.Dispose();
		hashMap.Dispose();
		densities.Dispose();
		pressures.Dispose();
		forces.Dispose();

		kinematicPositions.Dispose();
		kinematicVelocities.Dispose();
		kinematicHashMap.Dispose();
	}

	private void SetupWaterParticles() {
		numWaterParticlesAlive = waterSystem.GetParticles(particles);
		Vector2 vec;
		for(int i = 0; i < numWaterParticlesAlive; i++)
		{
			vec = particles[i].position;
			positions[i] = vec;
			vec = particles[i].velocity;
			velocities[i] = vec;
		}
	}

	private void SetupKinematicParticles() {
		WaterKinematicBody[] kinematicBodies = GameObject.FindObjectsOfType<WaterKinematicBody>();
		int numKinematicParticles = 0;
		foreach (var kinematicBody in kinematicBodies)
		{
			kinematicBody.UpdateParticles();
			foreach (var particle in kinematicBody.particles)
			{
				if (numKinematicParticles < MaxKinematicParticles) {
					kinematicPositions[numKinematicParticles] = particle.position;
					kinematicVelocities[numKinematicParticles] = particle.velocity;
					numKinematicParticles++;
				} else { break; }
			}
			if (numKinematicParticles >= MaxKinematicParticles) {
				Debug.LogWarning("Too many kinematic particles");
				break;
			}
		}
		numKinematicParticlesAlive = numKinematicParticles;
	}

	public void Emit(Vector3 position)
	{
		particlesToEmit.Add(position);
	}

	public static int Hash(float2 v, float cellSize)
	{
		return Hash(Quantize(v, cellSize));
	}

	public static int2 Quantize(float2 v, float cellSize)
	{
		return new int2(math.floor(v / cellSize));
	}

	public static int Hash(int2 grid)
	{
		unchecked
		{
			// Simple int3 hash based on a pseudo mix of :
			// 1) https://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function
			// 2) https://en.wikipedia.org/wiki/Jenkins_hash_function
			int hash = grid.x;
			hash = (hash * 397) ^ grid.y;
			hash += hash << 3;
			hash ^= hash >> 11;
			hash += hash << 15;
			return hash;
		}
	}
}
