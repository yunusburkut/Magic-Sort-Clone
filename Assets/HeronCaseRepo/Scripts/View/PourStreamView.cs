using UnityEngine;

// The liquid falling from a tilted tube's lip into the target, plus the mist and droplets where it lands.
// The ribbon is rebuilt every frame along a curve from the lip to the target surface: the head drops from the
// lip when the pour starts, the tail drops after it when the pour ends. Sizes are in tube-local units.
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public sealed class PourStreamView : MonoBehaviour
{
    private const int Segments = 28;
    private const int MistOrderOffset = 5;

    private static readonly int CapsId = Shader.PropertyToID("_Caps");
    private static readonly int SoftnessId = Shader.PropertyToID("_Softness");
    private static readonly int CoreId = Shader.PropertyToID("_Core");

    private GameSettings _settings;
    private TubeLiquid _source;
    private TubeLiquid _target;
    private int _tiltSign;
    private Color _color;

    private Mesh _mesh;
    private MeshRenderer _renderer;
    private MaterialPropertyBlock _mpb;
    private readonly Vector3[] _verts = new Vector3[(Segments + 1) * 2];
    private readonly Vector2[] _uv0 = new Vector2[(Segments + 1) * 2];
    private readonly Vector2[] _uv1 = new Vector2[(Segments + 1) * 2];
    private readonly Vector2[] _uv2 = new Vector2[(Segments + 1) * 2];
    private readonly Color[] _cols = new Color[(Segments + 1) * 2];

    private ParticleSystem _mist;
    private ParticleSystem _spray;
    private Material _mistMaterial;
    private Material _sprayMaterial;
    private ParticleSystem.EmitParams _emit;
    private readonly ParticleSystem.Particle[] _particles = new ParticleSystem.Particle[80];
    private float _mistDebt;
    private float _sprayDebt;
    private Vector3 _targetPos;

    private bool _active;
    private bool _ending;
    private float _headTime;
    private float _tailTime;
    private float _flow;
    private float _unit;
    private Vector3 _p0, _p1, _p2, _p3;

    public bool IsActive => _active;

    public void Setup(GameSettings settings, Material streamMaterial, Material splashMaterial, int sortingOrder)
    {
        _settings = settings;
        _renderer = GetComponent<MeshRenderer>();
        _renderer.sharedMaterial = streamMaterial;
        _renderer.sortingOrder = sortingOrder;
        _renderer.enabled = false;
        _mpb = new MaterialPropertyBlock();

        _mesh = new Mesh { name = "PourStreamRibbon" };
        _mesh.MarkDynamic();
        _mesh.vertices = _verts;
        var tris = new int[Segments * 6];
        for (var i = 0; i < Segments; i++)
        {
            var v = i * 2;
            var t = i * 6;
            tris[t] = v;
            tris[t + 1] = v + 2;
            tris[t + 2] = v + 1;
            tris[t + 3] = v + 1;
            tris[t + 4] = v + 2;
            tris[t + 5] = v + 3;
        }
        _mesh.triangles = tris;
        GetComponent<MeshFilter>().sharedMesh = _mesh;

        _mistMaterial = new Material(splashMaterial);
        _mistMaterial.SetFloat(SoftnessId, 0.9f);
        _sprayMaterial = new Material(splashMaterial);
        _sprayMaterial.SetFloat(SoftnessId, 0.25f);
        _sprayMaterial.SetFloat(CoreId, 0.15f);

        _mist = CreateMist(sortingOrder + MistOrderOffset);
        _spray = CreateSpray(sortingOrder + MistOrderOffset);
    }

    private void OnDestroy()
    {
        if (_mesh != null) Destroy(_mesh);
        if (_mistMaterial != null) Destroy(_mistMaterial);
        if (_sprayMaterial != null) Destroy(_sprayMaterial);
    }

    public void Begin(TubeLiquid source, TubeLiquid target, int tiltSign, Color color)
    {
        _source = source;
        _target = target;
        _tiltSign = tiltSign;
        // mesh vertex colors are not converted to linear by Unity
        _color = color.linear;
        _active = true;
        _ending = false;
        _headTime = 0f;
        _tailTime = 0f;
        _flow = 0f;
        _mistDebt = 0f;
        _sprayDebt = 0f;
        _targetPos = target.transform.position;
        _unit = Mathf.Abs(transform.lossyScale.x);
        BuildCurve(out _p0, out _p1, out _p2, out _p3);
        _renderer.enabled = true;

        // the splash is the poured liquid, so it takes its color instead of white
        var spray = _spray.main;
        spray.startColor = new ParticleSystem.MinMaxGradient(color, Color.Lerp(color, Color.white, 0.15f));
        var mist = _mist.main;
        mist.startColor = new Color(color.r, color.g, color.b, 0.75f);
    }

    public void End()
    {
        if (_active) _ending = true;
    }

    private void LateUpdate()
    {
        FollowTarget();
        if (!_active) return;

        var dt = Time.deltaTime;
        var fall = Mathf.Max(0.01f, _settings.StreamFallDuration);
        _unit = Mathf.Abs(transform.lossyScale.x);
        _headTime = Mathf.Min(1f, _headTime + dt / fall);
        _flow = Mathf.MoveTowards(_flow, _ending ? 0.75f : 1f, dt / 0.08f);
        if (_ending) _tailTime = Mathf.Min(1f, _tailTime + dt / fall);

        // falling accelerates, so the ends ease in
        var head = _headTime * _headTime;
        var tail = _tailTime * _tailTime;

        // once the pour ends the stream lets go of the lip and the rest of it falls along the last curve
        if (!_ending) BuildCurve(out _p0, out _p1, out _p2, out _p3);
        BuildMesh(_p0, _p1, _p2, _p3, tail, head);

        var landed = _headTime >= 1f && _tailTime < 1f;
        if (landed)
        {
            _target.SetImpact(_p3, _flow);
            Emit(_mist, ref _mistDebt, _settings.SplashMistRate * _flow * dt, 35f, 0.25f, 0.7f, 0.1f);
            Emit(_spray, ref _sprayDebt, _settings.SplashSprayRate * _flow * dt, 22f, 1f, 1.8f, 0.06f);
        }

        _renderer.GetPropertyBlock(_mpb);
        _mpb.SetVector(CapsId, new Vector4(_tailTime > 0f ? 1f : 0f, _headTime < 1f ? 1f : 0f, _headTime >= 1f ? 1f : 0f, 0f));
        _renderer.SetPropertyBlock(_mpb);

        if (_tailTime >= 1f)
        {
            _active = false;
            _renderer.enabled = false;
        }
    }

    // The target can be picked up again the moment the pour ends; the loose end of the stream and the splash
    // still in the air ride along with it instead of hanging where its liquid used to be.
    private void FollowTarget()
    {
        if (_target == null) return;

        var pos = _target.transform.position;
        var delta = pos - _targetPos;
        _targetPos = pos;
        if (delta.sqrMagnitude < 1e-8f) return;

        if (_ending)
        {
            _p0 += delta;
            _p1 += delta;
            _p2 += delta;
            _p3 += delta;
        }

        Shift(_mist, delta);
        Shift(_spray, delta);
    }

    private void Shift(ParticleSystem ps, Vector3 delta)
    {
        var count = ps.GetParticles(_particles);
        if (count == 0) return;
        for (var i = 0; i < count; i++)
        {
            _particles[i].position += delta;
        }
        ps.SetParticles(_particles, count);
    }

    // Emitted by hand at the landing point: the systems ride on the source tube, which is already swinging back
    // when the particle update runs, so their own emitters would scatter drops along its path.
    private void Emit(ParticleSystem ps, ref float debt, float count, float coneDeg, float minSpeed, float maxSpeed, float radius)
    {
        debt += count;
        var n = (int)debt;
        if (n <= 0) return;
        debt -= n;
        if (!ps.isPlaying) ps.Play();

        var size = ps.main.startSize;
        for (var i = 0; i < n; i++)
        {
            var a = Random.Range(-coneDeg, coneDeg) * Mathf.Deg2Rad;
            _emit.position = _p3 + new Vector3(Random.Range(-radius, radius) * _unit, 0f, 0f);
            _emit.velocity = new Vector3(Mathf.Sin(a), Mathf.Cos(a), 0f) * (Random.Range(minSpeed, maxSpeed) * _unit);
            _emit.startSize = Random.Range(size.constantMin, size.constantMax) * _unit;
            ps.Emit(_emit, 1);
        }
    }

    // Leaves the lip moving sideways, then gravity bends it straight down into the target mouth.
    private void BuildCurve(out Vector3 p0, out Vector3 p1, out Vector3 p2, out Vector3 p3)
    {
        p0 = _source.LipWorld(_tiltSign);
        p0.z = 0f;
        var toward = -_tiltSign * _unit;
        p3 = _target.SurfaceWorld(p0.x + toward * 0.08f, _settings.StreamMouthMargin);
        p3.z = 0f;
        var drop = Mathf.Max(0.2f * _unit, p0.y - p3.y);
        p1 = p0 + new Vector3(toward * 0.22f, -0.06f * _unit, 0f);
        p2 = new Vector3(p3.x, p0.y - drop * 0.35f, 0f);
    }

    private static Vector3 Bezier(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
    {
        var u = 1f - t;
        return u * u * u * a + 3f * u * u * t * b + 3f * u * t * t * c + t * t * t * d;
    }

    private static Vector3 BezierTangent(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
    {
        var u = 1f - t;
        return 3f * u * u * (b - a) + 6f * u * t * (c - b) + 3f * t * t * (d - c);
    }

    private void BuildMesh(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float tail, float head)
    {
        // rough arc length so the shader can scroll its pattern in tube-local units
        var length = 0f;
        var prev = p0;
        for (var i = 1; i <= 12; i++)
        {
            var p = Bezier(p0, p1, p2, p3, i / 12f);
            length += Vector3.Distance(prev, p);
            prev = p;
        }
        var invUnit = 1f / Mathf.Max(_unit, 1e-4f);

        var span = Mathf.Max(head - tail, 1e-4f);
        for (var i = 0; i <= Segments; i++)
        {
            var s = tail + span * i / Segments;
            var pos = Bezier(p0, p1, p2, p3, s);
            var tan = BezierTangent(p0, p1, p2, p3, s);
            if (tan.sqrMagnitude < 1e-8f) tan = Vector3.down;
            tan.Normalize();
            var normal = new Vector3(-tan.y, tan.x, 0f);

            // a wide sheet over the lip that narrows as it speeds up
            var hw = Mathf.Lerp(_settings.StreamLipWidth, _settings.StreamEndWidth, Mathf.SmoothStep(0f, 1f, s / 0.4f)) * 0.5f * _flow;
            const float pad = 1.15f;

            var dist = s * length * invUnit;
            var toHead = (head - s) * length * invUnit;
            var fromTail = (s - tail) * length * invUnit;

            var a = i * 2;
            var b = a + 1;
            _verts[a] = transform.InverseTransformPoint(pos - normal * (hw * pad * _unit));
            _verts[b] = transform.InverseTransformPoint(pos + normal * (hw * pad * _unit));
            _uv0[a] = new Vector2(-pad, s);
            _uv0[b] = new Vector2(pad, s);
            _uv1[a] = _uv1[b] = new Vector2(dist, toHead);
            _uv2[a] = _uv2[b] = new Vector2(hw, fromTail);
            _cols[a] = _cols[b] = _color;
        }

        _mesh.vertices = _verts;
        _mesh.uv = _uv0;
        _mesh.uv2 = _uv1;
        _mesh.uv3 = _uv2;
        _mesh.colors = _cols;
        _mesh.RecalculateBounds();
    }

    // ---- splash ---------------------------------------------------------------------------------------------

    private ParticleSystem CreateMist(int sortingOrder)
    {
        var ps = CreateSystem("SplashMist", _mistMaterial, sortingOrder);

        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.55f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new Color(1f, 1f, 1f, 0.75f);
        main.gravityModifier = -0.02f;
        main.maxParticles = 64;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.6f));

        var color = ps.colorOverLifetime;
        color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.8f, 0.15f), new GradientAlphaKey(0f, 1f) });
        color.color = gradient;

        var limit = ps.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.dampen = 0.15f;
        limit.limit = 0.2f;

        return ps;
    }

    private ParticleSystem CreateSpray(int sortingOrder)
    {
        var ps = CreateSystem("SplashSpray", _sprayMaterial, sortingOrder);

        var main = ps.main;
        // about one hop: the drops land back on the surface instead of sinking through the liquid
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.32f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.09f);
        main.gravityModifier = 1.2f;
        main.maxParticles = 80;

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
        // sizes and speeds already carry the tube scale, see Emit
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
