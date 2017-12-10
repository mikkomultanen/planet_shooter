using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof(Explodable))]
public class EarthBlock : ExplodableAddon
{
	protected int recurringCount = 0;
	private Rigidbody2D rb;
	private float health = 5f;

	public override void OnFragmentsGenerated (List<GameObject> fragments)
	{
		Explodable _explodable = GetComponent<Explodable> ();
		foreach (GameObject fragment in fragments) {
			fragment.GetComponent<Rigidbody2D> ().useAutoMass = true;
			fragment.GetComponent<Collider2D> ().density = 100;

			fragment.AddComponent<Gravity> ();

			Explodable fragExp = fragment.AddComponent<Explodable> ();
			fragExp.shatterType = Explodable.ShatterType.Voronoi;
			fragExp.allowRuntimeFragmentation = true;
			fragExp.extraPoints = 0;
			fragExp.fragmentLayer = _explodable.fragmentLayer;
			fragExp.sortingLayerName = _explodable.sortingLayerName;
			fragExp.orderInLayer = _explodable.orderInLayer;

			EarthBlock earthBlock = fragment.AddComponent<EarthBlock> ();
			earthBlock.recurringCount = recurringCount + 1;

			fragment.layer = _explodable.gameObject.layer;
		}
	}

	public void Start ()
	{
		rb = GetComponent<Rigidbody2D> ();
		if (recurringCount > 0) {
			health /= (recurringCount + 1);
			StartCoroutine (waitAndExplode ());
		}
	}

	private IEnumerator waitAndExplode ()
	{
		yield return new WaitForSecondsRealtime (15f / (recurringCount * recurringCount));

		Destroy (gameObject);
		// TODO add particle effect
	}

	public void doDamage (float damage)
	{
		health -= damage;
		if (health < 0f) {
			if (recurringCount > 2) {
				Destroy (gameObject);
				// TODO add particle effect
			} else {
				GetComponent<Explodable> ().explode ();
			}
		}
	}

	void OnCollisionEnter2D (Collision2D collision)
	{
		if (recurringCount > 0) {
			float magnitude = collision.relativeVelocity.magnitude;
			float impact = magnitude * magnitude;
			if (impact > 100) {
				doDamage (impact / 100);
			}
		}
	}

	void Update ()
	{
		if (recurringCount > 0 && rb.IsSleeping ()) {
			Destroy (gameObject);
			// TODO add particle effect
		}
	}
}

