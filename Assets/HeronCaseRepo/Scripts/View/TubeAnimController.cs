using DG.Tweening;
using UnityEngine;

public sealed class TubeAnimController
{
    private const int TiltSamples = 9;
    private const float ImpactDuration = 0.08f;
    private const int ChargeShakes = 1;
    // share of the cork's pop-in it hangs over the mouth before it drops
    private const float CapHang = 0.6f;
    // the cork pops in tipped over and rights itself on the way down
    private const float CapTilt = 16f;
    private const float GlowInDuration = 0.15f;
    private const float GlowOutDuration = 0.25f;
    // the slide in: it arrives about halfway through, swings a touch past and settles
    private const float TravelDamping = 0.7f;
    private const float TravelFrequency = 1f;
    // back home the top rocks on the way it came and wobbles upright
    private const float LandLean = 4f;
    private const float LandWobbleDuration = 0.55f;
    private const float WobbleDamping = 0.4f;
    private const float WobbleFrequency = 2.2f;

    private readonly Transform _transform;
    private readonly Transform _body;
    private readonly TweenCallback<float> _applyGlow;
    private readonly TweenCallback<float> _setGlow;
    private readonly GameSettings _settings;
    private readonly TweenScope _scope;

    private Sequence _pourSequence;
    private Tween _selectTween;
    private Tween _shakeTween;
    private Tween _solvedTween;
    private Tween _glowTween;
    private Tween _enterTween;
    private Tween _bodyTween;
    private Tween _leanTween;
    private Tween _punchTween;

    private Vector3 _restLocalPos;

    // Squash & stretch lives on the Body child so it never fights the move and tilt tweens on the root.
    // The body keeps its area (width = 1 / stretch) and pivots on the bottle base, which stays planted.
    private float _stretch = 1f;
    private float _lean;
    // the tap's swell, a uniform scale over the squash & stretch so the two never fight
    private float _punch = 1f;
    // how far the selected glass glow is in, 0..1
    private float _glow;
    private float _bottomY;
    private readonly TweenCallback<float> _setStretch;
    private readonly TweenCallback<float> _setLean;
    private readonly TweenCallback<float> _setPunch;
    private readonly TweenCallback _landAfterPour;
    private readonly EaseFunction _settle;
    private readonly EaseFunction _wobble;
    private readonly SpringEase _travel;

    // the cork pushed into the mouth when the tube is solved; it pivots on its top edge. It falls behind the glass
    // as the whole cork and, once in, swaps to the seated sprite drawn over the glass with its hidden part faded.
    private SpriteRenderer _cap;
    private Transform _capTransform;
    private Sprite _capFlightSprite;
    private Sprite _capSeatedSprite;
    private int _capFlightOrder;
    private int _capSeatedOrder;
    private Vector3 _capRest;
    private Vector3 _capScale;
    private float _capHeight;
    private readonly TweenCallback<float> _setCapSquash;
    private readonly TweenCallback _seatCap;

    // pour state, read by the cached tween updates
    private readonly float[] _tilts = new float[TiltSamples];
    private readonly TweenCallback<float> _moveUpdate;
    private readonly TweenCallback<float> _flowUpdate;
    private readonly TweenCallback<float> _returnUpdate;
    private TweenCallback<float> _onFlow;
    private Vector3 _anchor;
    private Vector3 _lipLocal;
    private Vector3 _startPos;
    private float _startAngle;
    // the mouth's path into the pour: from where the wind-up leaves it, over a low arc, to the anchor
    private Vector3 _mouthStart;
    private Vector3 _mouthArc;
    private Vector3 _windUpOffset;
    private float _windUpLean;
    private float _windUpShare;
    private Vector3 _flowEndPos;
    private float _flowEndAngle;
    private Vector3 _restWorldPos;
    private float _pourAmount;

    public int PourTiltSign { get; private set; }

    // applyGlow lights the glass up for the selection, 0 off to 1 fully on
    public TubeAnimController(Transform transform, Transform body, TweenCallback<float> applyGlow, GameSettings settings, TweenScope scope)
    {
        _transform = transform;
        _body = body;
        _applyGlow = applyGlow;
        _setGlow = SetGlow;
        _settings = settings;
        _scope = scope;
        _moveUpdate = UpdateMove;
        _flowUpdate = UpdateFlow;
        _returnUpdate = UpdateReturn;
        _setStretch = SetStretch;
        _setLean = SetLean;
        _setPunch = SetPunch;
        _landAfterPour = LandAfterPour;
        _setCapSquash = SetCapSquash;
        _seatCap = SeatCap;
        _settle = new SpringEase(settings.SettleDamping, settings.SettleFrequency).Function;
        _wobble = new SpringEase(WobbleDamping, WobbleFrequency).Function;
        _travel = new SpringEase(TravelDamping, TravelFrequency);
    }

