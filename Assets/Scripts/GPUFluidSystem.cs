﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

public class GPUFluidSystem : MonoBehaviour {
	private struct GPUParticle
	{
		public bool alive;
		public Vector2 position;
		public Vector2 velocity;
		public Vector2 life; //x = age, y = lifetime
		public float density;
		public Vector2 force;
	}

	private struct GPUKinematicParticle
	{
		public Vector2 position;
		public Vector2 velocity;
	}

	[Range(1f, 10f)]
	public float restDensity = 3f;

	[Range(0f, 500f)]
	public float pressureConstant = 250f;

	[Range(0f, 10f)]
	public float viscosity = 1f;

	[Range(0f, 10f)]
	public float kinematicViscosity = 5f;

	[Range(0.05f, 1f)]
	public float radius = 1f;
	public ComputeShader computeShader;
	public int maxParticles = 131072;
	public int maxKinematicParticles = 1024;
	public Material material;
	public TerrainDistanceField terrainDistanceField;
	private const string propParticles = "_Particles";
	private const string propKinematicParticles = "_KinematicParticles";
	private const string propKinematicForcesAndBuoyances = "_KinematicForcesAndBuoyances";
	private const string propCellOffsets = "_CellOffsets";
	private const string propDead = "_Dead";
	private const string propPool = "_Pool";
	private const string propAlive = "_Alive";
	private const string propUploads = "_Uploads";
	private const string propTerrainDistanceField = "_TerrainDistanceField";
	private int initKernel;
	private int emitKernel;
	private int sortKernel;
	private int resetCellOffsetsKernel;
	private int calculateCellOffsetsKernel;
	private int calculateDensityKernel;
	private int calculateForceKernel;
	private int calculateKinematicForceKernel;
	private int updateKernel;
	private int threadCount;
	private int groupCount;
	private int bufferSize;
	private int kinematicGroupCount;
	private int kinematicBufferSize;
	private ComputeBuffer particles;
	private ComputeBuffer kinematicParticles;
	private ComputeBuffer kinematicForcesAndBuoyances;
	private ComputeBuffer cellOffsets;
	private ComputeBuffer args;
	private ComputeBuffer pool;
	private ComputeBuffer alive;
	private ComputeBuffer uploads;
	private ComputeBuffer counter;
	private int[] counterArray;
    public int poolCount = 0;
	private Mesh mesh;
	private Bounds bounds;
	private List<Vector4> emitList = new List<Vector4>();
	private KinematicParticle[] kParticles;
	private GPUKinematicParticle[] kGPUParticles;
	private Vector3[] kForcesAndBuoyances;
	public int kinematicNumAlive = 0;

