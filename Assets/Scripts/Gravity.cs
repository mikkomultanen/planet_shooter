using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gravity : MonoBehaviour {

	public float gravity = -9.81f;

	private Rigidbody2D rb;

	void Start () {
		rb = GetComponent<Rigidbody2D> ();
	}
	
	void FixedUpdate () {
		rb.AddForce (rb.position.normalized * gravity);
	}
}
