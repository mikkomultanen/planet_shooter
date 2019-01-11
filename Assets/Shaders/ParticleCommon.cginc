#ifndef PARTICLE_STRUCT
#define PARTICLE_STRUCT

struct Particle
{
    uint flags;
    float2 position;
    float2 velocity;
    float2 life; //x = age, y = lifetime
    float density;
    float2 force;
};

struct KinematicParticle
{
    float2 position;
    float2 velocity;
};

struct KinematicParticleResult
{
    float2 force;
    uint flags;
};

struct Explosion
{
    float2 position;
    float force;
    float lifeTime;
};

#endif