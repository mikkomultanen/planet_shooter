using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
[RequireComponent(typeof(LightSource))]
public class ParticleLight : MonoBehaviour {

	private ParticleSystem ps;
	private ParticleSystem.Particle[] particles;

	private LightSource ls;
	private float originalLightSize;
	// Use this for initialization
	void Start () {
		ps = GetComponent<ParticleSystem>();
        particles = new ParticleSystem.Particle[ps.main.maxParticles];
		ls = GetComponent<LightSource>();
		originalLightSize = ls.size;
	}
	
    private void LateUpdate()
    {
        // GetParticles is allocation free because we reuse the m_Particles buffer between updates
        int numParticlesAlive = ps.GetParticles(particles);

		Color color = new Color(0, 0, 0, 1);
		float size = 0;
        // Change only the particles that are alive
        for (int i = 0; i < numParticlesAlive; i++)
        {
			color += particles[i].GetCurrentColor(ps);
			size += particles[i].GetCurrentSize(ps);
        }
		if(numParticlesAlive > 0)
		{
			color /= numParticlesAlive;
			size /= numParticlesAlive;
		}
		ls.lightColor = color;
		ls.size = originalLightSize * size / ps.main.startSize.constant;
    }
}
