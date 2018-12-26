using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;

public sealed class KinematicParticle
{
	public Vector2 position;
	public Vector2 velocity;
	public float buoyance;
	public Vector2 force;
}

public sealed class FluidContainers : IDisposable
{
	private ParticleSystem particleSystem;
	public List<ParticleSystem.EmitParams> particlesToEmit = new List<ParticleSystem.EmitParams>();
	public ParticleSystem.Particle[] particles;
	public int numAlive = 0;
	public NativeArray<float2> positions;
	public NativeArray<float2> velocities;
	public NativeMultiHashMap<int, int> hashMap;
	public NativeArray<float> densities;
	public NativeArray<float> pressures;
	public NativeArray<float2> forces;

	public FluidContainers(ParticleSystem ps)
	{
		particleSystem = ps;
		particles = new ParticleSystem.Particle[ps.main.maxParticles];
		positions = new NativeArray<float2>(ps.main.maxParticles, Allocator.Persistent);
		velocities = new NativeArray<float2>(ps.main.maxParticles, Allocator.Persistent);
		hashMap = new NativeMultiHashMap<int, int>(ps.main.maxParticles, Allocator.Persistent);
		densities = new NativeArray<float>(ps.main.maxParticles, Allocator.Persistent);
		pressures = new NativeArray<float>(ps.main.maxParticles, Allocator.Persistent);
		forces = new NativeArray<float2>(ps.main.maxParticles, Allocator.Persistent);
	}

	public void SetupFluidParticles() {
		numAlive = particleSystem.GetParticles(particles);
		Vector2 vec;
		ParticleSystem.Particle particle;
		for(int i = 0; i < numAlive; i++)
		{
			particle = particles[i];
			vec = particle.position;
			positions[i] = vec;
			vec = particle.velocity;
			velocities[i] = vec;
		}
	}

	public void UpdateFluidParticles() {
		float2 v;
		for(int i = 0; i < numAlive; i++)
		{
			v = velocities[i];
			particles[i].velocity = new Vector3(v.x, v.y, 0);
		}
		particleSystem.SetParticles(particles, numAlive);
	}

	public void EmitParticles() {
		Vector3 position;
		float z = particleSystem.transform.position.z;
		foreach (var p in particlesToEmit)
		{
			var emitParams = p;
			position = p.position;
			position.z = z;
			emitParams.position = position;
			particleSystem.Emit(emitParams, 1);
		}
		particlesToEmit.Clear();
	}

	public void Dispose()
	{
		positions.Dispose();
		velocities.Dispose();
		hashMap.Dispose();
		densities.Dispose();
		pressures.Dispose();
		forces.Dispose();
	}
}

public sealed class KinematicContainers : IDisposable
{
	public KinematicParticle[] particles;
	public int numAlive = 0;
	public NativeArray<float2> positions;
	public NativeArray<float2> velocities;
	public NativeMultiHashMap<int, int> hashMap;
	public NativeArray<float> buoyances;
	public NativeArray<float2> forces;

	public KinematicContainers(int maxParticles)
	{
		particles = new KinematicParticle[maxParticles];
		positions = new NativeArray<float2>(maxParticles, Allocator.Persistent);
		velocities = new NativeArray<float2>(maxParticles, Allocator.Persistent);
		hashMap = new NativeMultiHashMap<int, int>(maxParticles, Allocator.Persistent);
		buoyances = new NativeArray<float>(maxParticles, Allocator.Persistent);
		forces = new NativeArray<float2>(maxParticles, Allocator.Persistent);
	}

	public void Dispose()
	{
		positions.Dispose();
		velocities.Dispose();
		hashMap.Dispose();
		buoyances.Dispose();
		forces.Dispose();
	}
}

public sealed class ExplosionContainers : IDisposable
{
	public struct Explosion
	{
		public float2 position;
		public float force;
		public float lifeTime;
	}
	public List<Explosion> explosionsList = new List<Explosion>();
	public int numAlive = 0;
	public NativeArray<float2> positions;
	public NativeArray<float> forces;
	public ExplosionContainers(int maxParticles)
	{
		positions = new NativeArray<float2>(maxParticles, Allocator.Persistent);
		forces = new NativeArray<float>(maxParticles, Allocator.Persistent);
	}

