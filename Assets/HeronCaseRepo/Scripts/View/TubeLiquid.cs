using System.Collections.Generic;
using UnityEngine;

// Draws every liquid layer of one tube with a single quad (Liquid.shader). The surface stays level in world space
// while the tube tilts, layer boundaries come from the volume each layer holds, and the whole column sloshes
// when the tube moves.
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public sealed class TubeLiquid : MonoBehaviour
{
    public const int MaxLayers = 8;

    private sealed class Layer
    {
        public Color Color;
        public bool Hidden;
        public float Volume;  // in slots
        public float Reveal;  // 0 shows the hidden color, 1 the real color
    }

    private static readonly int LayerColorsId = Shader.PropertyToID("_LayerColors");
    private static readonly int LayerBoundsId = Shader.PropertyToID("_LayerBounds");
    private static readonly int LayerCountId = Shader.PropertyToID("_LayerCount");
    private static readonly int FrameId = Shader.PropertyToID("_Frame");
    private static readonly int ShapeId = Shader.PropertyToID("_Shape");
    private static readonly int Shape2Id = Shader.PropertyToID("_Shape2");
    private static readonly int ImpactId = Shader.PropertyToID("_Impact");

    private readonly List<Layer> _layers = new List<Layer>();
    private readonly Vector4[] _colors = new Vector4[MaxLayers];
    private readonly Vector4[] _bounds = new Vector4[MaxLayers];
    private readonly Vector2[] _poly = new Vector2[LiquidShape.VertexCount];
    // a boundary only moves when the tube turns or the liquid under it changes, so each is kept with the frame and
    // area it was found for and reused until then
    private readonly float[] _boundArea = new float[MaxLayers];
    private Vector2 _boundUp;

    // how far below the neck the liquid under the poured one is kept while pouring
    private const float NeckClearance = 0.1f;

    private GameSettings _settings;
    private LiquidShape _shape;
    private Color _hiddenColor;
    private float _slotArea;
    private MeshRenderer _renderer;
    private MaterialPropertyBlock _mpb;
    private Mesh _mesh;

    private Layer _drainLayer;
    private float _drainFrom;
    private Layer _fillLayer;
    private float _fillFrom;

    // slosh: x is the surface slope along world-right, z the front/back tilt as a fraction of the ellipse height
    private Vector3 _lastPos;
    private Vector3 _lastVel;
    private float _lastAngle;
    private bool _hasLast;
    private float _sloshX, _sloshXVel;
    private float _sloshZ, _sloshZVel;
    private float _phaseSeed;

    private float _impactW;
    private float _impactStrength;
    private float _impactTarget;
    private float _bubbles;

    public float Angle => Mathf.DeltaAngle(0f, transform.eulerAngles.z);

    public float TotalVolume
    {
        get
        {
            var v = 0f;
            for (var i = 0; i < _layers.Count; i++) v += _layers[i].Volume;
            return v;
        }
    }

    public float TopVolume => _layers.Count > 0 ? _layers[_layers.Count - 1].Volume : 0f;

    public Vector3 MouthWorld =>transform.TransformPoint(new Vector3(0f, _shape.LipY, 0f));

    // Tube-local height of the top surface while the tube stands upright.
    public float SurfaceLocalY => LiquidMath.HeightForArea(_poly, Vector2.up, TotalVolume * _slotArea);

    public void Setup(LiquidShape shape, GameSettings settings, Material material, int sortingOrder)
    {
        _shape = shape;
        _settings = settings;
        _hiddenColor = settings.HiddenLiquidColor;
        _shape.FillPolygon(_poly);
        _slotArea = settings.WaterSlotHeight * shape.BodyHalfWidth * 2f;
        _phaseSeed = Random.value * 10f;

        _renderer = GetComponent<MeshRenderer>();
        _renderer.sharedMaterial = material;
        _renderer.sortingOrder = sortingOrder;
        // the block belongs to this renderer alone, so it is filled in place every frame and never read back;
        // the shape is set once here and stays in it
        _mpb = new MaterialPropertyBlock();
        _mpb.SetVector(ShapeId, new Vector4(_shape.BodyHalfWidth, _shape.NeckHalfWidth, _shape.BottomY, _shape.LipY));
        _mpb.SetVector(Shape2Id, new Vector4(_shape.ShoulderY, _shape.NeckY, _shape.EllipseRatio, _shape.BottomCurveHeight));
        ForgetBounds();

        BuildMesh();
        Push(Angle);
    }

    private void OnDestroy()
    {
        if (_mesh != null) Destroy(_mesh);
    }

    private void BuildMesh()
    {
        const float margin = 0.06f;
        var x0 = -_shape.BodyHalfWidth - margin;
        var x1 = _shape.BodyHalfWidth + margin;
        var y0 = _shape.BottomY - margin;
        var y1 = _shape.LipY + _shape.EllipseRatio * _shape.NeckHalfWidth + margin;

        _mesh = new Mesh { name = "TubeLiquidQuad" };
        _mesh.vertices = new[] { new Vector3(x0, y0), new Vector3(x1, y0), new Vector3(x1, y1), new Vector3(x0, y1) };
        _mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        _mesh.RecalculateBounds();
        GetComponent<MeshFilter>().sharedMesh = _mesh;
    }

    // ---- layers ---------------------------------------------------------------------------------------------

    public void Append(Color color, float volume, bool hidden)
    {
        if (_layers.Count > 0)
        {
            var top = _layers[_layers.Count - 1];
            if (top.Color == color && top.Hidden == hidden)
            {
                top.Volume += volume;
                return;
            }
        }

        if (_layers.Count >= MaxLayers) return;
        _layers.Add(new Layer { Color = color, Hidden = hidden, Volume = volume, Reveal = hidden ? 0f : 1f });
    }

    public void RevealTop(bool instant)
    {
        if (_layers.Count == 0) return;
        var top = _layers[_layers.Count - 1];
        top.Hidden = false;
        if (instant) top.Reveal = 1f;
    }

    public void BeginDrain()
    {
        _drainLayer = _layers.Count > 0 ? _layers[_layers.Count - 1] : null;
        _drainFrom = _drainLayer?.Volume ?? 0f;
    }

    public void SetDrained(float slots)
    {
        if (_drainLayer == null) return;
        _drainLayer.Volume = Mathf.Max(0f, _drainFrom - slots);
    }

    public void EndDrain()
    {
        if (_drainLayer == null) return;
        if (_drainLayer.Volume <= 1e-3f)
        {
            _layers.Remove(_drainLayer);
            // the layer underneath is on top now, so a hidden one starts revealing itself
            RevealTop(false);
        }
        _drainLayer = null;
    }

    public void BeginFill(Color color)
    {
        var top = _layers.Count > 0 ? _layers[_layers.Count - 1] : null;
        if (top != null && top.Color == color && !top.Hidden)
        {
            _fillLayer = top;
        }
        else
        {
            _fillLayer = new Layer { Color = color, Volume = 0f, Reveal = 1f };
            if (_layers.Count < MaxLayers) _layers.Add(_fillLayer);
        }
        _fillFrom = _fillLayer.Volume;
    }

    public void SetFilled(float slots)
    {
        if (_fillLayer == null) return;
        _fillLayer.Volume = _fillFrom + slots;
    }

    public void EndFill()
    {
        _fillLayer = null;
    }

    // ---- pour helpers ---------------------------------------------------------------------------------------

    // Tilt at which a level surface holding `volume` slots touches the lip; signed like transform z rotation.
    public float LipAngle(float volume, int tiltSign)
    {
        return LiquidMath.LipAngle(_poly, _shape, volume * _slotArea, tiltSign, _settings.PourMaxAngle);
    }

    // Tilt for pouring with `volume` slots left, `below` of them under the poured liquid: the lip angle, held back
    // where a thin poured layer would let the neck dip into the liquid underneath, so only the poured color is ever
    // at the mouth and every layer keeps its own thickness.
    public float PourAngle(float volume, float below, int tiltSign)
    {
        var angle = LipAngle(volume, tiltSign);
        if (below <= 1e-3f || NeckClear(angle, below)) return angle;

        var lo = 0f;
        var hi = angle;
        for (var i = 0; i < 20; i++)
        {
            var mid = (lo + hi) * 0.5f;
            if (NeckClear(mid, below)) lo = mid;
            else hi = mid;
        }
        return lo;
    }

    private bool NeckClear(float angle, float below)
    {
        var up = LiquidMath.LocalUp(angle);
        var neck = float.MaxValue;
        for (var c = 0; c < 4; c++) neck = Mathf.Min(neck, Vector2.Dot(NeckCorner(c), up));
        return LiquidMath.HeightForArea(_poly, up, below * _slotArea) <= neck - NeckClearance;
    }

    public Vector3 LipLocal(int tiltSign) => _shape.Lip(tiltSign);

    public Vector3 LipWorld(int tiltSign) => transform.TransformPoint(_shape.Lip(tiltSign));

    // Centre of the top surface under a world x, kept inside the mouth; the stream lands here.
    public Vector3 SurfaceWorld(float worldX, float mouthMargin)
    {
        var local = transform.InverseTransformPoint(new Vector3(worldX, transform.position.y, 0f));
        var maxX = Mathf.Max(0f, _shape.NeckHalfWidth - mouthMargin);
        local.x = Mathf.Clamp(local.x, -maxX, maxX);

        var up = LiquidMath.LocalUp(Angle);
        var h = LiquidMath.HeightForArea(_poly, up, TotalVolume * _slotArea);
        local.y = h + _sloshX * local.x;
        local.z = 0f;
        return transform.TransformPoint(local);
    }

    public void SetImpact(Vector3 worldPoint, float strength)
    {
        var local = (Vector2)transform.InverseTransformPoint(worldPoint);
        var right = LiquidMath.LocalRight(LiquidMath.LocalUp(Angle));
        _impactW = Vector2.Dot(local, right);
        _impactTarget = Mathf.Max(_impactTarget, strength);
    }

    // A knock from outside, like the tube hitting the table: the surface rocks to one side and wobbles front to back,
    // then the springs let it settle.
    public void Jolt(float strength)
    {
        var side = Random.value < 0.5f ? -1f : 1f;
        _sloshXVel += side * strength;
        _sloshZVel -= strength * 2.5f;
    }

    // ---- per frame ------------------------------------------------------------------------------------------

    private void LateUpdate()
    {
        var dt = Time.deltaTime;
        if (dt <= 0f || _settings == null) return;

        var angle = Angle;
        UpdateReveal(dt);
        UpdateSlosh(dt, angle);

        // the splash fades fast, the bubbles it stirred up linger
        _impactStrength = Mathf.MoveTowards(_impactStrength, _impactTarget, dt / 0.1f);
        _bubbles = Mathf.Max(_impactStrength, _bubbles - dt / 0.7f);
        if (_impactTarget > 0f)
        {
            // the falling stream keeps the surface stirred
            _sloshZVel += _impactTarget * Random.Range(-1f, 1f) * _settings.SloshImpactKick;
            _sloshXVel += _impactTarget * Random.Range(-1f, 1f) * _settings.SloshImpactKick * 0.1f;
        }
        _impactTarget = 0f;

        Push(angle);
    }

    private void UpdateReveal(float dt)
    {
        var speed = dt / Mathf.Max(0.01f, _settings.WaterRevealDuration);
        for (var i = 0; i < _layers.Count; i++)
        {
            var l = _layers[i];
            if (!l.Hidden && l.Reveal < 1f) l.Reveal = Mathf.Min(1f, l.Reveal + speed);
        }
    }

    // Two damped springs. Sideways acceleration and spin tip the surface left/right; the front/back tilt is driven
    // by that slope and lags it by a quarter period, so the surface swirls around the bottle instead of only rocking.
    private void UpdateSlosh(float dt, float angle)
    {
        var pos = transform.position;
        if (!_hasLast)
        {
            _lastPos = pos;
            _lastAngle = angle;
            _hasLast = true;
        }

        var vel = (pos - _lastPos) / dt;
        var acc = Vector3.ClampMagnitude((vel - _lastVel) / dt, _settings.SloshMaxAccel);
        var spin = Mathf.DeltaAngle(_lastAngle, angle) * Mathf.Deg2Rad / dt;
        _lastPos = pos;
        _lastVel = vel;
        _lastAngle = angle;

        var k = Mathf.Pow(2f * Mathf.PI * _settings.SloshFrequency, 2f);
        var c = 2f * _settings.SloshDamping * Mathf.Sqrt(k);
        var maxSlope = Mathf.Max(0.01f, _settings.SloshMaxSlope);
        var driveX = -acc.x * _settings.SloshAccelGain - spin * _settings.SloshSpinGain;
        var driveZBase = -acc.y * _settings.SloshVerticalGain;

        var steps = Mathf.Clamp(Mathf.CeilToInt(dt * 120f), 1, 8);
        var h = dt / steps;
        for (var s = 0; s < steps; s++)
        {
            _sloshXVel += (-k * _sloshX - c * _sloshXVel + driveX) * h;
            _sloshX += _sloshXVel * h;

            var driveZ = driveZBase + k * _settings.SloshSwirl * _sloshX / maxSlope;
            _sloshZVel += (-k * _sloshZ - c * _sloshZVel + driveZ) * h;
            _sloshZ += _sloshZVel * h;
        }

        if (Mathf.Abs(_sloshX) > maxSlope)
        {
            _sloshX = Mathf.Sign(_sloshX) * maxSlope;
            _sloshXVel *= 0.5f;
        }
        if (Mathf.Abs(_sloshZ) > 0.7f)
        {
            _sloshZ = Mathf.Sign(_sloshZ) * 0.7f;
            _sloshZVel *= 0.5f;
        }
    }

    private void Push(float angle)
    {
        var count = Mathf.Min(_layers.Count, MaxLayers);
        var up = LiquidMath.LocalUp(angle);
        var right = LiquidMath.LocalRight(up);
        if (up.x != _boundUp.x || up.y != _boundUp.y)
        {
            _boundUp = up;
            ForgetBounds();
        }
        var t = Time.time + _phaseSeed;

        // a gentle idle sway on top of the springs keeps the liquid alive at rest
        var idle = _settings.SloshIdle;
        var slope = _sloshX + idle * 0.15f * Mathf.Sin(t * 1.9f);
        var tilt = _sloshZ + idle * Mathf.Cos(t * 1.9f);

        var area = 0f;
        for (var i = 0; i < count; i++)
        {
            var l = _layers[i];
            area += l.Volume * _slotArea;
            if (area != _boundArea[i])
            {
                _boundArea[i] = area;
                var h = LiquidMath.HeightForArea(_poly, up, area);
                LiquidMath.Chord(_poly, up, h, out var wMin, out var wMax);
                var mid = (wMin + wMax) * 0.5f;
                var half = Mathf.Max(0.001f, (wMax - wMin) * 0.5f);
                var midLocal = up * h + right * mid;
                _bounds[i] = new Vector4(h, mid, half, _shape.EllipseAt(midLocal.y));
            }
            // vectors skip Unity's gamma-to-linear conversion, so the palette colors are converted here
            var col = Color.Lerp(_hiddenColor, l.Color, l.Reveal).linear;
            _colors[i] = new Vector4(col.r, col.g, col.b, 1f);
        }
        if (count >= 2) slope = NeckSlope(_bounds[count - 2], up, right, slope);

        var energy = 0.2f + Mathf.Clamp01(Mathf.Abs(_sloshXVel) * 0.5f + Mathf.Abs(_sloshZVel) * 0.12f + _impactStrength);

        _mpb.SetVectorArray(LayerColorsId, _colors);
        _mpb.SetVectorArray(LayerBoundsId, _bounds);
        _mpb.SetFloat(LayerCountId, count);
        _mpb.SetVector(FrameId, new Vector4(up.x, up.y, slope, tilt));
        _mpb.SetVector(ImpactId, new Vector4(_impactW, _impactStrength, energy, _bubbles));
        _renderer.SetPropertyBlock(_mpb);
    }

    // NaN matches no area, so every boundary is worked out again the next time it is used
    private void ForgetBounds()
    {
        for (var i = 0; i < MaxLayers; i++) _boundArea[i] = float.NaN;
    }

    // The slosh tips every boundary, and tipped to pour that can lift the liquid under the top one into the neck;
    // the slope is cut back there so the top liquid alone stays at the mouth.
    private float NeckSlope(Vector4 below, Vector2 up, Vector2 right, float slope)
    {
        for (var c = 0; c < 4; c++)
        {
            var corner = NeckCorner(c);
            var room = Vector2.Dot(corner, up) - NeckClearance * 0.25f - below.x;
            if (room <= 0f) continue;
            var dw = Vector2.Dot(corner, right) - below.y;
            if (dw > 1e-4f) slope = Mathf.Min(slope, room / dw);
            else if (dw < -1e-4f) slope = Mathf.Max(slope, room / dw);
        }
        return slope;
    }

    private Vector2 NeckCorner(int c)
    {
        return new Vector2((c & 1) == 0 ? -_shape.NeckHalfWidth : _shape.NeckHalfWidth, c < 2 ? _shape.NeckY : _shape.LipY);
    }
}