    public void SetRestLocalPos(Vector3 pos)
    {
        _restLocalPos = pos;
    }

    // Body-local height of the bottle base, the point squash & stretch pivots on.
    public void SetBottom(float bottomY)
    {
        _bottomY = bottomY;
        ApplyBody();
    }

    // Body-local rest pose of the cork's top edge and its height there, so a squash keeps its bottom in the mouth.
    // The renderer holds the flight sprite and the seated sorting order; flightOrder puts it behind the glass.
    public void SetCap(SpriteRenderer cap, Sprite seatedSprite, int flightOrder, Vector3 restLocalPos, Vector3 restScale, float height)
    {
        _cap = cap;
        _capTransform = cap.transform;
        _capFlightSprite = cap.sprite;
        _capSeatedSprite = seatedSprite;
        _capFlightOrder = flightOrder;
        _capSeatedOrder = cap.sortingOrder;
        _capRest = restLocalPos;
        _capScale = restScale;
        _capHeight = height;
    }

    public void PlaySelect(bool selected)
    {
        _selectTween?.Kill(false);
        _glowTween?.Kill(false);
        _bodyTween?.Kill(false);

        _glowTween = _scope.Add(DOVirtual.Float(_glow, selected ? 1f : 0f, selected ? GlowInDuration : GlowOutDuration, _setGlow)
            .SetEase(Ease.OutSine));

        var y = _restLocalPos.y;
        var dip = _settings.AnticipationDuration;
        var lift = _settings.LiftDuration;
        var crouch = _settings.SquashCrouch;
        var jump = _settings.SquashJumpStretch;

        if (selected)
        {
            // anticipation crouch, then it glides up stretched and eases back into shape at the top
            _selectTween = _scope.Add(DOTween.Sequence()
                .Append(_transform.DOLocalMoveY(y - _settings.AnticipationDip, dip).SetEase(Ease.OutSine))
                .Append(_transform.DOLocalMoveY(y + _settings.LiftAmount, lift).SetEase(Ease.OutBack, 1.2f)));

            _bodyTween = _scope.Add(DOTween.Sequence()
                .Append(StretchTo(_stretch, crouch, dip, Ease.OutSine))
                .Append(StretchTo(crouch, jump, lift * 0.5f, Ease.OutSine))
                .Append(StretchTo(jump, 1f, _settings.SquashSettleDuration, _settle)));
        }
        else
        {
            // drops back down, a little stretched by the speed, and gives softly when it lands
            var fall = lift * 1.3f;
            _selectTween = _scope.Add(_transform.DOLocalMoveY(y, fall).SetEase(Ease.InSine));

            _bodyTween = _scope.Add(DOTween.Sequence()
                .Append(StretchTo(_stretch, _settings.SquashFallStretch, fall, Ease.InSine))
                .Append(Land(_settings.SquashFallStretch, _settings.SquashLand)));
        }
    }

    // A tap that picks the tube swells it a touch from its base, and it springs back.
    public void PlayPunch()
    {
        _punchTween?.Kill(false);

        var peak = 1f + _settings.TapPunch;
        _punchTween = _scope.Add(DOTween.Sequence()
            .Append(DOVirtual.Float(_punch, peak, ImpactDuration, _setPunch).SetEase(Ease.OutSine))
            .Append(DOVirtual.Float(peak, 1f, _settings.TapPunchSettleDuration, _setPunch).SetEase(_settle)));
    }

