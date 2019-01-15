using System;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class KinematicParticle
{
	public Vector2 position;
	public Vector2 velocity;
	public float buoyance;
	public float damage;
	public Vector2 force;
}

public abstract class FluidSystem : MonoBehaviour
{
    public abstract void EmitWater(Vector2 position, Vector2 velocity);
    public abstract void EmitSteam(Vector2 position, Vector2 velocity);
    public abstract void EmitFire(Vector2 position, Vector2 velocity);
	public abstract void EmitExplosion(Vector2 position, float force, float lifeTime);
    public abstract void Render(CommandBuffer commandBuffer);
    public abstract void RenderSteam(CommandBuffer commandBuffer);
    public abstract void RenderFire(CommandBuffer commandBuffer);
}
