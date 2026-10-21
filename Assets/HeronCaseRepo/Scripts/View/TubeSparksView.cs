using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// The celebration when a tube is solved. Built in code like the pour splash; every particle is emitted by hand in
// world space, with counts, sizes, spins and lifetimes drawn from wide ranges so no two bursts look alike. A few
// glints pop around the cork as it snaps in; at the top of the hop a cluster of flares lights up the mouth and stars
// keep popping up around the bottle for a moment. Everything is a plain white sparkle, nothing shoots up or rains down.
public sealed class TubeSparksView : MonoBehaviour
{
    // plain white: the sparks shine by brightness alone and never match a liquid
    private static readonly Color Spark = Color.white;

    private static readonly int SoftnessId = Shader.PropertyToID("_Softness");
    private static readonly int CoreId = Shader.PropertyToID("_Core");
    private static readonly int StarId = Shader.PropertyToID("_Star");
    private static readonly int TwinkleId = Shader.PropertyToID("_Twinkle");
    private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");

    // the twinkle reads a stable random per particle from TEXCOORD0.z
    private static readonly List<ParticleSystemVertexStream> RandomStreams = new List<ParticleSystemVertexStream>
    {
        ParticleSystemVertexStream.Position,
        ParticleSystemVertexStream.Color,
        ParticleSystemVertexStream.UV,
        ParticleSystemVertexStream.StableRandomX,
    };

    private readonly List<Material> _materials = new List<Material>();
    private ParticleSystem _stars;
    private ParticleSystem _flares;

    public void Setup(Material particleMaterial, int sortingOrder)
    {
        var flare = CreateMaterial(particleMaterial, StarId, 1f);
        var twinkle = CreateMaterial(particleMaterial, StarId, 1f);
        twinkle.SetFloat(TwinkleId, 1f);

        _stars = CreateStars("Stars", twinkle, sortingOrder + 1);
        _flares = CreateStars("Flares", flare, sortingOrder + 1);
    }

    private void OnDestroy()
    {
        for (var i = 0; i < _materials.Count; i++)
        {
            if (_materials[i] != null) Destroy(_materials[i]);
        }
    }

