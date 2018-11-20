using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct KinematicParticle
{
	public float mass;
	public Vector2 relativePosition;
	// dynamic properties
	public Vector2 position;
	public Vector2 velocity;
	public float pressure;
}

[RequireComponent(typeof(Rigidbody2D))]
public class WaterKinematicBody : MonoBehaviour {

	public List<Vector2> points;
	public KinematicParticle[] particles;
	private Rigidbody2D rb;

	void Start () {
		rb = GetComponent<Rigidbody2D>();
		particles = new KinematicParticle[points.Count];
		int count = points.Count;
		float particleMass = rb.mass / count;
		for (int i = 0; i < count; i++)
		{
			particles[i] = new KinematicParticle();
			particles[i].mass = particleMass;
			particles[i].relativePosition = points[i];
			particles[i].pressure = 0;
		}
	}

	public void UpdateParticles() {
		int count = particles.Length;
		for (int i = 0; i < count; i++)
		{
			particles[i].position = rb.position + particles[i].relativePosition;
			particles[i].velocity = rb.velocity;
		}
	}

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (Application.isEditor)
        {
			Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.red;
			points.ForEach(p => Gizmos.DrawSphere(new Vector3(p.x, p.y, 0), 0.1f));
        }
    }
#endif
}
