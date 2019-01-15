using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class WaterSystem : FluidSystem {
	public ParticleSystem steamSystem;

    public ParticleSystem fireSystem;

	private ParticleSystemRenderer steamSystemRenderer;
	private ParticleSystemRenderer fireSystemRenderer;
	void Start () {
		steamSystemRenderer = steamSystem.GetComponent<ParticleSystemRenderer>();
		fireSystemRenderer = fireSystem.GetComponent<ParticleSystemRenderer>();
	}

	public override void EmitWater(Vector2 position, Vector2 velocity)
	{
	}

	public override void EmitSteam(Vector2 position, Vector2 velocity)
	{
		var p = new ParticleSystem.EmitParams();
		p.position = position;
		p.velocity = velocity;
		steamSystem.Emit(p, 1);
	}

	public override void EmitFire(Vector2 position, Vector2 velocity)
	{
        var p = new ParticleSystem.EmitParams();
        p.velocity = velocity;
        p.position = position;
        fireSystem.Emit(p, 1);
	}

	public override void EmitExplosion(Vector2 position, float force, float lifeTime)
	{
	}

	public override void Render(CommandBuffer commandBuffer) {
	}

	public override void RenderSteam(CommandBuffer commandBuffer) {
		commandBuffer.DrawRenderer(steamSystemRenderer, steamSystemRenderer.sharedMaterial);
	}

	public override void RenderFire(CommandBuffer commandBuffer) {
		commandBuffer.DrawRenderer(fireSystemRenderer, fireSystemRenderer.sharedMaterial);
	}
}
