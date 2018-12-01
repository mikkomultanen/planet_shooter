using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterEmitterSystem : MonoBehaviour {
	[Range(0f, 1f)]
	public float interval = 0.1f;
	public TerrainMesh terrainMesh;
	public WaterSystem waterSystem;
	private PSEdge[] emitters = new PSEdge[0];

	private void Start() {
		StartCoroutine(CreateEmitters());
		StartCoroutine(EmitWater());
	}

	private IEnumerator CreateEmitters()
	{
		yield return new WaitUntil(() => terrainMesh.Ready);
		emitters = new PSEdge[6];
		for (int i = 0; i < emitters.Length; i++)
		{
			emitters[i] = terrainMesh.randomCaveCeiling();
		}
	}

	private IEnumerator EmitWater() {
		while(true)
		{
			yield return new WaitForSeconds(interval);
			foreach (var emitter in emitters)
			{
				var position = Vector2.Lerp(emitter.v0, emitter.v1, Random.Range(0f, 1f));
				var direction = Vector2.Perpendicular(emitter.v0 - emitter.v1);
				if (Vector2.Dot(direction, position) > 0) {
					direction *= -1;
				}
				var directionNormalized = direction.normalized;
				waterSystem.EmitWater(position + 0.2f * directionNormalized, Random.Range(3f, 6f) * directionNormalized);
			}
		}
	}
}