	public void Dispose()
	{
		positions.Dispose();
		forces.Dispose();
	}
}

[RequireComponent (typeof(ParticleSystem))]
public class WaterSystem : FluidSystem {
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
	public ParticleSystem steamSystem;
	[Range(1f, 10f)]
	public float steamRestDensity = 1f;

	[Range(0f, 500f)]
	public float steamPressureConstant = 100f;
	[Range(0f, 10f)]
	public float steamViscosity = 0.1f;

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
	public const int MaxExplosions = 100;
	public const float ExplosionRadius2 = 25;

	private int skippedFrames = 0;
	private int numParticlesAlive;

	private ParticleSystem waterSystem;
	private FluidContainers waterContainers;
	private FluidContainers steamContainers;
	private KinematicContainers kinematicContainers;
	private ExplosionContainers explosionContainers;

	private JobHandle jobHandle;
	void Start () {
		Debug.Log("Wpoly6 " + Wpoly6);
		Debug.Log("gradientWspiky " + gradientWspiky);
		Debug.Log("laplacianWviscosity " + laplacianWviscosity);
		numParticlesAlive = 0;

		waterSystem = GetComponent<ParticleSystem>();
		
		waterContainers = new FluidContainers(waterSystem);
		steamContainers = new FluidContainers(steamSystem);
		kinematicContainers = new KinematicContainers(MaxKinematicParticles);
		explosionContainers = new ExplosionContainers(MaxExplosions);
	}
	
	[BurstCompile]
	struct ApplyMultiplier : IJobParallelFor
	{
		[ReadOnly] public float multiplier;
		public NativeArray<float2> array;