    public void PlayShake(TweenCallback onComplete)
    {
        _selectTween?.Kill(false);
        _shakeTween?.Kill(false);
        _leanTween?.Kill(false);

        var x = _restLocalPos.x;
        var d = _settings.ShakeMagnitude;
        var t = _settings.ShakeDuration;
        var lean = _settings.ShakeLean;

        _shakeTween = _scope.Add(DOTween.Sequence()
            .Append(_transform.DOLocalMoveX(x + d, t).SetEase(Ease.OutSine))
            .Append(_transform.DOLocalMoveX(x - d, t).SetEase(Ease.InOutSine))
            .Append(_transform.DOLocalMoveX(x + d * _settings.ShakeDecay1, t).SetEase(Ease.InOutSine))
            .Append(_transform.DOLocalMoveX(x - d * _settings.ShakeDecay2, t).SetEase(Ease.InOutSine))
            .Append(_transform.DOLocalMoveX(x, t).SetEase(Ease.OutSine))
            .OnComplete(onComplete));

        // the top lags the base, so the bottle wags its head a beat behind the shake
        _leanTween = _scope.Add(DOTween.Sequence()
            .AppendInterval(t * 0.4f)
            .Append(LeanTo(_lean, lean, t, Ease.OutSine))
            .Append(LeanTo(lean, -lean, t, Ease.InOutSine))
            .Append(LeanTo(-lean, lean * _settings.ShakeDecay1, t, Ease.InOutSine))
            .Append(LeanTo(lean * _settings.ShakeDecay1, -lean * _settings.ShakeDecay2, t, Ease.InOutSine))
            .Append(LeanTo(-lean * _settings.ShakeDecay2, 0f, t * 3f, _settle)));
    }

    // Slides in from fromLocalX: the top trails behind while it moves, leans past a touch when it stops, then settles.
    public void PlayEnter(float fromLocalX, float delay)
    {
        _enterTween?.Kill(false);
        _leanTween?.Kill(false);
        _bodyTween?.Kill(false);

        _transform.localPosition = new Vector3(fromLocalX, _restLocalPos.y, _restLocalPos.z);

        var duration = _settings.EnterDuration;
        var trail = _settings.EnterLean * Mathf.Sign(_restLocalPos.x - fromLocalX);
        var squash = _settings.EnterSquash;

        _enterTween = _scope.Add(_transform.DOLocalMoveX(_restLocalPos.x, duration)
            .SetDelay(delay)
            .SetEase(_travel.Function));

        _leanTween = _scope.Add(DOTween.Sequence()
            .AppendInterval(delay)
            .Append(LeanTo(_lean, trail, duration * 0.2f, Ease.OutSine))
            .Append(LeanTo(trail, -trail * 0.35f, duration * 0.45f, Ease.InOutSine))
            .Append(LeanTo(-trail * 0.35f, 0f, _settings.SquashSettleDuration * 1.5f, _settle)));

        // stretched along the motion while it is fast, round again with a soft give when it arrives
        _bodyTween = _scope.Add(DOTween.Sequence()
            .AppendInterval(delay)
            .Append(StretchTo(_stretch, squash, duration * 0.2f, Ease.OutSine))
            .Append(StretchTo(squash, 1f, duration * 0.7f, _settle)));
    }

    // The tube that catches the stream gives a little under the weight once it lands; onLand fires then.
    public void PlayReceive(float delay, TweenCallback onLand)
    {
        _bodyTween?.Kill(false);

        var squash = _settings.SquashReceive;
        _bodyTween = _scope.Add(DOTween.Sequence()
            .AppendInterval(delay)
            .AppendCallback(onLand)
            .Append(StretchTo(_stretch, squash, ImpactDuration * 2f, Ease.OutSine))
            .Append(StretchTo(squash, 1f, _settings.SquashSettleDuration, _settle)));
    }

