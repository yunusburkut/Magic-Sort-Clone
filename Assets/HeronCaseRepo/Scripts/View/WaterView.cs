using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;

public class WaterView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color hiddenColor;

    private static readonly int RevealAmountId = Shader.PropertyToID("_RevealAmount");
    private static readonly int SurfaceId = Shader.PropertyToID("_Surface");
    private MaterialPropertyBlock _mpb;
    private float _revealAmount = 1f;
    private TweenScope _scope;
    private Tween _heightTween;

    private DOGetter<float> _heightGetter;
    private DOSetter<float> _heightSetter;
    private DOGetter<float> _revealGetter;

    public TweenCallback CachedDestroySelf { get; private set; }
    public Color Color { get; private set; }

    private void Awake()
    {
        _scope = new TweenScope();
        _mpb = new MaterialPropertyBlock();
        CachedDestroySelf = DestroySelf;
        _heightGetter = GetSpriteHeight;
        _heightSetter = SetSpriteHeight;
        _revealGetter = () => _revealAmount;
    }

    private float GetSpriteHeight() => spriteRenderer.size.y;

    private void SetSpriteHeight(float h)
    {
        var s = spriteRenderer.size;
        spriteRenderer.size = new Vector2(s.x, h);
    }

    private void OnDestroy()
    {
        _scope.KillAll();
    }

    private void DestroySelf()
    {
        Destroy(gameObject);
    }

    public void Init(Color color)
    {
        Color = color;
        spriteRenderer.color = color;
    }

    public void SetHidden(bool hidden)
    {
        spriteRenderer.color = hidden ? hiddenColor : Color;
    }

    public void SetSortingOrder(int order)
    {
        spriteRenderer.sortingOrder = order;
    }

    public void SetReveal(float amount)
    {
        _revealAmount = amount;
        spriteRenderer.GetPropertyBlock(_mpb);
        _mpb.SetFloat(RevealAmountId, _revealAmount);
        spriteRenderer.SetPropertyBlock(_mpb);
    }

    public void SetSurface(bool isSurface)
    {
        spriteRenderer.GetPropertyBlock(_mpb);
        _mpb.SetFloat(SurfaceId, isSurface ? 1f : 0f);
        spriteRenderer.SetPropertyBlock(_mpb);
    }

    public void SetHeight(float height)
    {
        var s = spriteRenderer.size;
        spriteRenderer.size = new Vector2(s.x, height);
    }

    public Tween AnimateHeightTo(float targetHeight, float targetCenterY, float duration, float delay = 0f)
    {
        _heightTween?.Kill(false);
        var seq = DOTween.Sequence();
        if (delay > 0f)
        {
            seq.AppendInterval(delay);
        }
        
        seq.Append(DOTween.To(_heightGetter, _heightSetter, targetHeight, duration).SetEase(Ease.OutCubic));
        seq.Join(transform.DOLocalMoveY(targetCenterY, duration).SetEase(Ease.OutCubic));
        _heightTween = _scope.Add(seq);
        return _heightTween;
    }

    public Tween AnimateRevealTo(float to, float duration)
    {
        var ease = to > _revealAmount ? Ease.OutCubic : Ease.InCubic;

        return _scope.Add(DOTween.To(_revealGetter, SetReveal, to, duration).SetEase(ease));
    }
}
