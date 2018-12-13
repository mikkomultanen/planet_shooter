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
	}
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
	private int updateKernel;
	private int emitKernel;
	private int bufferSize;
	private ComputeBuffer particles;
	private ComputeBuffer args;
	private ComputeBuffer pool;
	private ComputeBuffer alive;
	private ComputeBuffer uploads;
	private Mesh mesh;
	private Bounds bounds;
	private uint x;
	private List<Vector4> emitList = new List<Vector4>();
	private void OnEnable() {
		initKernel = computeShader.FindKernel("Init");
		updateKernel = computeShader.FindKernel("Update");
		emitKernel = computeShader.FindKernel("Emit");

		uint y, z;
		computeShader.GetKernelThreadGroupSizes(initKernel, out x, out y, out z);
		
		bufferSize = (int)((maxParticles / x) * x);

		particles = new ComputeBuffer(bufferSize, Marshal.SizeOf(typeof(Particle)), ComputeBufferType.Default);
		args = new ComputeBuffer(5, Marshal.SizeOf(typeof(uint)), ComputeBufferType.IndirectArguments);
		pool = new ComputeBuffer(bufferSize, Marshal.SizeOf(typeof(uint)), ComputeBufferType.Append);
		pool.SetCounterValue(0);
		alive = new ComputeBuffer(bufferSize, Marshal.SizeOf(typeof(uint)), ComputeBufferType.Append);
		alive.SetCounterValue(0);
		uploads = new ComputeBuffer(256, Marshal.SizeOf(typeof(Vector4)));

		GameObject o = GameObject.CreatePrimitive(PrimitiveType.Quad);
		mesh = o.GetComponent<MeshFilter>().sharedMesh;
		GameObject.Destroy(o);
		uint[] argsData = new uint[] { mesh.GetIndexCount(0), 0, 0, 0, 0 };
		args.SetData(argsData);

		bounds = new Bounds(Vector3.zero, 256f * Vector3.one);

		computeShader.SetBuffer(initKernel, propParticles, particles);
		computeShader.SetBuffer(initKernel, propDead, pool);
		computeShader.Dispatch(initKernel, bufferSize / (int)x, 1, 1);
	}
	private void OnDisable() {
		particles.Dispose();
		args.Dispose();
		pool.Dispose();
		alive.Dispose();
		uploads.Dispose();

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
			computeShader.Dispatch(emitKernel, uploads.count / (int)x, 1, 1);
			emitList.Clear();
		}

		alive.SetCounterValue(0);
		computeShader.SetFloat("_MinH", 42f);
		computeShader.SetFloat("_MaxH", 128f);
		computeShader.SetFloat("_DT", Time.deltaTime);
		computeShader.SetVector("_TerrainDistanceFieldScale", terrainDistanceField.terrainDistanceFieldScale);
		computeShader.SetTexture(updateKernel, propTerrainDistanceField, terrainDistanceField.terrainDistanceField);
		computeShader.SetBuffer(updateKernel, propParticles, particles);
		computeShader.SetBuffer(updateKernel, propDead, pool);
		computeShader.SetBuffer(updateKernel, propAlive, alive);
		computeShader.Dispatch(updateKernel, bufferSize / (int)x, 1, 1);

		ComputeBuffer.CopyCount(alive, args, Marshal.SizeOf(typeof(uint)));
		material.SetBuffer(propParticles, particles);
		material.SetBuffer(propAlive, alive);
		Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, args, 0);
	}

	public void Emit(Vector2 position, Vector2 velocity) {
		if (emitList.Count < uploads.count) {
			emitList.Add(new Vector4(position.x, position.y, velocity.x, velocity.y));
		}
	}
}