    // Anticipation (a slow sink into a deep crouch that trembles for a beat), a stretched hop that is round again at
    // the top, a squash on the landing and a soft spring settle. onSeat fires as the cork snaps in, onPeak at the top
    // of the hop. The returned tween carries no OnComplete of its own.
    public Tween PlayMarkSolved(TweenCallback onSeat, TweenCallback onPeak)
    {
        _selectTween?.Kill(false);
        _shakeTween?.Kill(false);
        _pourSequence?.Kill(false);
        _solvedTween?.Kill(false);
        _bodyTween?.Kill(false);

        var y = _restLocalPos.y;
        _transform.localPosition = new Vector3(_transform.localPosition.x, y, _restLocalPos.z);
        _transform.localRotation = Quaternion.identity;
        _transform.localScale = Vector3.one;

        var crouch = _settings.SolvedSquashY;
        var crouchTime = _settings.SolvedSquashDuration;
        var charge = _settings.SolvedChargeDuration;
        var tremble = _settings.SolvedChargeTremble;
        var stretch = _settings.SolvedStretch;
        var fall = _settings.SquashFallStretch;
        var land = _settings.SolvedLandSquash;
        var half = _settings.SolvedHopDuration * 0.5f;

        var seq = DOTween.Sequence();
        // the snap of the cap is what pushes the bottle into its crouch
        var t = _cap != null ? InsertCapSnap(seq) : 0f;
        seq.InsertCallback(t, onSeat);
        seq.Insert(t, StretchTo(_stretch, crouch, crouchTime, Ease.OutCubic));
        t += crouchTime;

        // it gathers itself in the crouch, then springs; the tremble ends back on the crouch
        seq.Insert(t, DOVirtual.Float(0f, 1f, charge, p => SetStretch(crouch + tremble * Mathf.Sin(p * Mathf.PI * 2f * ChargeShakes))).SetEase(Ease.Linear));
        t += charge;

        seq.Insert(t, _transform.DOLocalMoveY(y + _settings.SolvedHopHeight, half).SetEase(Ease.OutSine));
        seq.Insert(t, StretchTo(crouch, stretch, half * 0.3f, Ease.OutSine));
        seq.Insert(t + half * 0.3f, StretchTo(stretch, 1f, half * 0.7f, Ease.InOutSine));
        t += half;

        // the body is round again here, so the mouth the sparks leave from is where it looks
        seq.InsertCallback(t, onPeak);

        seq.Insert(t, _transform.DOLocalMoveY(y, half).SetEase(Ease.InSine));
        seq.Insert(t, StretchTo(1f, fall, half, Ease.InSine));
        t += half;

        seq.Insert(t, Land(fall, land, _settings.SolvedBounceDuration));

        _solvedTween = _scope.Add(seq);
        return _solvedTween;
    }

    // Moves the tube so its lowest lip hangs just above the target mouth, then tilts it exactly as far as the
    // remaining liquid needs to keep reaching the lip (never so far that the liquid underneath gets into the neck), pivoting around that lip (which slowly rises) while the liquid flows out.
    public void PlayPourInto(TubeLiquid liquid, TubeLiquid target, float amount, Vector3 restWorldPos, PourAnimCallbacks callbacks)
    {
        _selectTween?.Kill(false);
        _shakeTween?.Kill(false);
        _pourSequence?.Kill(false);
        _bodyTween?.Kill(false);
        _leanTween?.Kill(false);

        var isLeft = _transform.position.x < target.transform.position.x;
        PourTiltSign = isLeft ? -1 : 1;

        var volume = liquid.TotalVolume;
        var below = volume - liquid.TopVolume;
        for (var i = 0; i < TiltSamples; i++)
        {
            _tilts[i] = liquid.PourAngle(volume - amount * i / (TiltSamples - 1), below, PourTiltSign);
        }

        _pourAmount = amount;
        _onFlow = callbacks.OnFlow;
        _lipLocal = liquid.LipLocal(PourTiltSign);
        _anchor = target.MouthWorld + new Vector3(PourTiltSign * _settings.PourLipSideOffset, _settings.PourLipHeight, 0f);
        _startPos = _transform.position;
        _startAngle = Mathf.DeltaAngle(0f, _transform.eulerAngles.z);
        _restWorldPos = restWorldPos;

        var windUp = _settings.PourWindUpDuration;
        var move = _settings.PourMoveDuration;
        // each extra slot adds a little less time, so longer pours run faster
        var flowDuration = _settings.PourFlowBaseDuration + _settings.PourFlowDurationPerSlot * Mathf.Pow(amount, _settings.PourFlowSlotExponent);
        var crouch = _settings.SquashCrouch;
        var jump = _settings.SquashJumpStretch;

        // the wind-up draws back away from the target, a little down and leaning away from it
        var pull = windUp > 0f ? _settings.PourWindUpDistance : 0f;
        _windUpOffset = new Vector3(PourTiltSign * pull, -0.5f * pull, 0f);
        _windUpLean = windUp > 0f ? -PourTiltSign * _settings.PourWindUpLean : 0f;
        _windUpShare = windUp / (windUp + move);
        _mouthStart = _startPos + _windUpOffset + LipOffset(_startAngle + _windUpLean);
        _mouthArc = new Vector3((_mouthStart.x + _anchor.x) * 0.5f, Mathf.Max(_mouthStart.y, _anchor.y) + _settings.PourArcHeight, 0f);

        // it crouches into the wind-up and pushes off stretched; the body is back to rest before the lip has to meet the anchor
        var body = DOTween.Sequence()
            .Append(StretchTo(_stretch, crouch, windUp, Ease.OutSine))
            .Append(StretchTo(crouch, jump, move * 0.25f, Ease.OutSine))
            .Append(StretchTo(jump, 1f, move * 0.5f, Ease.InOutSine))
            .Insert(0f, LeanTo(_lean, 0f, (windUp + move) * 0.5f, Ease.OutSine));

        _pourSequence = DOTween.Sequence();
        _pourSequence.Append(DOVirtual.Float(0f, 1f, windUp + move, _moveUpdate).SetEase(Ease.Linear));
        _pourSequence.Join(body);
        _pourSequence.AppendCallback(callbacks.OnTransfer);
        _pourSequence.Append(DOVirtual.Float(0f, 1f, flowDuration, _flowUpdate).SetEase(Ease.Linear));
        _pourSequence.AppendCallback(callbacks.OnFlowEnd);
        _pourSequence.Append(DOVirtual.Float(0f, 1f, _settings.PourReturnDuration, _returnUpdate).SetEase(Ease.Linear));
        _pourSequence.AppendCallback(_landAfterPour);
        _pourSequence.OnComplete(callbacks.OnComplete);

        _scope.Add(_pourSequence);
    }

