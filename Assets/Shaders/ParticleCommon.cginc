#ifndef PARTICLE_STRUCT
#define PARTICLE_STRUCT

struct Particle
{
    bool alive;
    float2 position;
    float2 velocity;
    float2 life; //x = age, y = lifetime
    float density;
};

#endif