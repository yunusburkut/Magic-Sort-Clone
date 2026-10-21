using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// The celebration when a tube is solved. Built in code like the pour splash; every particle is emitted by hand in
// world space, with counts, sizes, spins and lifetimes drawn from wide ranges so no two bursts look alike. A few
// glints pop around the cork as it snaps in; at the top of the hop two layers of light rays open behind the bottle
// and turn against each other, a ring of light rolls out, streaks fan out of the mouth around a cluster of flares,
// stars keep popping up around the bottle for a moment and glitter drifts down twinkling.
public sealed class TubeSparksView : MonoBehaviour
{
    private const float RayCount = 28f;

    // white and a pale gold glint: the sparks shine by brightness alone and never match a liquid
    private static readonly Color Spark = Color.white;
    private static readonly Color Glint = new Color(1f, 0.933f, 0.757f, 1f);

    private static readonly int SoftnessId = Shader.PropertyToID("_Softness");
    private static readonly int CoreId = Shader.PropertyToID("_Core");
    private static readonly int StarId = Shader.PropertyToID("_Star");
    private static readonly int RingId = Shader.PropertyToID("_Ring");
    private static readonly int RingWidthId = Shader.PropertyToID("_RingWidth");
    private static readonly int RaysId = Shader.PropertyToID("_Rays");
    private static readonly int RayCountId = Shader.PropertyToID("_RayCount");
    private static readonly int TwinkleId = Shader.PropertyToID("_Twinkle");
    private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");

    // the sunburst and the twinkle read a stable random per particle from TEXCOORD0.z
    private static readonly List<ParticleSystemVertexStream> RandomStreams = new List<ParticleSystemVertexStream>
    {
        ParticleSystemVertexStream.Position,
        ParticleSystemVertexStream.Color,
        ParticleSystemVertexStream.UV,
        ParticleSystemVertexStream.StableRandomX,
    };

    private readonly List<Material> _materials = new List<Material>();
    private ParticleSystem _halo;
    private ParticleSystem _ring;
    private ParticleSystem _streaks;
    private ParticleSystem _dust;
    private ParticleSystem _glitter;
    private ParticleSystem _stars;
    private ParticleSystem _flares;