    public void AcceleratePour(float timeScale)
    {
        if (_pourSequence != null && _pourSequence.IsActive())
        {
            _pourSequence.timeScale = timeScale;
        }
    }

    // Runs on its own so the settle never holds up the pour completion: a soft squash, while the top rocks on the way
    // the tube flew home and wobbles back upright.
    private void LandAfterPour()
    {
        _bodyTween?.Kill(false);
        _bodyTween = _scope.Add(Land(_stretch, Mathf.Lerp(1f, _settings.SquashLand, 0.5f)));

        _leanTween?.Kill(false);
        var lean = -Mathf.Sign(_restWorldPos.x - _flowEndPos.x) * LandLean;
        _leanTween = _scope.Add(DOTween.Sequence()
            .Append(LeanTo(_lean, lean, ImpactDuration * 1.5f, Ease.OutSine))
            .Append(LeanTo(lean, 0f, LandWobbleDuration, _wobble)));
    }

    private Sequence Land(float from, float squash)
    {
        return Land(from, squash, _settings.SquashSettleDuration);
    }

    private Sequence Land(float from, float squash, float settle)
    {
        return DOTween.Sequence()
            .Append(StretchTo(from, squash, ImpactDuration, Ease.OutSine))
            .Append(StretchTo(squash, 1f, settle, _settle));
    }

    // The cork pops up tipped over the mouth, rights itself as it drops in and is pushed home with a squash that
    // springs back. Returns the time it lands.
    private float InsertCapSnap(Sequence seq)
    {
        var pop = _settings.CapPopDuration;
        var drop = _settings.CapDropDuration;
        var land = pop * CapHang + drop;

        _cap.sprite = _capFlightSprite;
        _cap.sortingOrder = _capFlightOrder;
        _cap.gameObject.SetActive(true);
        _capTransform.localPosition = _capRest + Vector3.up * _settings.CapDropHeight;
        _capTransform.localRotation = Quaternion.Euler(0f, 0f, CapTilt);
        _capTransform.localScale = Vector3.zero;

        seq.Insert(0f, _capTransform.DOScale(_capScale, pop).SetEase(Ease.OutBack));
        seq.Insert(0f, _capTransform.DOLocalRotate(Vector3.zero, land).SetEase(Ease.OutBack));
        seq.Insert(land - drop, _capTransform.DOLocalMove(_capRest, drop).SetEase(Ease.InQuad));
        seq.InsertCallback(land, _seatCap);
        seq.Insert(land, DOVirtual.Float(_settings.CapSnapSquash, 1f, _settings.CapSnapDuration, _setCapSquash).SetEase(_settle));
        return land;
    }

    private void SeatCap()
    {
        _cap.sprite = _capSeatedSprite;
        _cap.sortingOrder = _capSeatedOrder;
    }

    private void SetCapSquash(float squash)
    {
        _capTransform.localScale = new Vector3(_capScale.x / squash, _capScale.y * squash, 1f);
        _capTransform.localPosition = _capRest + Vector3.down * (_capHeight * (1f - squash));
    }

    private Tween StretchTo(float from, float to, float duration, Ease ease)
    {
        return DOVirtual.Float(from, to, duration, _setStretch).SetEase(ease);
    }