		public void Execute(int index)
		{
			array[index] = array[index] * multiplier;
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
		[WriteOnly] public NativeArray<float> densities;
		[WriteOnly] public NativeArray<float> pressures;

		public void Execute(int index)
		{
			float density = 0;
			float2 position = positions[index];
			int otherIndex;
            var iterator = new NativeMultiHashMapIterator<int>();
			int hash;
			for (int i = -1; i < 2; ++i) {
				for (int j = -1; j < 2; ++j) {
					hash = Hash(position + new float2(i * H, j * H), H);
					if (hashMap.TryGetFirstValue(hash, out otherIndex, out iterator)) {
						do {
							Calculate(otherIndex, position, ref density);
						} while (hashMap.TryGetNextValue(out otherIndex, ref iterator));
					}
				}
			}

			density = math.max(restDensity, Wpoly6 * density);
			densities[index] = density;
			//  k * (density-p – rest-density)
			//pressures[index] = pressureConstant * (density - restDensity);
			float p = density / restDensity;
			pressures[index] = pressureConstant * (p * p - 1);
		}

		private void Calculate(int otherIndex, float2 position, ref float density)
		{
			float2 r = positions[otherIndex] - position;
			float r2 = math.lengthsq(r);
			if (r2 < H2) {
				// add mass-n * W_poly6(r, h) to density of particle
				// mass = 1
				density += math.pow(H2 - r2, 3);
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
			float2 pForce = float2.zero;
			float2 vForce = float2.zero;
			float2 kForce = float2.zero;

			float2 position = positions[index];
			float2 velocity = velocities[index];
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
							if (index != otherIndex) {
								Calculate(otherIndex, position, velocity, pressure_p, ref pForce, ref vForce);
							}
						} while (hashMap.TryGetNextValue(out otherIndex, ref iterator));
					}
					if (kinematicHashMap.TryGetFirstValue(hash, out otherIndex, out iterator)) {
						do {
							CalculateKinematic(otherIndex, position, velocity, ref kForce);
						} while (kinematicHashMap.TryGetNextValue(out otherIndex, ref iterator));
					}
				}
			}
			pForce = gradientWspiky * pForce;
			vForce = viscosity * laplacianWviscosity * vForce;
			kForce = kinematicViscosity / density_p * laplacianWviscosity * kForce;
			forces[index] = (pForce + vForce + kForce) / density_p;
		}

		private void Calculate(int otherIndex, float2 position, float2 velocity, float pressure_p, ref float2 pForce, ref float2 vForce)
		{
			float2 _r = positions[otherIndex] - position;
			float r = math.max(0.001f, math.length(_r));
			if (r < H) {
				float density_n = densities[otherIndex];
				float pressure_n = pressures[otherIndex];

				//add mass-n * (pressure-p + pressure-n) / (2 * density-n) * gradient-W-spiky(r, h) to pressure-force of particle
				// mass = 1
				pForce += (_r / r) * ((pressure_p + pressure_n) / (2 * density_n) * math.pow(H - r, 2));

				float2 _v = velocities[otherIndex] - velocity;

				//add viscosity * mass-n * (velocity of neighbour – velocity of particle) / density-n * laplacian-W-viscosity(r, h) to viscosity-force of particle
				// mass = 1
				vForce +=  _v * (1f / density_n * (H - r));
			}
		}
		private void CalculateKinematic(int otherIndex, float2 position, float2 velocity, ref float2 force)
		{
			float2 _r = kinematicPositions[otherIndex] - position;
			float r = math.length(_r);
			if (r < H) {
				float2 _v = kinematicVelocities[otherIndex] - velocity;
				force +=  _v * (H - r);
			}
		}
	}

	[BurstCompile]
	struct FluidVelocityChanges : IJobParallelFor
	{
		[ReadOnly] public float gravityA;
		[ReadOnly] public NativeArray<float2> positions;
		[ReadOnly] public NativeArray<float2> forces;
		[ReadOnly] public int numExplosions;
		[ReadOnly] public NativeArray<float2> explosionPositions;
		[ReadOnly] public NativeArray<float> explosionForces;
		public NativeArray<float2> velocities;

		public void Execute(int index)
		{
			float2 position = positions[index];
			float2 explosionForce = float2.zero;
			for (int i = 0; i < numExplosions; ++i) {
				CalculateExplosion(i, position, ref explosionForce);
			}
			//float2 gravity = new float2(0, -9.81f);
			float2 gravity = gravityA * math.normalizesafe(position);
			velocities[index] += (forces[index] + gravity + explosionForce) * DT;
		}

		private void CalculateExplosion(int otherIndex, float2 position, ref float2 force)
		{
			float2 _r = position - explosionPositions[otherIndex];
			float r2 = math.lengthsq(_r);
			if (r2 < ExplosionRadius2) {
				float q = r2 / ExplosionRadius2;
				force += math.normalizesafe(_r) * (explosionForces[otherIndex] * (1 - q));
			}
		}
	}

	[BurstCompile]
	struct CalculateKinematicForce : IJobParallelFor
	{
		[ReadOnly] public float restDensity;
		[ReadOnly] public float kinematicViscosity;
		[ReadOnly] public NativeArray<float2> positions;
		[ReadOnly] public NativeArray<float2> velocities;
		[ReadOnly] public NativeArray<float> densities;
		[ReadOnly] public NativeMultiHashMap<int, int> hashMap;
		[ReadOnly] public NativeArray<float2> kinematicPositions;
		[ReadOnly] public NativeArray<float2> kinematicVelocities;
		[WriteOnly] public NativeArray<float> kinematicBuoyances;
		[WriteOnly] public NativeArray<float2> kinematicForces;

		public void Execute(int index)
		{
			float2 position = kinematicPositions[index];
			float2 velocity = kinematicVelocities[index];
			float density = 0;
			float2 force = float2.zero;
			int otherIndex;
            var iterator = new NativeMultiHashMapIterator<int>();
			int hash;
			for (int i = -1; i < 2; ++i) {
				for (int j = -1; j < 2; ++j) {
					hash = Hash(position + new float2(i * H, j * H), H);
					if (hashMap.TryGetFirstValue(hash, out otherIndex, out iterator)) {
						do {
							Calculate(otherIndex, position, velocity, ref density, ref force);
						} while (hashMap.TryGetNextValue(out otherIndex, ref iterator));
					}
				}
			}
			kinematicBuoyances[index] = math.step(restDensity, density);
			kinematicForces[index] = force;
		}

		private void Calculate(int otherIndex, float2 position, float2 velocity, ref float density, ref float2 force)
		{
			float2 _r = positions[otherIndex] - position;
			float r2 = math.lengthsq(_r);
			if (r2 < H2) {
				density += Wpoly6 * math.pow(H2 - r2, 3);

				float r = math.sqrt(r2);
				float2 _v = velocities[otherIndex] - velocity;
				float p = densities[otherIndex];
				force +=  _v * (kinematicViscosity / (p * p) * laplacianWviscosity * (H - r));
			}
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

			waterContainers.UpdateFluidParticles();
			steamContainers.UpdateFluidParticles();
			UpdateKinematicParticles();
		}

		waterContainers.EmitParticles();
		steamContainers.EmitParticles();

		waterSystem.Simulate(DT, true, false, false);
		steamSystem.Simulate(DT, true, false, false);

		float multiplier = H / radius;
		float demultiplier = radius / H;
		
		waterContainers.SetupFluidParticles();
		steamContainers.SetupFluidParticles();
		SetupKinematicParticles();
		SetupExplosions();

		numParticlesAlive = waterContainers.numAlive + steamContainers.numAlive + kinematicContainers.numAlive;

		if (numParticlesAlive == 0) {
			return;
		}

		steamContainers.hashMap.Clear();

		// Initialize data
		var setupWaterHandle = NewFluidSetup(multiplier, restDensity, pressureConstant, waterContainers);
		var setupSteamHandle = NewFluidSetup(multiplier, steamRestDensity, steamPressureConstant, steamContainers);
		var setupKinematicHandle = NewKinematicSetup(multiplier, kinematicContainers);
		var setupExplosionHandle = NewApplyMultiplier(multiplier, explosionContainers.positions, explosionContainers.numAlive);

		// Calculate forces
		var waterForceHandle = NewCalculateForce(viscosity, waterContainers, kinematicViscosity, kinematicContainers, JobHandle.CombineDependencies(setupWaterHandle, setupKinematicHandle));
		var steamForceHandle = NewCalculateForce(steamViscosity, steamContainers, kinematicViscosity, kinematicContainers, JobHandle.CombineDependencies(setupSteamHandle, setupKinematicHandle));
		var kinematicForceHandle = NewCalculateKinematicForce(restDensity, waterContainers, kinematicViscosity, kinematicContainers, JobHandle.CombineDependencies(setupWaterHandle, setupKinematicHandle));

		// Apply velocity changes
		var waterVelocityChangesHandle = NewVelocityChanges(-9.81f, waterContainers, explosionContainers, JobHandle.CombineDependencies(waterForceHandle, kinematicForceHandle, setupExplosionHandle));
		var steamVelocityChangesHandle = NewVelocityChanges(5f, steamContainers, explosionContainers, JobHandle.CombineDependencies(steamForceHandle, kinematicForceHandle, setupExplosionHandle));

		// Collect results
		var waterResultsHandle = NewApplyDemultiplier(demultiplier, waterContainers, waterVelocityChangesHandle);
		var steamResultsHandle = NewApplyDemultiplier(demultiplier, steamContainers, steamVelocityChangesHandle);
		var kinematicResultsHandle = NewApplyDemultiplier(demultiplier, kinematicContainers, kinematicForceHandle);

		jobHandle = JobHandle.CombineDependencies(waterResultsHandle, steamResultsHandle, kinematicResultsHandle);

		JobHandle.ScheduleBatchedJobs();
	}

	private static JobHandle NewKinematicSetup(float multiplier, KinematicContainers kc)
	{
		kc.hashMap.Clear();
		var kinematicApplyMultiplierHandle = NewApplyMultiplier(multiplier, kc);
		var kinematicHashPositionsHandle = NewHashPositions(kc, kinematicApplyMultiplierHandle);
		return kinematicHashPositionsHandle;
	}
	private static JobHandle NewFluidSetup(float multiplier, float restDensity, float pressureConstant, FluidContainers fc)
	{
		fc.hashMap.Clear();
		var applyMultiplierHandle = NewApplyMultiplier(multiplier, fc);
		var hashMapHandle = NewHashPositions(fc, applyMultiplierHandle);
		var calculatePressureHandle = NewCalculatePressure(restDensity, pressureConstant, fc, hashMapHandle);
		return calculatePressureHandle;
	}
	private static JobHandle NewApplyMultiplier(float multiplier, FluidContainers fc)
	{
		return JobHandle.CombineDependencies(
			NewApplyMultiplier(multiplier, fc.positions, fc.numAlive),
			NewApplyMultiplier(multiplier, fc.velocities, fc.numAlive)
			);
	}

	private static JobHandle NewApplyMultiplier(float multiplier, KinematicContainers kc)
	{
		return JobHandle.CombineDependencies(
			NewApplyMultiplier(multiplier, kc.positions, kc.numAlive),
			NewApplyMultiplier(multiplier, kc.velocities, kc.numAlive)
			);
	}
	private static JobHandle NewApplyDemultiplier(float demultiplier, FluidContainers fc, JobHandle dependsOn)
	{
		return NewApplyMultiplier(demultiplier, fc.velocities, fc.numAlive, dependsOn);
	}
	private static JobHandle NewApplyDemultiplier(float demultiplier, KinematicContainers kc, JobHandle dependsOn)
	{
		return NewApplyMultiplier(demultiplier, kc.forces, kc.numAlive, dependsOn);
	}
	private static JobHandle NewApplyMultiplier(float multiplier, NativeArray<float2> array, int count, JobHandle dependsOn = default(JobHandle))
	{
		var multiply = new ApplyMultiplier()
		{
			multiplier = multiplier,
			array = array
		};
		return multiply.Schedule(count, 64, dependsOn);
	}

	private static JobHandle NewHashPositions(FluidContainers fc, JobHandle dependsOn)
	{
		return NewHashPositions(fc.positions, fc.hashMap, fc.numAlive, dependsOn);
	}
	private static JobHandle NewHashPositions(KinematicContainers kc, JobHandle dependsOn)
	{
		return NewHashPositions(kc.positions, kc.hashMap, kc.numAlive, dependsOn);
	}

	private static JobHandle NewHashPositions(NativeArray<float2> positions, NativeMultiHashMap<int, int> hashMap, int count, JobHandle dependsOn)
	{
		var hashPositions = new HashPositions()
		{
			positions = positions,
			hashMap = hashMap.ToConcurrent()
		};
		return hashPositions.Schedule(count, 64, dependsOn);
	}

	private static JobHandle NewCalculatePressure(float restDensity, float pressureConstant, FluidContainers fc, JobHandle dependsOn)
	{
		var calculatePressure = new CalculatePressure()
		{
			restDensity = restDensity,
			pressureConstant = pressureConstant,
			positions = fc.positions,
			hashMap = fc.hashMap,
			densities = fc.densities,
			pressures = fc.pressures
		};
		return calculatePressure.Schedule(fc.numAlive, 64, dependsOn);
	}

	private static JobHandle NewCalculateForce(float viscosity, FluidContainers fc, float kinematicViscosity, KinematicContainers kc, JobHandle dependsOn)
	{
		var calculateForce = new CalculateForce()
		{
			viscosity = viscosity,
			positions = fc.positions,
			velocities = fc.velocities,
			densities = fc.densities,
			pressures = fc.pressures,
			hashMap = fc.hashMap,
			kinematicViscosity = kinematicViscosity,
			kinematicPositions = kc.positions,
			kinematicVelocities = kc.velocities,
			kinematicHashMap = kc.hashMap,
			forces = fc.forces
		};
		return calculateForce.Schedule(fc.numAlive, 64, dependsOn);
	}

	private static JobHandle NewVelocityChanges(float gravity, FluidContainers fc, ExplosionContainers ec, JobHandle dependsOn)
	{
		var velocityChanges = new FluidVelocityChanges()
		{
			gravityA = gravity,
			positions = fc.positions,
			forces = fc.forces,
			numExplosions = ec.numAlive,
			explosionPositions = ec.positions,
			explosionForces = ec.forces,
			velocities = fc.velocities
		};
		return velocityChanges.Schedule(fc.numAlive, 64, dependsOn);
	}

	private static JobHandle NewCalculateKinematicForce(float restDensity, FluidContainers fc, float kinematicViscosity, KinematicContainers kc, JobHandle dependsOn)
	{
		var calculateKinematicForce = new CalculateKinematicForce()
		{
			restDensity = restDensity,
			kinematicViscosity = kinematicViscosity,
			positions = fc.positions,
			velocities = fc.velocities,
			densities = fc.densities,
			hashMap = fc.hashMap,
			kinematicPositions = kc.positions,
			kinematicVelocities = kc.velocities,
			kinematicBuoyances = kc.buoyances,
			kinematicForces = kc.forces
		};
		return calculateKinematicForce.Schedule(kc.numAlive, 64, dependsOn);
	}

	private void OnDestroy() {
		if(numParticlesAlive > 0) {
			jobHandle.Complete();
		}

		waterContainers.Dispose();
		steamContainers.Dispose();
		kinematicContainers.Dispose();
		explosionContainers.Dispose();
	}

	private void OnParticleTrigger() {
		List<ParticleSystem.Particle> enter = new List<ParticleSystem.Particle>();
        int numEnter = waterSystem.GetTriggerParticles(ParticleSystemTriggerEventType.Enter, enter);
		if (numEnter > 0) {
			foreach (var particle in enter)
			{
				EmitSteam(particle.position, Vector2.zero);
			}
		}
	}

	private void SetupKinematicParticles() {
		WaterKinematicBody[] kinematicBodies = GameObject.FindObjectsOfType<WaterKinematicBody>();
		int index = 0;
		foreach (var kinematicBody in kinematicBodies)
		{
			kinematicBody.UpdateParticles();
			foreach (var particle in kinematicBody.particles)
			{
				if (index < MaxKinematicParticles) {
					kinematicContainers.particles[index] = particle;
					kinematicContainers.positions[index] = particle.position;
					kinematicContainers.velocities[index] = particle.velocity;
					index++;
				} else { break; }
			}
			if (index >= MaxKinematicParticles) {
				Debug.LogWarning("Too many kinematic particles");
				break;
			}
		}
		kinematicContainers.numAlive = index;
	}

	private void UpdateKinematicParticles() {
		KinematicParticle particle;
		for(int i = 0; i < kinematicContainers.numAlive; i++)
		{
			particle = kinematicContainers.particles[i];
			particle.buoyance = kinematicContainers.buoyances[i];
			particle.force = kinematicContainers.forces[i];
		}
	}

	private void SetupExplosions() {
		int count = explosionContainers.explosionsList.Count;
		var newExplosionsList = new List<ExplosionContainers.Explosion>();
		ExplosionContainers.Explosion e;
		for (int i = 0; i < count; i++)
		{
			e = explosionContainers.explosionsList[i];
			explosionContainers.positions[i] = e.position;
			explosionContainers.forces[i] = e.force;
			e.lifeTime -= DT;
			if (e.lifeTime > 0) {
				newExplosionsList.Add(e);
			}
		}
		explosionContainers.numAlive = count;
		explosionContainers.explosionsList = newExplosionsList;
	}

	public override void EmitWater(Vector2 position, Vector2 velocity)
	{
		var p = new ParticleSystem.EmitParams();
		p.position = position;
		p.velocity = velocity;
		waterContainers.particlesToEmit.Add(p);
	}

	public override void EmitSteam(Vector2 position, Vector2 velocity)
	{
		var p = new ParticleSystem.EmitParams();
		p.position = position;
		p.velocity = velocity;
		steamContainers.particlesToEmit.Add(p);
	}

	public override void EmitExplosion(Vector2 position, float force, float lifeTime)
	{
		if (explosionContainers.explosionsList.Count < MaxExplosions) {
			explosionContainers.explosionsList.Add(new ExplosionContainers.Explosion()
			{
				position = position,
				force = force,
				lifeTime = lifeTime
			});
		}
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
