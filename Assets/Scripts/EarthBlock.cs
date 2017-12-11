using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof(Explodable))]
public class EarthBlock : ExplodableAddon
{
	private Rigidbody2D rb;
	private float health = 5f;

	public override void OnFragmentsGenerated (List<GameObject> fragments)
	{
		Explodable _explodable = GetComponent<Explodable> ();
		foreach (GameObject fragment in fragments) {
			Collider2D col = fragment.GetComponent<Collider2D> ();
			col.attachedRigidbody.useAutoMass = true;
			col.density = 100;

			fragment.AddComponent<Gravity> ();

			Explodable fragExp = fragment.AddComponent<Explodable> ();
			fragExp.shatterType = Explodable.ShatterType.Voronoi;
			fragExp.allowRuntimeFragmentation = true;
			fragExp.extraPoints = 0;
			fragExp.fragmentLayer = _explodable.fragmentLayer;
			fragExp.sortingLayerName = _explodable.sortingLayerName;
			fragExp.orderInLayer = _explodable.orderInLayer;

			fragment.AddComponent<EarthBlock> ();

			fragment.layer = _explodable.gameObject.layer;
		}
	}

	public void Start ()
	{
		rb = GetComponent<Rigidbody2D> ();
		health *= rb.mass / 500;
		if (rb.bodyType != RigidbodyType2D.Static) {
			StartCoroutine (waitAndExplode (rb.mass / 10));
		}
	}

	private IEnumerator waitAndExplode (float lifetime)
	{
		yield return new WaitForSecondsRealtime (lifetime);

		Destroy (gameObject);
		// TODO add particle effect
	}

	public void doDamage (float damage)
	{
		//health -= damage;
		if (health < damage) {//0f) {
			if (rb.bodyType != RigidbodyType2D.Static && rb.mass < 10) {
				Destroy (gameObject);
				// TODO add particle effect
			} else {
				GetComponent<Explodable> ().explode ();
			}
		}
	}

	void OnCollisionEnter2D (Collision2D collision)
	{
		if (rb.bodyType != RigidbodyType2D.Static) {
			doDamage (collision.relativeVelocity.sqrMagnitude / 100);
		}
	}
}

