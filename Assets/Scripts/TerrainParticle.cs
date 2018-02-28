using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TerrainParticle : MonoBehaviour
{

    ParticleSystem ps;

	private void Awake() {
        ps = GetComponent<ParticleSystem>();
	}
}
