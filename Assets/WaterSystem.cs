﻿using System.Collections;
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
	[Range(0.05f, 1f)]
	public float radius = 1f;
	public const float H = 1f;
	public const float H2 = H * H;
	public const float H3 = H * H2;
	public const float H6 = H3 * H3;
	public const float H9 = H3 * H6;
	public const float Wpoly6 = 315f / (64f * Mathf.PI * H9);
	public const float gradientWspiky = -45 / (Mathf.PI * H6);
	public const float laplacianWviscosity = 45 / (Mathf.PI * H6);
	public const float DT = 0.016f;

	public Camera _camera;
	private int skippedFrames = 0;
	private ParticleSystem waterSystem;
	private ParticleSystem.Particle[] particles;

	private int numParticlesAlive;
	private NativeArray<float2> scaledPositions;
	private NativeArray<float2> scaledVelocities;
	private NativeMultiHashMap<int, int> hashMap;
	private NativeArray<float> densities;
	private NativeArray<float> pressures;
	private NativeArray<float2> deltas;
	private JobHandle jobHandle;
	void Start () {
		Debug.Log("Wpoly6 " + Wpoly6);
		Debug.Log("gradientWspiky " + gradientWspiky);
		Debug.Log("laplacianWviscosity " + laplacianWviscosity);
		waterSystem = GetComponent<ParticleSystem>();
        particles = new ParticleSystem.Particle[waterSystem.main.maxParticles];
		numParticlesAlive = 0;

		scaledPositions = new NativeArray<float2>(waterSystem.main.maxParticles, Allocator.Persistent);
		scaledVelocities = new NativeArray<float2>(waterSystem.main.maxParticles, Allocator.Persistent);
		hashMap = new NativeMultiHashMap<int, int>(waterSystem.main.maxParticles, Allocator.Persistent);
		densities = new NativeArray<float>(waterSystem.main.maxParticles, Allocator.Persistent);
		pressures = new NativeArray<float>(waterSystem.main.maxParticles, Allocator.Persistent);
		deltas = new NativeArray<float2>(waterSystem.main.maxParticles, Allocator.Persistent);
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
		public NativeMultiHashMap<int, int>.Concurrent hashMap;

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
					if (hashMap.TryGetFirstValue(hash, out otherIndex, out iterator))
					{
						Calculate(index, otherIndex, position);
						while (hashMap.TryGetNextValue(out otherIndex, ref iterator))
						{
							Calculate(index, otherIndex, position);
						}
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
				// add mass * W_poly6(r, h) to density of particle
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
		public NativeArray<float2> delta;

		public void Execute(int index)
		{
			delta[index] = float2.zero;

			float2 position = positions[index];
			int otherIndex;
            var iterator = new NativeMultiHashMapIterator<int>();
			int hash;
			float pressure_p = pressures[index];
			for (int i = -1; i < 2; ++i) {
				for (int j = -1; j < 2; ++j) {
					hash = Hash(position + new float2(i * H, j * H), H);
					if (hashMap.TryGetFirstValue(hash, out otherIndex, out iterator))
					{
						Calculate(index, otherIndex, position, pressure_p);
						while (hashMap.TryGetNextValue(out otherIndex, ref iterator))
						{
							Calculate(index, otherIndex, position, pressure_p);
						}
					}
				}
			}
			delta[index] = delta[index] / densities[index];
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

				//add mass * (pressure-p + pressure-n) / (2 * density-n) * gradient-W-spiky(r, h) to pressure-force of particle
				// mass = 1
				delta[index] += (_r / r) * ((pressure_p + pressure_n) / (2 * density_n) * gradientWspiky * math.pow(H - r, 2));

				float2 _v = velocities[otherIndex] - velocities[index];

				//add eta * mass * (velocity of neighbour – velocity of particle) / density-n * laplacian-W-viscosity(r, h) to viscosity-force of particle
				// mass = 1
				delta[index] += viscosity * _v / density_n * laplacianWviscosity * (H - r);
			}
		}
	}

	[BurstCompile]
	struct VelocityChanges : IJobParallelFor
	{
		[ReadOnly] public NativeArray<float2> delta;
		public NativeArray<float2> velocities;

		public void Execute(int index)
		{
			velocities[index] += (delta[index] + new float2(0, -9.81f)) * DT;
		}
	}

	private void Update() {
		int oldParticleCount = numParticlesAlive;
		if(numParticlesAlive > 0) {
			if (!jobHandle.IsCompleted && skippedFrames < 3) {
				skippedFrames++;
				return;
			}
			skippedFrames = 0;
			jobHandle.Complete();

			float2 v;
			for(int i = 0; i < numParticlesAlive; i++)
			{
				v = scaledVelocities[i];
				particles[i].velocity = new Vector3(v.x, v.y, 0);
			}
			waterSystem.SetParticles(particles, numParticlesAlive);
			numParticlesAlive = 0;
		}

		if(Input.GetMouseButton(0)) {
			Vector3 mousePos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0f);
			Vector3 wordPos = _camera.ScreenToWorldPoint(mousePos);
			for (int i = 0; i < 5; i++) {
				Vector3 position = wordPos + UnityEngine.Random.insideUnitSphere * radius;
				position.z = 0f;
				var emitParams = new ParticleSystem.EmitParams();
				emitParams.position = position;
				waterSystem.Emit(emitParams, 1);
			}
		}

		waterSystem.Simulate(DT, true, false, false);

		float multiplier = H / radius;

		numParticlesAlive = waterSystem.GetParticles(particles);
		if (numParticlesAlive == 0) {
			return;
		}

		if(Input.GetMouseButton(0)) {
			Debug.Log ("count " + numParticlesAlive);
		}

		hashMap.Clear();

		Vector2 vec;
		for(int i = 0; i < numParticlesAlive; i++)
		{
			vec = particles[i].position;
			scaledPositions[i] = new float2(vec.x, vec.y);
			vec = particles[i].velocity;
			scaledVelocities[i] = new float2(vec.x, vec.y);
		}

		var applyMultiplier = new ApplyMultiplier()
		{
			multiplier = multiplier,
			positions = scaledPositions,
			velocities = scaledVelocities
		};
		var hashPositions = new HashPositions()
		{
			positions = scaledPositions,
			hashMap = hashMap.ToConcurrent()
		};
		var calculatePressure = new CalculatePressure()
		{
			restDensity = restDensity,
			pressureConstant = pressureConstant,
			positions = scaledPositions,
			hashMap = hashMap,
			densities = densities,
			pressures = pressures
		};
		var calculateForce = new CalculateForce()
		{
			viscosity = viscosity,
			positions = scaledPositions,
			velocities = scaledVelocities,
			densities = densities,
			pressures = pressures,
			hashMap = hashMap,
			delta = deltas
		};
		var velocityChanges = new VelocityChanges()
		{
			delta = deltas,
			velocities = scaledVelocities
		};
		var deapplyMultiplier = new DeapplyMultiplier()
		{
			multiplier = multiplier,
			positions = scaledPositions,
			velocities = scaledVelocities
		};

		var applyMultiplierHandle = applyMultiplier.Schedule(numParticlesAlive, 64);
		var hashMapHandle = hashPositions.Schedule(numParticlesAlive, 64, applyMultiplierHandle);
		var calculatePressureHandle = calculatePressure.Schedule(numParticlesAlive, 64, hashMapHandle);
		var calculateForceHandle = calculateForce.Schedule(numParticlesAlive, 64, calculatePressureHandle);
		var velocityChangesHandle = velocityChanges.Schedule(numParticlesAlive, 64, calculateForceHandle);
		jobHandle = deapplyMultiplier.Schedule(numParticlesAlive, 64, velocityChangesHandle);
		JobHandle.ScheduleBatchedJobs();
	}

	private void OnDestroy() {
		if(numParticlesAlive > 0) {
			jobHandle.Complete();
		}
		scaledPositions.Dispose();
		scaledVelocities.Dispose();
		hashMap.Dispose();
		densities.Dispose();
		pressures.Dispose();
		deltas.Dispose();
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
