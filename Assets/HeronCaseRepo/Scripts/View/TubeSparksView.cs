using UnityEngine;
using UnityEngine.Rendering;

// The burst when a tube is solved: glowing streaks fanning out of the mouth, one big flash and stars twinkling
// around the bottle. Built in code like the pour splash; every particle is emitted by hand in world space.
public sealed class TubeSparksView : MonoBehaviour
{
    private const int StreakCount = 30;
    private const int TwinkleCount = 8;

    private static readonly int SoftnessId = Shader.PropertyToID("_Softness");
    private static readonly int CoreId = Shader.PropertyToID("_Core");
    private static readonly int StarId = Shader.PropertyToID("_Star");
    private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");

    private ParticleSystem _streaks;
    private ParticleSystem _stars;
    private Material _streakMaterial;
    private Material _starMaterial;

    public void Setup(Material particleMaterial, int sortingOrder)
    {
        _streakMaterial = CreateMaterial(particleMaterial, 0f, 0.7f);
        _starMaterial = CreateMaterial(particleMaterial, 1f, 1f);
        _streaks = CreateStreaks(sortingOrder);
        _stars = CreateStars(sortingOrder + 1);
    }

    private void OnDestroy()
    {
        if (_streakMaterial != null) Destroy(_streakMaterial);
        if (_starMaterial != null) Destroy(_starMaterial);
    }

    // mouth: where the streaks leave the bottle; center and extents: the world box the stars twinkle in
    public void Burst(Vector3 mouth, Vector3 center, Vector3 extents, Color color, float unit)
    {
        if (!_streaks.isPlaying) _streaks.Play();
        if (!_stars.isPlaying) _stars.Play();

        var tint = Color.Lerp(color, Color.white, 0.15f);
        tint.a = 1f;
        var pale = Color.Lerp(color, Color.white, 0.45f);
        pale.a = 1f;

        var streak = new ParticleSystem.EmitParams();
        for (var i = 0; i < StreakCount; i++)
        {
            var a = Random.Range(-80f, 80f) * Mathf.Deg2Rad;
            streak.position = mouth + new Vector3(Random.Range(-0.35f, 0.35f) * unit, 0f, 0f);
            streak.velocity = new Vector3(Mathf.Sin(a), Mathf.Cos(a), 0f) * (Random.Range(4.5f, 10f) * unit);
            streak.startSize = Random.Range(0.14f, 0.26f) * unit;
            streak.startLifetime = Random.Range(0.45f, 0.8f);
            streak.startColor = i % 4 == 0 ? pale : tint;
            _streaks.Emit(streak, 1);
        }

        var star = new ParticleSystem.EmitParams();
        star.position = mouth + new Vector3(0f, 0.15f * unit, 0f);
        star.velocity = Vector3.zero;
        star.rotation = 0f;
        star.angularVelocity = 90f;
        star.startSize = 3.6f * unit;
        star.startLifetime = 0.4f;
        star.startColor = pale;
        _stars.Emit(star, 1);

        for (var i = 0; i < TwinkleCount; i++)
        {
            var spread = new Vector3(Random.Range(-1.4f, 1.4f) * extents.x, Random.Range(-0.6f, 1.1f) * extents.y, 0f);
            star.position = center + spread;
            star.velocity = new Vector3(0f, Random.Range(0.2f, 0.6f) * unit, 0f);
            star.rotation = Random.Range(-20f, 20f);
            star.angularVelocity = Random.Range(-120f, 120f);
            star.startSize = Random.Range(0.6f, 1.1f) * unit;
            star.startLifetime = Random.Range(0.5f, 0.95f);
            star.startColor = i % 2 == 0 ? pale : tint;
            _stars.Emit(star, 1);
        }
    }

    private static Material CreateMaterial(Material source, float star, float core)
    {
        var material = new Material(source);
        material.SetFloat(StarId, star);
        material.SetFloat(CoreId, core);
        material.SetFloat(SoftnessId, 0.85f);
        // additive, so the sparks glow against the night sky
        material.SetFloat(DstBlendId, (float)BlendMode.One);
        return material;
    }

    private ParticleSystem CreateStreaks(int sortingOrder)
    {
        var ps = CreateSystem("Streaks", _streakMaterial, sortingOrder);

        var main = ps.main;
        main.gravityModifier = 0.7f;
        main.maxParticles = 64;

        var limit = ps.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.limit = 1000f;
        limit.dampen = 0f;
        limit.drag = 2.5f;
        limit.multiplyDragByParticleSize = false;
        limit.multiplyDragByParticleVelocity = false;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

        var color = ps.colorOverLifetime;
        color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.55f), new GradientAlphaKey(0f, 1f) });
        color.color = gradient;

        var psRenderer = ps.GetComponent<ParticleSystemRenderer>();
        psRenderer.renderMode = ParticleSystemRenderMode.Stretch;
        psRenderer.velocityScale = 0.05f;
        psRenderer.lengthScale = 2f;
        return ps;
    }

    private ParticleSystem CreateStars(int sortingOrder)
    {
        var ps = CreateSystem("Stars", _starMaterial, sortingOrder);

        var main = ps.main;
        main.maxParticles = 16;

        // pops in, then shrinks away while it spins
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 6f), new Keyframe(0.2f, 1f), new Keyframe(1f, 0f)));

        var color = ps.colorOverLifetime;
        color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
        color.color = gradient;

        return ps;
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