	private void OnEnable() {
		initKernel = computeShader.FindKernel("Init");
		emitKernel = computeShader.FindKernel("Emit");
		sortKernel = computeShader.FindKernel("BitonicSortParticles");
		resetCellOffsetsKernel = computeShader.FindKernel("ResetCellOffsets");
		calculateCellOffsetsKernel = computeShader.FindKernel("CalculateCellOffsets");
		calculateDensityKernel = computeShader.FindKernel("CalculateDensity");
		calculateForceKernel = computeShader.FindKernel("CalculateForce");
		calculateKinematicForceKernel = computeShader.FindKernel("CalculateKinematicForce");
		updateKernel = computeShader.FindKernel("Update");

		uint x, y, z;
		computeShader.GetKernelThreadGroupSizes(initKernel, out x, out y, out z);
		
		threadCount = (int)x;
		groupCount = maxParticles / threadCount;
		bufferSize = groupCount * threadCount;

		kinematicGroupCount = maxKinematicParticles / threadCount;
		kinematicBufferSize = kinematicGroupCount * threadCount;

		particles = new ComputeBuffer(bufferSize, Marshal.SizeOf(typeof(GPUParticle)), ComputeBufferType.Default);
		kinematicParticles = new ComputeBuffer(kinematicBufferSize, Marshal.SizeOf(typeof(GPUKinematicParticle)), ComputeBufferType.Default);
		kinematicForcesAndBuoyances = new ComputeBuffer(kinematicBufferSize, Marshal.SizeOf(typeof(Vector3)), ComputeBufferType.Default);
		cellOffsets = new ComputeBuffer(1024 * 1024, Marshal.SizeOf(typeof(uint)), ComputeBufferType.Default);
		args = new ComputeBuffer(5, Marshal.SizeOf(typeof(uint)), ComputeBufferType.IndirectArguments);
		pool = new ComputeBuffer(bufferSize, Marshal.SizeOf(typeof(uint)), ComputeBufferType.Append);
		pool.SetCounterValue(0);
		alive = new ComputeBuffer(bufferSize, Marshal.SizeOf(typeof(uint)), ComputeBufferType.Append);
		alive.SetCounterValue(0);
		uploads = new ComputeBuffer(Mathf.Max((256 / threadCount) * threadCount, threadCount), Marshal.SizeOf(typeof(Vector4)));
		counter = new ComputeBuffer(4, sizeof(int), ComputeBufferType.IndirectArguments);
		counterArray = new int[] { 0, 1, 0, 0 };

		kParticles = new KinematicParticle[kinematicBufferSize];
		kGPUParticles = new GPUKinematicParticle[kinematicBufferSize];
		kForcesAndBuoyances = new Vector3[kinematicBufferSize];

		GameObject o = GameObject.CreatePrimitive(PrimitiveType.Quad);
		mesh = o.GetComponent<MeshFilter>().sharedMesh;
		GameObject.Destroy(o);
		uint[] argsData = new uint[] { mesh.GetIndexCount(0), 0, 0, 0, 0 };
		args.SetData(argsData);

		bounds = new Bounds(Vector3.zero, 256f * Vector3.one);

		computeShader.SetBuffer(initKernel, propParticles, particles);
		computeShader.SetBuffer(initKernel, propDead, pool);
		computeShader.Dispatch(initKernel, groupCount, 1, 1);
	}
	private void OnDisable() {
		particles.Dispose();
		cellOffsets.Dispose();
		args.Dispose();
		pool.Dispose();
		alive.Dispose();
		uploads.Dispose();
		counter.Dispose();

		mesh = null;
	}