    // haloSortingOrder: the sunburst draws behind the bottles
    public void Setup(Material particleMaterial, int sortingOrder, int haloSortingOrder)
    {
        var halo = CreateMaterial(particleMaterial, RaysId, 0f);
        halo.SetFloat(RayCountId, RayCount);
        var ring = CreateMaterial(particleMaterial, RingId, 0f);
        ring.SetFloat(RingWidthId, 0.05f);
        var streak = CreateMaterial(particleMaterial, -1, 0.7f);
        var dust = CreateMaterial(particleMaterial, TwinkleId, 1f);
        var flare = CreateMaterial(particleMaterial, StarId, 1f);
        var twinkle = CreateMaterial(particleMaterial, StarId, 1f);
        twinkle.SetFloat(TwinkleId, 1f);

        _halo = CreateHalo(halo, haloSortingOrder);
        _ring = CreateRing(ring, sortingOrder);
        _streaks = CreateStreaks(streak, sortingOrder);
        _dust = CreateGlitter("Dust", dust, sortingOrder);
        _glitter = CreateGlitter("Glitter", twinkle, sortingOrder);
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
        var count = Random.Range(5, 9);
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
            star.startColor = Random.value < 0.4f ? Glint : Spark;
            _stars.Emit(star, 1);
        }
    }

    // mouth: where the streaks leave the bottle; center and extents: the world box the stars pop up in
    public void Burst(Vector3 mouth, Vector3 center, Vector3 extents, float unit)
    {
        EmitHalo(center, extents);
        EmitRing(center, extents);
        EmitStreaks(mouth, unit);
        EmitFlares(mouth, unit);
        EmitGlitter(mouth, unit);
        StartCoroutine(PopStars(center, extents, unit));
    }

    // two layers of rays turning against each other at their own speeds, so the light keeps shifting
    private void EmitHalo(Vector3 center, Vector3 extents)
    {
        Wake(_halo);
        var halo = new ParticleSystem.EmitParams();
        halo.position = center + new Vector3(0f, 0.2f * extents.y, 0f);
        var dir = Random.value < 0.5f ? -1f : 1f;
        for (var i = 0; i < 2; i++)
        {
            halo.rotation = Random.Range(0f, 360f);
            halo.angularVelocity = (i == 0 ? dir : -dir) * Random.Range(8f, 24f);
            halo.startSize = (i == 0 ? Random.Range(3.2f, 3.9f) : Random.Range(2.1f, 2.8f)) * extents.y;
            halo.startLifetime = Random.Range(1f, 1.35f);
            halo.startColor = i == 0 ? Glint : Spark;
            _halo.Emit(halo, 1);
        }
    }

    private void EmitRing(Vector3 center, Vector3 extents)
    {
        Wake(_ring);
        var ring = new ParticleSystem.EmitParams();
        ring.position = center;
        ring.startSize = Random.Range(2.6f, 3f) * extents.y;
        ring.startLifetime = Random.Range(0.28f, 0.36f);
        ring.startColor = Spark;
        _ring.Emit(ring, 1);
    }

    private void EmitStreaks(Vector3 mouth, float unit)
    {
        Wake(_streaks);
        var streak = new ParticleSystem.EmitParams();
        var count = Random.Range(30, 41);
        for (var i = 0; i < count; i++)
        {
            var a = Random.Range(-85f, 85f) * Mathf.Deg2Rad;
            streak.position = mouth + new Vector3(Random.Range(-0.35f, 0.35f) * unit, 0f, 0f);
            streak.velocity = new Vector3(Mathf.Sin(a), Mathf.Cos(a), 0f) * (Random.Range(3.5f, 12f) * unit);
            streak.startSize = SizeIn(0.08f, 0.32f) * unit;
            streak.startLifetime = Random.Range(0.4f, 0.9f);
            streak.startColor = Random.value < 0.3f ? Glint : Spark;
            _streaks.Emit(streak, 1);
        }
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
        flare.startLifetime = Random.Range(0.4f, 0.5f);
        flare.startColor = Spark;
        _flares.Emit(flare, 1);

        var count = Random.Range(2, 5);
        for (var i = 0; i < count; i++)
        {
            flare.position = mouth + new Vector3(Random.Range(-0.9f, 0.9f), Random.Range(0f, 1.2f), 0f) * unit;
            flare.rotation = Random.Range(0f, 90f);
            flare.angularVelocity = Spin(60f, 200f);
            flare.startSize = SizeIn(0.9f, 2.2f) * unit;
            flare.startLifetime = Random.Range(0.25f, 0.45f);
            flare.startColor = Random.value < 0.5f ? Glint : Spark;
            _flares.Emit(flare, 1);
        }
    }

    // thrown up out of the mouth, then it hangs and drifts down twinkling: star glints mixed with round specks
    private void EmitGlitter(Vector3 mouth, float unit)
    {
        Wake(_glitter);
        Wake(_dust);
        var glitter = new ParticleSystem.EmitParams();
        var count = Random.Range(26, 36);
        for (var i = 0; i < count; i++)
        {
            var a = Random.Range(-70f, 70f) * Mathf.Deg2Rad;
            glitter.position = mouth + new Vector3(Random.Range(-0.35f, 0.35f) * unit, 0f, 0f);
            glitter.velocity = new Vector3(Mathf.Sin(a) * 1.3f, Mathf.Cos(a), 0f) * (Random.Range(2.5f, 8f) * unit);
            glitter.rotation = Random.Range(0f, 90f);
            glitter.angularVelocity = Spin(90f, 420f);
            glitter.startLifetime = Random.Range(0.9f, 1.8f);
            glitter.startColor = Random.value < 0.35f ? Glint : Spark;
            if (Random.value < 0.4f)
            {
                glitter.startSize = SizeIn(0.12f, 0.3f) * unit;
                _dust.Emit(glitter, 1);
            }
            else
            {
                glitter.startSize = SizeIn(0.2f, 0.75f) * unit;
                _glitter.Emit(glitter, 1);
            }
        }
    }

    // stars keep popping up around the bottle for a moment, at random spots, sizes and spins
    private IEnumerator PopStars(Vector3 center, Vector3 extents, float unit)
    {
        Wake(_stars);
        var star = new ParticleSystem.EmitParams();
        var count = Random.Range(8, 13);
        for (var i = 0; i < count; i++)
        {
            star.position = center + new Vector3(Random.Range(-1.5f, 1.5f) * extents.x, Random.Range(-0.7f, 1.2f) * extents.y, 0f);
            star.velocity = new Vector3(0f, Random.Range(0.1f, 0.7f) * unit, 0f);
            star.rotation = Random.Range(0f, 90f);
            star.angularVelocity = Spin(30f, 220f);
            star.startSize = SizeIn(0.35f, 1.5f) * unit;
            star.startLifetime = Random.Range(0.4f, 1f);
            star.startColor = Random.value < 0.5f ? Glint : Spark;
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

    // opens fast, then keeps turning slowly while it fades
    private ParticleSystem CreateHalo(Material material, int sortingOrder)
    {
        var ps = CreateSystem("Sunburst", material, sortingOrder);
        var main = ps.main;
        main.maxParticles = 4;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.5f, 0f, 3f), new Keyframe(0.4f, 0.95f), new Keyframe(1f, 1.05f)));

        SetAlpha(ps, new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.7f, 0.12f), new GradientAlphaKey(0.45f, 0.55f),
            new GradientAlphaKey(0f, 1f));
        UseRandomStream(ps);
        return ps;
    }

    // rolls out fast and thins away
    private ParticleSystem CreateRing(Material material, int sortingOrder)
    {
        var ps = CreateSystem("Ring", material, sortingOrder);
        var main = ps.main;
        main.maxParticles = 2;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.2f, 0f, 3f), new Keyframe(1f, 1f, 0f, 0f)));

        SetAlpha(ps, new GradientAlphaKey(0.85f, 0f), new GradientAlphaKey(0.6f, 0.35f), new GradientAlphaKey(0f, 1f));
        UseRandomStream(ps);
        return ps;
    }

    private ParticleSystem CreateStreaks(Material material, int sortingOrder)
    {
        var ps = CreateSystem("Streaks", material, sortingOrder);

        var main = ps.main;
        main.gravityModifier = 0.7f;
        main.maxParticles = 64;

        SetDrag(ps, 2.5f);

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

        SetAlpha(ps, new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.55f), new GradientAlphaKey(0f, 1f));

        var psRenderer = ps.GetComponent<ParticleSystemRenderer>();
        psRenderer.renderMode = ParticleSystemRenderMode.Stretch;
        psRenderer.velocityScale = 0.05f;
        psRenderer.lengthScale = 2f;
        return ps;
    }

    // heavy drag stops the throw quickly, so the glitter hangs and then floats down slowly
    private ParticleSystem CreateGlitter(string systemName, Material material, int sortingOrder)
    {
        var ps = CreateSystem(systemName, material, sortingOrder);

        var main = ps.main;
        main.gravityModifier = 0.25f;
        main.maxParticles = 48;

        SetDrag(ps, 4f);

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.6f, 0f, 4f), new Keyframe(0.15f, 1f), new Keyframe(1f, 0.5f)));

        SetAlpha(ps, new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f));
        UseRandomStream(ps);
        return ps;
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

    private static void SetDrag(ParticleSystem ps, float drag)
    {
        var limit = ps.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.limit = 1000f;
        limit.dampen = 0f;
        limit.drag = drag;
        limit.multiplyDragByParticleSize = false;
        limit.multiplyDragByParticleVelocity = false;
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
