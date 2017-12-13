using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent (typeof(Explodable))]
public class CaveGenerator : ExplodableAddon
{
	private Explodable _explodable;

	void Awake ()
	{
		#if UNITY_EDITOR
		if (EditorApplication.isPlaying) {
			#endif
			Debug.Log ("Generate cave");
			_explodable = GetComponent<Explodable> ();
			_explodable.explode ();
			#if UNITY_EDITOR
		}
		#endif
	}

	public override void OnFragmentsGenerated (List<GameObject> fragments)
	{
		_explodable = GetComponent<Explodable> ();
		float mass;
		foreach (GameObject fragment in fragments) {
			Rigidbody2D fragRb = fragment.GetComponent<Rigidbody2D> ();
			fragRb.useAutoMass = true;
			mass = fragRb.mass * 100;
			fragRb.useAutoMass = false;
			fragRb.mass = mass;
			fragRb.bodyType = RigidbodyType2D.Static;

			Explodable fragExp = fragment.AddComponent<Explodable> ();
			fragExp.shatterType = Explodable.ShatterType.Voronoi;
			fragExp.allowRuntimeFragmentation = true;
			fragExp.extraPoints = 3;
			fragExp.fragmentLayer = _explodable.fragmentLayer;
			fragExp.sortingLayerName = _explodable.sortingLayerName;
			fragExp.orderInLayer = _explodable.orderInLayer;

			fragment.AddComponent<EarthBlock> ();
			fragment.AddComponent<ExplodeOnClick> ();
			fragment.layer = _explodable.gameObject.layer;
		}
	}
}

