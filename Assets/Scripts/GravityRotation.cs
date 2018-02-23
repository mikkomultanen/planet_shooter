using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GravityRotation : MonoBehaviour {

	private Rigidbody2D rb;
    private Vector2 previousPosition;

	void Start () {
		rb = gameObject.GetComponent<Rigidbody2D> ();
		previousPosition = rb.position;
	}
	
    void LateUpdate()
    {
        var h = rb.position.magnitude;
        var positionDelta = rb.position - previousPosition;
        previousPosition = rb.position;
        var positionNormalized = rb.position.normalized;
        var t = positionDelta - (positionNormalized * Vector2.Dot(positionDelta, positionNormalized));
        rb.rotation -= Mathf.Atan(t.magnitude / h) * Mathf.Sign(PSEdge.Cross(t, positionNormalized)) * Mathf.Rad2Deg;
    }
}
