using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gravity : MonoBehaviour
{
	private Rigidbody2D rb;
	private float gravityForceMagnitude;

	void Start ()
	{
		rb = gameObject.GetComponent<Rigidbody2D> ();
		gravityForceMagnitude = rb.gravityScale * rb.mass * (-9.81f);
	}

	void FixedUpdate ()
	{
		rb.AddForce (rb.position.normalized * gravityForceMagnitude);
	}
}
