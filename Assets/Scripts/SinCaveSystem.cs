
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class Noise : System.Object
{
    [SerializeField]
    private float aMin;
    [SerializeField]
    private float aMax;
    [SerializeField]
    private float[] a;
    [SerializeField]
    private int[] f;
    [SerializeField]
    private float[] p;

    public Noise(float aMin, float aMax, int minF, int maxF, int n)
    {
        this.aMin = aMin;
        this.aMax = aMax;
        this.a = new float[n];
        this.f = new int[n];
        this.p = new float[n];
        float sumA = 0;
        for (int i = 0; i < n; i++)
        {
            this.a[i] = Random.Range(n - i - 0.9f, n - i);
            sumA += this.a[i];
            this.f[i] = Mathf.RoundToInt(Random.Range((i + 1) * minF, (i + 1) * maxF));
            this.p[i] = Random.Range(0, 2 * Mathf.PI);
        }
        float scale = 1.0f / sumA;
        for (int i = 0; i < n; i++)
        {
            this.a[i] *= scale;
        }
    }

    public float value(float angle)
    {
        float sum = 0;
        for (int i = 0; i < a.Length; i++)
        {
            sum += a[i] * Mathf.Sin(f[i] * angle + p[i]);
        }
        return Mathf.Lerp(aMin, aMax, 0.5f * sum + 0.5f);
    }
}

[System.Serializable]
public class Cave : System.Object
{
    [SerializeField]
    private float r = 1;
    [SerializeField]
    private Noise wave;
    [SerializeField]
    private Noise thickness;
    public Cave(float r, float aMin, float aMax, float tMin, float tMax)
    {
        this.r = r;
        this.wave = new Noise(aMin, aMax, 1, 5, 5);
        this.thickness = new Noise(tMin, tMax, 11, 17, 5);
    }

    public float ceilingMagnitude(float angle)
    {
        return waveValue(angle) + thicknessValue(angle) / 2;
    }

    public float centerMagnitude(float angle)
    {
        return waveValue(angle);
    }

    public float floorMagnitude(float angle)
    {
        return waveValue(angle) - thicknessValue(angle) / 2;
    }

    public float waveValue(float angle)
    {
        return wave.value(angle) + r;
    }

    public float thicknessValue(float angle)
    {
        return thickness.value(angle);
    }
}

public class SinCaveSystem : ICaveSystem
{
    private float outerRadius;
    private float innerRadius;
    private const float threshold = 1f;
    private List<Cave> caves = new List<Cave>();

    public SinCaveSystem(float outerRadius, float innerRadius)
    {
        this.outerRadius = outerRadius;
        this.innerRadius = innerRadius;
        float r = (innerRadius + outerRadius) / 2;
        float t = (outerRadius - innerRadius);
        caves.Clear();
        int n = Random.Range(2, 4);
        for (int i = 0; i < n; i++)
        {
            caves.Add(new Cave(r - t / 4 * i / (n - 1), -t / 4, t / 4 + t / 8, t / 8, t / 4));
        }
    }

    public bool insideCave(Vector2 coord)
    {
        return caveFieldValue(coord) > threshold;
    }

    public float caveFieldValue(Vector2 coord)
    {
        var magnitude = coord.magnitude;
        var innerValue = stepValue(magnitude - innerRadius);
        var outerValue = stepValue(outerRadius - magnitude);
        return caves.Select(c => caveFieldValue(c, coord)).Sum() + innerValue + outerValue;
    }

    private float caveFieldValue(Cave cave, Vector2 coord)
    {
        var angle = Mathf.Atan2(coord.x, coord.y);
        var magnitude = coord.magnitude;
        var d = Mathf.Abs(magnitude - cave.centerMagnitude(angle));
        return stepValue(d - cave.thicknessValue(angle) / 2);
    }

    private static float STEP_V = 5;
    private static float stepValue(float x)
    {
        return -Mathf.Sin(Mathf.Clamp(x / STEP_V, -1f, 1f) * Mathf.PI / 2) + 1;
    }

    private static Vector2 RandomPointOnUnitCircle()
    {
        float angle = Random.Range(0f, Mathf.PI * 2);
        float x = Mathf.Sin(angle);
        float y = Mathf.Cos(angle);
        return new Vector2(x, y);
    }
}