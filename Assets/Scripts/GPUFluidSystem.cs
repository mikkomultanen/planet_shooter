using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

public class GPUFluidSystem : MonoBehaviour {
	private struct Particle
	{
		public bool alive;
		public Vector2 position;
		public Vector2 velocity;
		public Vector2 life; //x = age, y = lifetime
		public float density;
		public Vector2 force;
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
	public int maxParticles = 100000;
	public Material material;
	public TerrainDistanceField terrainDistanceField;
	private const string propParticles = "_Particles";
	private const string propDead = "_Dead";
	private const string propPool = "_Pool";
	private const string propAlive = "_Alive";
	private const string propUploads = "_Uploads";
	private const string propTerrainDistanceField = "_TerrainDistanceField";
	private int initKernel;
	private int calculateDensityKernel;
	private int calculateForceKernel;
	private int updateKernel;
	private int emitKernel;
	private int threadCount;
	private int groupCount;
	private int bufferSize;
	private ComputeBuffer particles;
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
	private void OnEnable() {
		initKernel = computeShader.FindKernel("Init");
		calculateDensityKernel = computeShader.FindKernel("CalculateDensity");
		calculateForceKernel = computeShader.FindKernel("CalculateForce");
		updateKernel = computeShader.FindKernel("Update");
		emitKernel = computeShader.FindKernel("Emit");

		uint x, y, z;
		computeShader.GetKernelThreadGroupSizes(initKernel, out x, out y, out z);
		
		threadCount = (int)x;
		groupCount = maxParticles / threadCount;
		bufferSize = groupCount * threadCount;

		particles = new ComputeBuffer(bufferSize, Marshal.SizeOf(typeof(Particle)), ComputeBufferType.Default);
		args = new ComputeBuffer(5, Marshal.SizeOf(typeof(uint)), ComputeBufferType.IndirectArguments);
		pool = new ComputeBuffer(bufferSize, Marshal.SizeOf(typeof(uint)), ComputeBufferType.Append);
		pool.SetCounterValue(0);
		alive = new ComputeBuffer(bufferSize, Marshal.SizeOf(typeof(uint)), ComputeBufferType.Append);
		alive.SetCounterValue(0);
		uploads = new ComputeBuffer(Mathf.Max((256 / threadCount) * threadCount, threadCount), Marshal.SizeOf(typeof(Vector4)));
		counter = new ComputeBuffer(4, sizeof(int), ComputeBufferType.IndirectArguments);
		counterArray = new int[] { 0, 1, 0, 0 };

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
		args.Dispose();
		pool.Dispose();
		alive.Dispose();
		uploads.Dispose();
		counter.Dispose();

		mesh = null;
	}

	private void Update() {
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

		computeShader.SetFloat("_RestDensity", restDensity);
		computeShader.SetFloat("_PressureConstant", pressureConstant);
		computeShader.SetFloat("_Viscosity", viscosity);
		computeShader.SetFloat("_Demultiplier", radius);
		computeShader.SetFloat("_MinH", 42f / radius);
		computeShader.SetFloat("_MaxH", 128f / radius);
		computeShader.SetFloat("_DT", 0.016f);
		computeShader.SetVector("_TerrainDistanceFieldScale", terrainDistanceField.terrainDistanceFieldScale);

		computeShader.SetBuffer(calculateDensityKernel, propParticles, particles);
		computeShader.Dispatch(calculateDensityKernel, groupCount, 1, 1);

		computeShader.SetBuffer(calculateForceKernel, propParticles, particles);
		computeShader.Dispatch(calculateForceKernel, groupCount, 1, 1);

		alive.SetCounterValue(0);
		computeShader.SetTexture(updateKernel, propTerrainDistanceField, terrainDistanceField.terrainDistanceField);
		computeShader.SetBuffer(updateKernel, propParticles, particles);
		computeShader.SetBuffer(updateKernel, propDead, pool);
		computeShader.SetBuffer(updateKernel, propAlive, alive);
		computeShader.Dispatch(updateKernel, groupCount, 1, 1);

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
}
