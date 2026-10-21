using DG.Tweening;
using UnityEngine;

public sealed class TubeAnimController
{
    private const int TiltSamples = 9;

    private readonly Transform _transform;
    private readonly SpriteRenderer _outline;
    private readonly GameSettings _settings;
    private readonly TweenScope _scope;

    private Sequence _pourSequence;
    private Tween _selectTween;
    private Tween _shakeTween;
    private Tween _solvedTween;
    private Tween _outlineTween;

    private Vector3 _restLocalPos;

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

    public TubeAnimController(Transform transform, SpriteRenderer outline, GameSettings settings, TweenScope scope)
    {
        _transform = transform;
        _outline = outline;
        _settings = settings;
        _scope = scope;
        _moveUpdate = UpdateMove;
        _flowUpdate = UpdateFlow;
        _returnUpdate = UpdateReturn;
    }

    public void SetRestLocalPos(Vector3 pos)
    {
        _restLocalPos = pos;
    }

    public void PlaySelect(bool selected)
    {
        _selectTween?.Kill(false);
        _outlineTween?.Kill(false);

        _outlineTween = _scope.Add(_outline.DOColor(selected ? _settings.OutlineColor : Color.clear, 0.1f));

        if (selected)
        {
            _selectTween = _scope.Add(DOTween.Sequence()
                .Append(_transform.DOLocalMoveY(_restLocalPos.y - _settings.AnticipationDip, _settings.AnticipationDuration).SetEase(Ease.OutQuad))
                .Append(_transform.DOLocalMoveY(_restLocalPos.y + _settings.LiftAmount, _settings.LiftDuration).SetEase(Ease.OutBack)));
        }
        else
        {
            _selectTween = _scope.Add(_transform.DOLocalMoveY(_restLocalPos.y, _settings.LiftDuration).SetEase(Ease.OutBounce));
        }
    }

    public void PlayShake(TweenCallback onComplete)
    {
        _selectTween?.Kill(false);
        _shakeTween?.Kill(false);

        var x = _restLocalPos.x;
        var d = _settings.ShakeMagnitude;
        var t = _settings.ShakeDuration;

        _shakeTween = _scope.Add(DOTween.Sequence()
            .Append(_transform.DOLocalMoveX(x + d, t).SetEase(Ease.OutExpo))
            .Append(_transform.DOLocalMoveX(x - d, t).SetEase(Ease.InOutSine))
            .Append(_transform.DOLocalMoveX(x + d * _settings.ShakeDecay1, t).SetEase(Ease.InOutSine))
            .Append(_transform.DOLocalMoveX(x - d * _settings.ShakeDecay2, t).SetEase(Ease.InOutSine))
            .Append(_transform.DOLocalMoveX(x, t).SetEase(Ease.OutSine))
            .OnComplete(onComplete));
    }

    public Tween PlayMarkSolved(float preDelay = 0f)
    {
        _selectTween?.Kill(false);
        _shakeTween?.Kill(false);
        _pourSequence?.Kill(false);
        _solvedTween?.Kill(false);

        _transform.localPosition = _restLocalPos;
        _transform.localRotation = Quaternion.identity;
        _transform.localScale = Vector3.one;

        _solvedTween = _scope.Add(DOTween.Sequence()
            .AppendInterval(preDelay)
            .Append(_transform.DOScale(new Vector3(_settings.SolvedSquashX, _settings.SolvedSquashY, 1f), _settings.SolvedSquashDuration).SetEase(Ease.OutQuad))
            .Append(_transform.DOScale(Vector3.one, _settings.SolvedBounceDuration).SetEase(Ease.OutElastic)));

        return _solvedTween;
    }

    // Moves the tube so its lowest lip hangs just above the target mouth, then tilts it exactly as far as the
    // remaining liquid needs to keep reaching the lip, pivoting around that lip while the liquid flows out.
    public void PlayPourInto(TubeLiquid liquid, TubeLiquid target, float amount, Vector3 restWorldPos, PourAnimCallbacks callbacks)
    {
        _selectTween?.Kill(false);
        _shakeTween?.Kill(false);
        _pourSequence?.Kill(false);

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

        var flowDuration = _settings.PourFlowBaseDuration + _settings.PourFlowDurationPerSlot * amount;

        _pourSequence = DOTween.Sequence();
        _pourSequence.Append(DOVirtual.Float(0f, 1f, _settings.PourMoveDuration, _moveUpdate).SetEase(Ease.Linear));
        _pourSequence.AppendCallback(callbacks.OnTransfer);
        _pourSequence.Append(DOVirtual.Float(0f, 1f, flowDuration, _flowUpdate).SetEase(Ease.InOutSine));
        _pourSequence.AppendCallback(callbacks.OnFlowEnd);
        _pourSequence.Append(DOVirtual.Float(0f, 1f, _settings.PourReturnDuration, _returnUpdate).SetEase(Ease.Linear));
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
