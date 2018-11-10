﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;

[RequireComponent (typeof(ParticleSystem))]
public class WaterSystem : MonoBehaviour {
	public const float RADIUS = 1.2f;
	public const float VISCOSITY = 0.004f;
	public const float IDEAL_RADIUS = 50f;
	public const float IDEAL_RADIUS_SQ = IDEAL_RADIUS * IDEAL_RADIUS;
	public const float CELL_SIZE = IDEAL_RADIUS * 1f;
	public const float MULTIPLIER = IDEAL_RADIUS / RADIUS;
	public const float DT = 1f / 60f;

	private ParticleSystem waterSystem;
	private ParticleSystem.Particle[] particles;
	void Start () {
		waterSystem = GetComponent<ParticleSystem>();
        particles = new ParticleSystem.Particle[waterSystem.main.maxParticles];
	}
	
	[BurstCompile]
	struct HashPositions : IJobParallelFor
	{
		[ReadOnly] public NativeArray<float2> positions;
		public NativeMultiHashMap<int, int>.Concurrent hashMap;

		public void Execute(int index)
		{
			int hash = Hash(positions[index], CELL_SIZE);
			hashMap.Add(hash, index);
		}
	}

	[BurstCompile]
	struct CalculatePressure : IJobParallelFor
	{
		[ReadOnly] public NativeArray<float2> positions;
		[ReadOnly] public NativeMultiHashMap<int, int> hashMap;
		public NativeArray<float> ps;
		public NativeArray<float> pnears;

