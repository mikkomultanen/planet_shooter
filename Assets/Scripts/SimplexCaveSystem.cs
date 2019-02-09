using UnityEngine;

public class SimplexCaveSystem : ICaveSystem
{
    private const float tiling = 6;
    private const int iterations = 1;
    private const float threshold = 0.1f;
    private const float thresholdAmplitude = 0.05f;
    private const float innerThreshold = 0;
    private const float outerThreshold = 0.95f;
    private float outerRadius;
    private float innerRadius;

    private FastNoise fastNoise = new FastNoise();

    public SimplexCaveSystem(float outerRadius, float innerRadius, int seed)
    {
        this.outerRadius = outerRadius;
        this.innerRadius = innerRadius;
        fastNoise.SetSeed(seed);
        fastNoise.SetFrequency(tiling / (outerRadius * 2));
    }

    private float fractalNoise(Vector2 position) {
        float o = 0;
        float w = 0.5f;
        float s = 1;
        for (int i = 0; i < iterations; i++) {
            Vector2 coord = position * s;
            float n = Mathf.Abs(fastNoise.GetSimplex(coord.x, coord.y));
            n = 1 - n;
            n *= n;
            n *= n;
            o += n * w;
            s *= 2.0f;
            w *= 0.5f;
        }
        return o;
    }

    private static float smoothstep(float edge0, float edge1, float x)
    {
        float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
        return t * t * (3.0f - 2.0f * t);
    }

    public bool insideCave(Vector2 coord)
    {
        float d = coord.magnitude;
        if (d <= innerRadius || d >= outerRadius) return true;
        float n = fractalNoise(coord);
        float a = smoothstep(innerThreshold, innerThreshold + 0.1f, d / outerRadius) * (1 - smoothstep(outerThreshold - 0.1f, outerThreshold, d / outerRadius));
        float v = fastNoise.GetSimplex(coord.x, coord.y);
        return threshold <= n * a - thresholdAmplitude * v;
    }
}
