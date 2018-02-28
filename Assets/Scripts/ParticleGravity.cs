using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleGravity : MonoBehaviour
{

    ParticleSystem ps;
    ParticleSystem.Particle[] particles;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
		particles = new ParticleSystem.Particle[ps.main.maxParticles];
    }

    void Update()
    {
		int numParticlesAlive = ps.GetParticles(particles);
		float velocityDelta = -9.81f * Time.deltaTime;
		for (int i = 0; i < numParticlesAlive; i++) {
			particles[i].velocity += particles[i].position.normalized * velocityDelta;
		}
		ps.SetParticles(particles, numParticlesAlive);
    }
}