		public void Execute(int index)
		{
			ps[index] = 0;
			pnears[index] = 0;

			float2 position = positions[index];
			int otherIndex;
            var iterator = new NativeMultiHashMapIterator<int>();
			int hash;
			for (int i = -1; i < 2; ++i) {
				for (int j = -1; j < 2; ++j) {
					hash = Hash(position + new float2(i * CELL_SIZE, j * CELL_SIZE), CELL_SIZE);
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
		}

		private void Calculate(int index, int otherIndex, float2 position)
		{
			if (index == otherIndex) {
				return;
			}
			float2 relativePosition = positions[otherIndex] - position;
			float distanceSq = math.lengthsq(relativePosition);
			if (distanceSq < 0.01f) {
				var random = new Unity.Mathematics.Random(math.hash(new float4(index, otherIndex, position.x, position.y)));
				relativePosition = random.NextFloat2Direction() * 0.1f;
				distanceSq = 0.01f;
			}
			if (distanceSq < IDEAL_RADIUS_SQ) {
				float distance = math.sqrt(distanceSq);
				float oneminusq = 1.0f - (distance / IDEAL_RADIUS);
				ps[index] += oneminusq * oneminusq;
				pnears[index] += oneminusq * oneminusq * oneminusq;
			}
		}
	}

	[BurstCompile]
	struct CalculateForce : IJobParallelFor
	{
		[ReadOnly] public NativeArray<float2> positions;
		[ReadOnly] public NativeArray<float2> velocities;
		[ReadOnly] public NativeArray<float> ps;
		[ReadOnly] public NativeArray<float> pnears;
		[ReadOnly] public NativeMultiHashMap<int, int> hashMap;
		public NativeArray<float2> delta;

		public void Execute(int index)
		{
			delta[index] = float2.zero;

			float2 position = positions[index];
			int otherIndex;
            var iterator = new NativeMultiHashMapIterator<int>();
			int hash;
			float pressure = (ps[index] - 5f) / 2f; //normal pressure term
			float presnear = pnears[index] / 2f; //near particles term
			for (int i = -1; i < 2; ++i) {
				for (int j = -1; j < 2; ++j) {
					hash = Hash(position + new float2(i * CELL_SIZE, j * CELL_SIZE), CELL_SIZE);
					if (hashMap.TryGetFirstValue(hash, out otherIndex, out iterator))
					{
						Calculate(index, otherIndex, position, pressure, presnear);
						while (hashMap.TryGetNextValue(out otherIndex, ref iterator))
						{
							Calculate(index, otherIndex, position, pressure, presnear);
						}
					}
				}
			}
		}

		private void Calculate(int index, int otherIndex, float2 position, float pressure, float presnear)
		{
			if (index == otherIndex) {
				return;
			}
			float2 relativePosition = positions[otherIndex] - position;
			float distanceSq = math.lengthsq(relativePosition);
			if (distanceSq < 0.01f) {
				var random = new Unity.Mathematics.Random(math.hash(new float4(index, otherIndex, position.x, position.y)));
				relativePosition = random.NextFloat2Direction() * 0.1f;
				distanceSq = 0.01f;
			}
			if (distanceSq < IDEAL_RADIUS_SQ) {
				float distance = math.sqrt(distanceSq);
				float q = distance / IDEAL_RADIUS;
				float oneminusq = 1.0f - q;
				float factor = oneminusq * (pressure + presnear * oneminusq) / (2f * distance);
				float2 d = relativePosition * factor;
				float2 relativeVelocity = velocities[otherIndex] - velocities[index];

				factor = VISCOSITY * oneminusq * DT;
				d -= relativeVelocity * factor;
				delta[index] -= 2 * d;
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
			velocities[index] += delta[index] / DT;
			velocities[index] += new float2(0, -9.81f) * DT * MULTIPLIER;
		}
	}

	private void Update() {
		waterSystem.Simulate(DT, true, false, true);

		int numParticlesAlive = waterSystem.GetParticles(particles);

		var scaledPositions = new NativeArray<float2>(numParticlesAlive, Allocator.Temp);
		var scaledVelocities = new NativeArray<float2>(numParticlesAlive, Allocator.Temp);
		var hashMap = new NativeMultiHashMap<int, int>(numParticlesAlive, Allocator.Temp);
		var ps = new NativeArray<float>(numParticlesAlive, Allocator.Temp);
		var pnears = new NativeArray<float>(numParticlesAlive, Allocator.Temp);
		var deltas = new NativeArray<float2>(numParticlesAlive, Allocator.Temp);

		Vector2 vec;
		for(int i = 0; i < numParticlesAlive; i++)
		{
			vec = particles[i].position;
			scaledPositions[i] = new float2(vec.x, vec.y) * MULTIPLIER;
			vec = particles[i].velocity;
			scaledVelocities[i] = new float2(vec.x, vec.y) * MULTIPLIER;
		}

		var hashMapJob = new HashPositions()
		{
			positions = scaledPositions,
			hashMap = hashMap.ToConcurrent()
		};
		var calculatePressure = new CalculatePressure()
		{
			positions = scaledPositions,
			hashMap = hashMap,
			ps = ps,
			pnears = pnears
		};
		var calculateForce = new CalculateForce()
		{
			positions = scaledPositions,
			velocities = scaledVelocities,
			ps = ps,
			pnears = pnears,
			hashMap = hashMap,
			delta = deltas
		};
		var velocityChanges = new VelocityChanges()
		{
			delta = deltas,
			velocities = scaledVelocities
		};

		var hashMapHandle = hashMapJob.Schedule(numParticlesAlive, 64);
		var calculatePressureHandle = calculatePressure.Schedule(numParticlesAlive, 64, hashMapHandle);
		var calculateForceHandle = calculateForce.Schedule(numParticlesAlive, 64, calculatePressureHandle);
		var velocityChangesHandle = velocityChanges.Schedule(numParticlesAlive, 64, calculateForceHandle);

		velocityChangesHandle.Complete();

		float2 v;
		for(int i = 0; i < numParticlesAlive; i++)
		{
			v = scaledVelocities[i] / MULTIPLIER;
			particles[i].velocity = new Vector3(v.x, v.y, 0);
		}
		waterSystem.SetParticles(particles, numParticlesAlive);

		scaledPositions.Dispose();
		scaledVelocities.Dispose();
		hashMap.Dispose();
		ps.Dispose();
		pnears.Dispose();
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