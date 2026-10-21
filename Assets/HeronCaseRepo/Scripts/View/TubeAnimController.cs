using DG.Tweening;
using UnityEngine;

public sealed class TubeAnimController
{
    private const int TiltSamples = 9;
    private const float ImpactDuration = 0.08f;
    private const int ChargeShakes = 2;

    private readonly Transform _transform;
    private readonly Transform _body;
    private readonly SpriteRenderer _outline;
    private readonly GameSettings _settings;
    private readonly TweenScope _scope;

    private Sequence _pourSequence;
    private Tween _selectTween;
    private Tween _shakeTween;
    private Tween _solvedTween;
    private Tween _outlineTween;
    private Tween _enterTween;
    private Tween _bodyTween;
    private Tween _leanTween;

    private Vector3 _restLocalPos;

    // Squash & stretch lives on the Body child so it never fights the move and tilt tweens on the root.
    // The body keeps its area (width = 1 / stretch) and pivots on the bottle base, which stays planted.
    private float _stretch = 1f;
    private float _lean;
    private float _bottomY;
    private readonly TweenCallback<float> _setStretch;
    private readonly TweenCallback<float> _setLean;
    private readonly TweenCallback _landAfterPour;
    private readonly EaseFunction _settle;

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
    private Vector3 _arcControl;
    private Vector3 _pourPos;
    private Vector3 _flowEndPos;
    private float _flowEndAngle;
    private Vector3 _restWorldPos;
    private float _pourAmount;

    public int PourTiltSign { get; private set; }

    public TubeAnimController(Transform transform, Transform body, SpriteRenderer outline, GameSettings settings, TweenScope scope)
    {
        _transform = transform;
        _body = body;
        _outline = outline;
        _settings = settings;
        _scope = scope;
        _moveUpdate = UpdateMove;
        _flowUpdate = UpdateFlow;
        _returnUpdate = UpdateReturn;
        _setStretch = SetStretch;
        _setLean = SetLean;
        _landAfterPour = LandAfterPour;
        _settle = new SpringEase(settings.SettleDamping, settings.SettleFrequency).Function;
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

    public void PlaySelect(bool selected)
    {
        _selectTween?.Kill(false);
        _outlineTween?.Kill(false);
        _bodyTween?.Kill(false);

        _outlineTween = _scope.Add(_outline.DOColor(selected ? _settings.OutlineColor : Color.clear, 0.1f));

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
            .SetEase(Ease.OutBack, 1.1f));

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
    // the top, a squash on the landing and a soft spring settle. onPeak fires at the top of the hop. The returned
    // tween carries no OnComplete of its own.
    public Tween PlayMarkSolved(TweenCallback onPeak)
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
        var t = 0f;
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
    // remaining liquid needs to keep reaching the lip, pivoting around that lip while the liquid flows out.
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
        for (var i = 0; i < TiltSamples; i++)
        {
            _tilts[i] = liquid.LipAngle(volume - amount * i / (TiltSamples - 1), PourTiltSign);
        }

        _pourAmount = amount;
        _onFlow = callbacks.OnFlow;
        _lipLocal = liquid.LipLocal(PourTiltSign);
        _anchor = target.MouthWorld + new Vector3(PourTiltSign * _settings.PourLipSideOffset, _settings.PourLipHeight, 0f);
        _startPos = _transform.position;
        _startAngle = Mathf.DeltaAngle(0f, _transform.eulerAngles.z);
        _pourPos = PoseFor(_tilts[0]);
        _arcControl = new Vector3((_startPos.x + _pourPos.x) * 0.5f, Mathf.Max(_startPos.y, _pourPos.y) + _settings.PourArcHeight, 0f);
        _restWorldPos = restWorldPos;

        var move = _settings.PourMoveDuration;
        var flowDuration = _settings.PourFlowBaseDuration + _settings.PourFlowDurationPerSlot * amount;
        var crouch = _settings.SquashCrouch;
        var jump = _settings.SquashJumpStretch;

        // a quick push-off on the way over; the body is back to rest before the lip has to meet the anchor
        var body = DOTween.Sequence()
            .Append(StretchTo(_stretch, crouch, move * 0.2f, Ease.OutSine))
            .Append(StretchTo(crouch, jump, move * 0.2f, Ease.OutSine))
            .Append(StretchTo(jump, 1f, move * 0.4f, Ease.InOutSine))
            .Insert(0f, LeanTo(_lean, 0f, move * 0.5f, Ease.OutSine));

        _pourSequence = DOTween.Sequence();
        _pourSequence.Append(DOVirtual.Float(0f, 1f, move, _moveUpdate).SetEase(Ease.Linear));
        _pourSequence.Join(body);
        _pourSequence.AppendCallback(callbacks.OnTransfer);
        _pourSequence.Append(DOVirtual.Float(0f, 1f, flowDuration, _flowUpdate).SetEase(Ease.InOutSine));
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

    // Runs on its own so the settle never holds up the pour completion.
    private void LandAfterPour()
    {
        _bodyTween?.Kill(false);
        _bodyTween = _scope.Add(Land(_stretch, Mathf.Lerp(1f, _settings.SquashLand, 0.5f)));
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

    private void ApplyBody()
    {
        var s = Mathf.Max(0.1f, _stretch);
        var scale = new Vector3(1f / s, s, 1f);
        var rotation = Quaternion.Euler(0f, 0f, _lean);
        var pivot = new Vector3(0f, _bottomY, 0f);
        _body.localScale = scale;
        _body.localRotation = rotation;
        _body.localPosition = pivot - rotation * Vector3.Scale(scale, pivot);
    }

    private Vector3 PoseFor(float angle)
    {
        var lip = Vector3.Scale(_lipLocal, _transform.lossyScale);
        var pos = _anchor - Quaternion.Euler(0f, 0f, angle) * lip;
        pos.z = _startPos.z;
        return pos;
    }

    private void SetPose(Vector3 position, float angle)
    {
        _transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angle));
    }