	private void Update() {
		UpdateConstants();

		DispatchEmit();

		DispatchSort();
		DispatchResetCellOffsets();
		DispatchCalculateCellOffsets();
		DispatchCalculateDensity();
		DispatchCalculateForce();
		SetupKinematicParticles();
		DispatchKinematicParticles();
		StartCoroutine(UpdateKinematicParticles()); // Should be called in PreFixedUpdate once after each dispatch
		DispatchUpdate();

		counter.SetData(counterArray);
        ComputeBuffer.CopyCount(pool, counter, 0);
		counter.GetData(counterArray);
		poolCount = counterArray[0];

		ComputeBuffer.CopyCount(alive, args, Marshal.SizeOf(typeof(uint)));
		material.SetBuffer(propParticles, particles);
		material.SetBuffer(propAlive, alive);
		material.SetFloat("_Demultiplier", radius);
		Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, args, 0);
	}

	public void Emit(Vector2 position, Vector2 velocity) {
		if (emitList.Count < uploads.count && emitList.Count < poolCount) {
			emitList.Add(new Vector4(position.x, position.y, velocity.x, velocity.y) / radius);
		}
	}

	private void UpdateConstants() {
		computeShader.SetFloat("_RestDensity", restDensity);
		computeShader.SetFloat("_PressureConstant", pressureConstant);
		computeShader.SetFloat("_Viscosity", viscosity);
		computeShader.SetFloat("_KinematicViscosity", kinematicViscosity);
		computeShader.SetFloat("_Multiplier", 1f / radius);
		computeShader.SetFloat("_Demultiplier", radius);
		computeShader.SetFloat("_MinH", 42f / radius);
		computeShader.SetFloat("_MaxH", 128f / radius);
		computeShader.SetFloat("_DT", 0.016f);
		computeShader.SetVector("_TerrainDistanceFieldScale", terrainDistanceField.terrainDistanceFieldScale);
	}

	private void DispatchEmit() {
		if (emitList.Count > 0) {
			uploads.SetData(emitList);
			computeShader.SetInt("_EmitCount", emitList.Count);
			computeShader.SetFloat("_LifeTime", 3600);
			computeShader.SetBuffer(emitKernel, propUploads, uploads);
			computeShader.SetBuffer(emitKernel, propPool, pool);
			computeShader.SetBuffer(emitKernel, propParticles, particles);
			computeShader.Dispatch(emitKernel, uploads.count / threadCount, 1, 1);
			emitList.Clear();
		}
	}

	private void DispatchSort() {
		var count = bufferSize;

		computeShader.SetInt("_SortCount", count);
		for (var dim = 2; dim <= count; dim <<= 1) {
			computeShader.SetInt("_SortDim", dim);
			for (var block = dim >> 1; block > 0; block >>= 1) {
				computeShader.SetInt("_SortBlock", block);
				computeShader.SetBuffer(sortKernel, propParticles, particles);
				computeShader.Dispatch(sortKernel, groupCount, 1, 1);
			}
		}
	}

	private void DispatchResetCellOffsets() {
		computeShader.SetBuffer(resetCellOffsetsKernel, propCellOffsets, cellOffsets);
		computeShader.Dispatch(resetCellOffsetsKernel, cellOffsets.count / threadCount, 1, 1);
	}

	private void DispatchCalculateCellOffsets() {
		computeShader.SetBuffer(calculateCellOffsetsKernel, propParticles, particles);
		computeShader.SetBuffer(calculateCellOffsetsKernel, propCellOffsets, cellOffsets);
		computeShader.Dispatch(calculateCellOffsetsKernel, groupCount, 1, 1);
	}

	private void DispatchCalculateDensity() {
		computeShader.SetBuffer(calculateDensityKernel, propParticles, particles);
		computeShader.SetBuffer(calculateDensityKernel, propCellOffsets, cellOffsets);
		computeShader.Dispatch(calculateDensityKernel, groupCount, 1, 1);
	}

	private void DispatchCalculateForce() {
		computeShader.SetBuffer(calculateForceKernel, propParticles, particles);
		computeShader.SetBuffer(calculateForceKernel, propCellOffsets, cellOffsets);
		computeShader.Dispatch(calculateForceKernel, groupCount, 1, 1);
	}

	private void DispatchUpdate() {
		alive.SetCounterValue(0);
		computeShader.SetTexture(updateKernel, propTerrainDistanceField, terrainDistanceField.terrainDistanceField);
		computeShader.SetBuffer(updateKernel, propParticles, particles);
		computeShader.SetBuffer(updateKernel, propDead, pool);
		computeShader.SetBuffer(updateKernel, propAlive, alive);
		computeShader.Dispatch(updateKernel, groupCount, 1, 1);
	}

	private void SetupKinematicParticles() {
		WaterKinematicBody[] kinematicBodies = GameObject.FindObjectsOfType<WaterKinematicBody>();
		int index = 0;
		foreach (var kinematicBody in kinematicBodies)
		{
			kinematicBody.UpdateParticles();
			foreach (var particle in kinematicBody.particles)
			{
				if (index < kinematicBufferSize) {
					kParticles[index] = particle;
					kGPUParticles[index] = new GPUKinematicParticle() {
						position = particle.position,
						velocity = particle.velocity
					};
					index++;
				} else { break; }
			}
			if (index >= kinematicBufferSize) {
				Debug.LogWarning("Too many kinematic particles");
				break;
			}
		}
		kinematicNumAlive = index;
		kinematicParticles.SetData(kGPUParticles);
	}

	private void DispatchKinematicParticles() {
		computeShader.SetInt("_KinematicCount", kinematicNumAlive);
		computeShader.SetBuffer(calculateKinematicForceKernel, propKinematicParticles, kinematicParticles);
		computeShader.SetBuffer(calculateKinematicForceKernel, propKinematicForcesAndBuoyances, kinematicForcesAndBuoyances);
		computeShader.SetBuffer(calculateKinematicForceKernel, propParticles, particles);
		computeShader.SetBuffer(calculateKinematicForceKernel, propCellOffsets, cellOffsets);
		computeShader.Dispatch(calculateKinematicForceKernel, kinematicGroupCount, 1, 1);

		// TODO apply kinematic particles to particle forces
	}

	private IEnumerator UpdateKinematicParticles() {
		yield return new WaitForEndOfFrame();
		kinematicForcesAndBuoyances.GetData(kForcesAndBuoyances);
		KinematicParticle particle;
		for(int i = 0; i < kinematicNumAlive; i++)
		{
			particle = kParticles[i];
			particle.buoyance = kForcesAndBuoyances[i].z;
			particle.force = kForcesAndBuoyances[i];
		}
	}
}
