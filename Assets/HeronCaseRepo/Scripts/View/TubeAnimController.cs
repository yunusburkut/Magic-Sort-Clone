using DG.Tweening;
using UnityEngine;

public sealed class TubeAnimController
{
    private readonly Transform _transform;
    private readonly Transform _tubeHead;
    private readonly SpriteRenderer _outline;
    private readonly SpriteRenderer _line;
    private readonly GameSettings _settings;
    private readonly TweenScope _scope;

    private Sequence _pourSequence;
    private Tween _selectTween;
    private Tween _shakeTween;
    private Tween _solvedTween;
    private Tween _outlineTween;
    private Tween _lineTween;

    private Vector3 _restLocalPos;

    public TubeAnimController(Transform transform, Transform tubeHead, SpriteRenderer outline, SpriteRenderer line, GameSettings settings, TweenScope scope)
    {
        _transform = transform;
        _tubeHead = tubeHead;
        _outline = outline;
        _line = line;
        _settings = settings;
        _scope = scope;
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
        _transform.localScale = Vector3.one;

        _solvedTween = _scope.Add(DOTween.Sequence()
            .AppendInterval(preDelay)
            .Append(_transform.DOScale(new Vector3(_settings.SolvedSquashX, _settings.SolvedSquashY, 1f), _settings.SolvedSquashDuration).SetEase(Ease.OutQuad))
            .Append(_transform.DOScale(Vector3.one, _settings.SolvedBounceDuration).SetEase(Ease.OutElastic)));

        return _solvedTween;
    }

    public void PlayPourInto(TubeView target, Vector3 restWorldPos, PourAnimCallbacks callbacks)
    {
        _selectTween?.Kill(false);
        _shakeTween?.Kill(false);
        _pourSequence?.Kill(false);

        var isLeft = _transform.position.x < target.transform.position.x;
        var signedOffsetX = isLeft ? -_settings.PourOffsetX : _settings.PourOffsetX;
        var signedAngle = isLeft ? -_settings.PourAngle : _settings.PourAngle;

        // World-space offset from tube pivot to head — correct under any scale or capacity.
        var headOffsetX = _tubeHead.position.x - _transform.position.x;
        var headOffsetY = _tubeHead.position.y - _transform.position.y;

        var pourWorldPos = new Vector3(
            target.HeadWorldPos.x + signedOffsetX - headOffsetX,
            target.HeadWorldPos.y - headOffsetY + _settings.PourHeightOffset,
            0f
        );
        var arcPeak = new Vector3(
            (_transform.position.x + pourWorldPos.x) * 0.5f,
            Mathf.Max(_transform.position.y, pourWorldPos.y) + _settings.PourArcHeight,
            0f
        );
        var pourLiftPos = new Vector3(pourWorldPos.x, pourWorldPos.y + _settings.PourLiftAmount, 0f);

        _pourSequence = DOTween.Sequence();
        _pourSequence.Append(_transform.DOMove(arcPeak, _settings.PourDuration * 0.45f).SetEase(Ease.OutSine));
        _pourSequence.Append(_transform.DOMove(pourWorldPos, _settings.PourDuration * 0.55f).SetEase(Ease.InSine));
        _pourSequence.Append(_transform.DORotate(new Vector3(0f, 0f, signedAngle), _settings.PourDuration).SetEase(Ease.OutBack));
        _pourSequence.AppendCallback(callbacks.OnTransfer);
        _pourSequence.AppendCallback(callbacks.OnShowLine);
        _pourSequence.Append(_transform.DOMove(pourLiftPos, _settings.PourHoldDuration * 0.5f).SetEase(Ease.OutSine));
        _pourSequence.Append(_transform.DOMove(pourWorldPos, _settings.PourHoldDuration * 0.5f).SetEase(Ease.InSine));
        _pourSequence.AppendCallback(callbacks.OnHideLine);
        _pourSequence.Append(_transform.DORotate(Vector3.zero, _settings.PourDuration).SetEase(Ease.OutBack));
        _pourSequence.Append(_transform.DOMove(restWorldPos, _settings.PourDuration).SetEase(Ease.OutBack));
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

    public void ShowLine(Color color)
    {
        _lineTween?.Kill(false);
        // PourStream shader ties width to alpha, so fading in grows the stream from a trickle.
        _line.color = new Color(color.r, color.g, color.b, 0f);
        _lineTween = _scope.Add(_line.DOFade(1f, _settings.PourStreamInDuration).SetEase(Ease.OutQuad));
    }

    public void HideLine()
    {
        _lineTween?.Kill(false);
        _lineTween = _scope.Add(_line.DOFade(0f, _settings.PourDuration));
    }
}