    private Tween StretchTo(float from, float to, float duration, EaseFunction ease)
    {
        return DOVirtual.Float(from, to, duration, _setStretch).SetEase(ease);
    }

    private Tween LeanTo(float from, float to, float duration, Ease ease)
    {
        return DOVirtual.Float(from, to, duration, _setLean).SetEase(ease);
    }

    private Tween LeanTo(float from, float to, float duration, EaseFunction ease)
    {
        return DOVirtual.Float(from, to, duration, _setLean).SetEase(ease);
    }

    private void SetGlow(float glow)
    {
        _glow = glow;
        _applyGlow(glow);
    }

    private void SetStretch(float stretch)
    {
        _stretch = stretch;
        ApplyBody();
    }

    private void SetLean(float lean)
    {
        _lean = lean;
        ApplyBody();
    }

    private void SetPunch(float punch)
    {
        _punch = punch;
        ApplyBody();
    }

    private void ApplyBody()
    {
        var s = Mathf.Max(0.1f, _stretch);
        var scale = new Vector3(_punch / s, _punch * s, 1f);
        var rotation = Quaternion.Euler(0f, 0f, _lean);
        var pivot = new Vector3(0f, _bottomY, 0f);
        _body.localScale = scale;
        _body.localRotation = rotation;
        _body.localPosition = pivot - rotation * Vector3.Scale(scale, pivot);
    }

    private Vector3 PoseFor(float angle)
    {
        return PoseAround(_anchor, angle);
    }

    // the tube position that puts its lip at the given point
    private Vector3 PoseAround(Vector3 lip, float angle)
    {
        var pos = lip - LipOffset(angle);
        pos.z = _startPos.z;
        return pos;
    }

    private Vector3 LipOffset(float angle)
    {
        return Quaternion.Euler(0f, 0f, angle) * Vector3.Scale(_lipLocal, _transform.lossyScale);
    }

    private void SetPose(Vector3 position, float angle)
    {
        _transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angle));
    }

    // A short wind-up, then the tube tips over mouth first, as if a hand lifted it by the base: it turns about its
    // mouth, so the base swings up, while the mouth glides over a low arc to the pour point at an even pace.
    private void UpdateMove(float p)
    {
        var a = _windUpShare;
        if (p < a)
        {
            var windUp = Mathf.Sin(p / a * Mathf.PI * 0.5f);
            SetPose(_startPos + _windUpOffset * windUp, _startAngle + _windUpLean * windUp);
            return;
        }

        var q = Mathf.Clamp01((p - a) / (1f - a));
        var angle = Mathf.LerpUnclamped(_startAngle + _windUpLean, _tilts[0], q);
        var mouth = QuadBezier(_mouthStart, _mouthArc, _anchor, q);
        SetPose(PoseAround(mouth, angle), angle);
    }

    // Like pouring from a jug: the flow picks up speed as it goes and the tube rises slowly, drawing the stream out.
    private void UpdateFlow(float p)
    {
        var v = EaseInOutSine(Mathf.Pow(p, _settings.PourFlowSpeedUp));
        _onFlow?.Invoke(_pourAmount * v);

        var f = v * (TiltSamples - 1);
        var i = Mathf.Min(Mathf.FloorToInt(f), TiltSamples - 2);
        var angle = Mathf.Lerp(_tilts[i], _tilts[i + 1], f - i);
        var pos = PoseFor(angle) + Vector3.up * (_settings.PourFlowRise * EaseInOutSine(p));
        SetPose(pos, angle);

        _flowEndPos = pos;
        _flowEndAngle = angle;
    }

    // The tube is upright before it gets home, which gives the liquid left inside a good slosh; both run at an even pace.
    private void UpdateReturn(float p)
    {
        var tilt = Mathf.Clamp01(p / 0.65f);
        var control = new Vector3((_flowEndPos.x + _restWorldPos.x) * 0.5f, Mathf.Max(_flowEndPos.y, _restWorldPos.y) + _settings.PourArcHeight * 0.4f, 0f);
        SetPose(QuadBezier(_flowEndPos, control, _restWorldPos, p), Mathf.LerpUnclamped(_flowEndAngle, 0f, tilt));
    }

    private static Vector3 QuadBezier(Vector3 a, Vector3 b, Vector3 c, float t)
    {
        var u = 1f - t;
        return u * u * a + 2f * u * t * b + t * t * c;
    }

    private static float EaseInOutSine(float t) => -(Mathf.Cos(Mathf.PI * t) - 1f) * 0.5f;
}