    // a few glints pop out around the cork as it snaps home
    public void Pop(Vector3 mouth, float unit)
    {
        Wake(_stars);
        var star = new ParticleSystem.EmitParams();
        var count = Random.Range(3, 6);
        var turn = Random.Range(0f, Mathf.PI * 2f);
        for (var i = 0; i < count; i++)
        {
            var a = turn + (i + Random.Range(-0.35f, 0.35f)) / count * Mathf.PI * 2f;
            var dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a) * 0.6f + 0.3f, 0f);
            star.position = mouth + dir * (Random.Range(0.3f, 0.6f) * unit);
            star.velocity = dir * (Random.Range(1f, 2.4f) * unit);
            star.rotation = Random.Range(0f, 90f);
            star.angularVelocity = Spin(60f, 260f);
            star.startSize = SizeIn(0.3f, 1f) * unit;
            star.startLifetime = Random.Range(0.25f, 0.5f);
            star.startColor = Spark;
            _stars.Emit(star, 1);
        }
    }

    // mouth: where the flares light up; center and extents: the world box the stars pop up in
    public void Burst(Vector3 mouth, Vector3 center, Vector3 extents, float unit)
    {
        EmitFlares(mouth, unit);
        StartCoroutine(PopStars(center, extents, unit));
    }

    // one big flash with a few smaller flares around it, each at its own size and spin
    private void EmitFlares(Vector3 mouth, float unit)
    {
        Wake(_flares);
        var flare = new ParticleSystem.EmitParams();
        flare.velocity = Vector3.zero;
        flare.position = mouth + new Vector3(0f, 0.15f * unit, 0f);
        flare.rotation = Random.Range(0f, 90f);
        flare.angularVelocity = Spin(40f, 120f);
        flare.startSize = Random.Range(3.4f, 4.6f) * unit;
        flare.startLifetime = Random.Range(0.3f, 0.4f);
        flare.startColor = Spark;
        _flares.Emit(flare, 1);

        var count = Random.Range(1, 3);
        for (var i = 0; i < count; i++)
        {
            flare.position = mouth + new Vector3(Random.Range(-0.9f, 0.9f), Random.Range(0f, 1.2f), 0f) * unit;
            flare.rotation = Random.Range(0f, 90f);
            flare.angularVelocity = Spin(60f, 200f);
            flare.startSize = SizeIn(0.9f, 2.2f) * unit;
            flare.startLifetime = Random.Range(0.2f, 0.35f);
            flare.startColor = Spark;
            _flares.Emit(flare, 1);
        }
    }

    // stars keep popping up around the bottle for a moment, at random spots, sizes and spins
    private IEnumerator PopStars(Vector3 center, Vector3 extents, float unit)
    {
        Wake(_stars);
        var star = new ParticleSystem.EmitParams();
        var count = Random.Range(4, 7);
        for (var i = 0; i < count; i++)
        {
            star.position = center + new Vector3(Random.Range(-1.5f, 1.5f) * extents.x, Random.Range(-0.7f, 1.2f) * extents.y, 0f);
            star.velocity = new Vector3(0f, Random.Range(0.1f, 0.7f) * unit, 0f);
            star.rotation = Random.Range(0f, 90f);
            star.angularVelocity = Spin(30f, 220f);
            star.startSize = SizeIn(0.35f, 1.5f) * unit;
            star.startLifetime = Random.Range(0.3f, 0.6f);
            star.startColor = Spark;
            _stars.Emit(star, 1);
            if (i >= 2) yield return new WaitForSeconds(Random.Range(0.02f, 0.09f));
        }
    }

    // most come out small and a few big, which reads richer than an even spread
    private static float SizeIn(float min, float max)
    {
        return Mathf.Lerp(min, max, Mathf.Pow(Random.value, 2.2f));
    }

    private static float Spin(float min, float max)
    {
        return Random.Range(min, max) * (Random.value < 0.5f ? -1f : 1f);
    }

    private static void Wake(ParticleSystem ps)
    {
        if (!ps.isPlaying) ps.Play();
    }

    // mode: the shader toggle that shapes the particle, or -1 for the plain soft dot
    private Material CreateMaterial(Material source, int mode, float core)
    {
        var material = new Material(source);
        if (mode >= 0) material.SetFloat(mode, 1f);
        material.SetFloat(CoreId, core);
        material.SetFloat(SoftnessId, 0.85f);
        // additive, so the sparks glow against the night sky
        material.SetFloat(DstBlendId, (float)BlendMode.One);
        _materials.Add(material);
        return material;
    }

    // pops in, then shrinks away while it spins
    private ParticleSystem CreateStars(string systemName, Material material, int sortingOrder)
    {
        var ps = CreateSystem(systemName, material, sortingOrder);
        var main = ps.main;
        main.maxParticles = 32;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 6f), new Keyframe(0.2f, 1f), new Keyframe(1f, 0f)));

        SetAlpha(ps, new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.9f, 0.55f), new GradientAlphaKey(0f, 1f));
        UseRandomStream(ps);
        return ps;
    }

    private static void SetAlpha(ParticleSystem ps, params GradientAlphaKey[] alphas)
    {
        var color = ps.colorOverLifetime;
        color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, alphas);
        color.color = gradient;
    }

    private static void UseRandomStream(ParticleSystem ps)
    {
        ps.GetComponent<ParticleSystemRenderer>().SetActiveVertexStreams(RandomStreams);
    }

    private ParticleSystem CreateSystem(string systemName, Material material, int sortingOrder)
    {
        var go = new GameObject(systemName);
        go.transform.SetParent(transform, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.playOnAwake = false;
        main.loop = true;
        main.duration = 1f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        // sizes and speeds already carry the tube scale, see Burst
        main.scalingMode = ParticleSystemScalingMode.Shape;

        var emission = ps.emission;
        emission.enabled = false;
        var shape = ps.shape;
        shape.enabled = false;

        var psRenderer = go.GetComponent<ParticleSystemRenderer>();
        psRenderer.sharedMaterial = material;
        psRenderer.sortingOrder = sortingOrder;
        psRenderer.renderMode = ParticleSystemRenderMode.Billboard;

        return ps;
    }
}
