using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class WaterKinematicBody : MonoBehaviour {

	public float buoyanceA;
	public Vector2[] points;
	public KinematicParticle[] particles;
	private Rigidbody2D rb;
	private float buoyanceForce;
	private Damageable damageable;

	private void Awake() {
		rb = GetComponent<Rigidbody2D>();
		particles = new KinematicParticle[points.Length];
		int count = points.Length;
		for (int i = 0; i < count; i++)
		{
			particles[i] = new KinematicParticle();
		}
		buoyanceForce = buoyanceA * rb.mass / count;
		damageable = GetComponent<Damageable>();
	}

	public void UpdateParticles() {
		int count = particles.Length;
		for (int i = 0; i < count; i++)
		{
			var point = points[i];
			particles[i].position = transform.TransformPoint(point.x, point.y, 0);
			particles[i].velocity = rb.velocity;
		}
	}

	private void FixedUpdate() {
		int count = particles.Length;
		Vector2 positionNormalized = rb.position.normalized;
		KinematicParticle particle;
		Vector2 point;
		Vector2 particlePosition;
		Vector2 force;
		float damage = 0;
		for (int i = 0; i < count; i++)
		{
			particle = particles[i];
			point = points[i];
			particlePosition = transform.TransformPoint(point.x, point.y, 0);
			force = buoyanceForce * particle.buoyance * positionNormalized;
			force += particle.force;
			damage = Mathf.Max(damage, particle.damage);
			rb.AddForceAtPosition(force, particlePosition);
		}
		if (damageable != null) {
			damageable.doDamage(Time.fixedDeltaTime * damage);
		}
	}

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (Application.isEditor)
        {
			Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.red;
			foreach (var p in points)
			{
				Gizmos.DrawSphere(new Vector3(p.x, p.y, 0), 0.1f);
			}
        }
    }
#endif
}