    private void UpdateMove(float p)
    {
        var t = EaseInOutSine(p);
        var tilt = EaseInOutQuad(Mathf.Clamp01((p - 0.2f) / 0.8f));
        SetPose(QuadBezier(_startPos, _arcControl, _pourPos, t), Mathf.LerpUnclamped(_startAngle, _tilts[0], tilt));
    }

    private void UpdateFlow(float p)
    {
        _onFlow?.Invoke(_pourAmount * p);

        var f = p * (TiltSamples - 1);
        var i = Mathf.Min(Mathf.FloorToInt(f), TiltSamples - 2);
        var angle = Mathf.Lerp(_tilts[i], _tilts[i + 1], f - i);
        var pos = PoseFor(angle);
        SetPose(pos, angle);

        _flowEndPos = pos;
        _flowEndAngle = angle;
    }

    // The tube swings upright first and then glides home, which gives the liquid left inside a good slosh.
    private void UpdateReturn(float p)
    {
        var tilt = 1f - Mathf.Pow(1f - Mathf.Clamp01(p / 0.65f), 3f);
        var t = EaseInOutSine(p);
        var control = new Vector3((_flowEndPos.x + _restWorldPos.x) * 0.5f, Mathf.Max(_flowEndPos.y, _restWorldPos.y) + _settings.PourArcHeight * 0.4f, 0f);
        SetPose(QuadBezier(_flowEndPos, control, _restWorldPos, t), Mathf.LerpUnclamped(_flowEndAngle, 0f, tilt));
    }

    private static Vector3 QuadBezier(Vector3 a, Vector3 b, Vector3 c, float t)
    {
        var u = 1f - t;
        return u * u * a + 2f * u * t * b + t * t * c;
    }

    private static float EaseInOutSine(float t) => -(Mathf.Cos(Mathf.PI * t) - 1f) * 0.5f;

    private static float EaseInOutQuad(float t) => t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f;
}
