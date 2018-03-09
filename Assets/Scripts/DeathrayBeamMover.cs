using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DeathrayBeamMover : MonoBehaviour
{
    public ParticleSystem sparkles;
    public float radius;

    private void Start()
    {
        var ps = GetComponent<ParticleSystem>();
		var shape = ps.shape;
		sparkles.transform.localPosition = new Vector3(0, radius, 0);
		shape.radius = radius;
    }
}
