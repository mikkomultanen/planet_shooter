using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[RequireComponent (typeof(Explodable))]
public class CaveGenerator : ExplodableAddon
{
	private Explodable _explodable;

	void Awake ()
	{
		if (!Application.isEditor || EditorApplication.isPlaying) {
			Debug.Log ("Generate cave");
			_explodable = GetComponent<Explodable>();
			_explodable.explode ();
		}
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

			Explodable fragExp = fragment.AddComponent<Explodable>();
			fragExp.shatterType = Explodable.ShatterType.Voronoi;
			fragExp.allowRuntimeFragmentation = true;
			fragExp.extraPoints = 3;
			fragExp.fragmentLayer = _explodable.fragmentLayer;
			fragExp.sortingLayerName = _explodable.sortingLayerName;
			fragExp.orderInLayer = _explodable.orderInLayer;

			fragment.AddComponent<EarthBlock> ();

			fragment.layer = _explodable.gameObject.layer;
		}
	}
}

