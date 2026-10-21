using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public class TubeView : MonoBehaviour, IPointerClickHandler
{
    [Header("References")]
    [SerializeField] private SpriteRenderer tubeRenderer;
    [SerializeField] private SpriteRenderer outlineRenderer;
    [SerializeField] private BoxCollider2D tubeCollider;
    [SerializeField] private ParticleSystem fillVFX;

    [Header("Liquid")]
    [SerializeField] private Material liquidMaterial;
    [SerializeField] private Material pourStreamMaterial;
    [SerializeField] private Material splashMaterial;
    [SerializeField] private int liquidSortingOrder = 10;
    [SerializeField] private int streamSortingOrder = 20;

    [Header("Bottle Interior (tube-local, at the default capacity)")]
    [SerializeField] private float bodyHalfWidth = 0.78f;
    [SerializeField] private float neckHalfWidth = 0.47f;
    [SerializeField] private float bottomY = -2.31f;
    [SerializeField] private float bottomCurveHeight = 0.38f;
    [SerializeField] private float shoulderY = 1.22f;
    [SerializeField] private float neckY = 1.6f;
    [SerializeField] private float lipY = 2.15f;
    [SerializeField] private float ellipseRatio = 0.3f;
    [SerializeField] private float bottomEllipseRatio = 0.12f;

    [Header("Settings")]
    [SerializeField] private GameSettings settings;
    [SerializeField] private int defaultCapacity = 3;

    private TubeWaterSlots _waterSlots;
    private TubeLiquid _liquid;
    private PourStreamView _stream;
    private bool _isSelected;
    private Vector3 _restLocalPos;
    private float _baseTubeHeight;

    private Renderer[] _sortedRenderers;
    private int[] _baseSortingOrders;

    private TubeView _pourTarget;
    private Color _pourColor;
    private int _pourAmount;
    private TweenCallback _cachedTransferWater;
    private TweenCallback<float> _cachedPourFlow;
    private TweenCallback _cachedPourFlowEnd;
    private TweenCallback _cachedInvokePourComplete;
    private TweenCallback _cachedInvokeShakeComplete;

    private TweenScope _scope;
    private TubeAnimController _anim;
    private Camera _mainCamera;

    public bool IsFull => _waterSlots.IsFull;

    public bool IsEmpty => _waterSlots.IsEmpty;

    public bool IsSolved { get; private set; }

    public bool IsSingleColor => _waterSlots.IsSingleColor;

    public Color TopColor => _waterSlots.TopColor;

    public int AvailableSlots => _waterSlots.AvailableSlots;

    public int TopColorCount => _waterSlots.TopColorCount;

    private void Awake()
    {
        _mainCamera = Camera.main;
        _scope = new TweenScope();
        _anim = new TubeAnimController(transform, outlineRenderer, settings, _scope);
        _cachedTransferWater = DoTransferWater;
        _cachedPourFlow = OnPourFlow;
        _cachedPourFlowEnd = OnPourFlowEnd;
        _cachedInvokePourComplete = InvokePourComplete;
        _cachedInvokeShakeComplete = InvokeShakeComplete;
        _baseTubeHeight = tubeRenderer.size.y;

        _liquid = CreateChild<TubeLiquid>("Liquid");
        _stream = CreateChild<PourStreamView>("PourStream");

        outlineRenderer.color = Color.clear;
    }

    private void OnDestroy()
    {
        _scope.KillAll();
    }

    private T CreateChild<T>(string childName) where T : Component
    {
        var go = new GameObject(childName);
        go.transform.SetParent(transform, false);
        return go.AddComponent<T>();
    }

    public void Init(TubeData data, WaterColorPalette palette)
    {
        var extraHeight = ResizeTube(data.capacity);
        _restLocalPos = transform.localPosition;
        _anim.SetRestLocalPos(_restLocalPos);

        _liquid.Setup(BuildLiquidShape(), settings, liquidMaterial, liquidSortingOrder);
        _stream.Setup(settings, pourStreamMaterial, splashMaterial, streamSortingOrder);
        CacheSortingOrders();

        _waterSlots = new TubeWaterSlots(_liquid, data.capacity);
        for (var i = 0; i < data.waters.Count; i++)
        {
            var entry = data.waters[i];
            _waterSlots.SpawnWater(palette.Get(entry.color), entry.modifier == WaterModifier.Hidden);
        }

        _waterSlots.RevealTopWater();

        if (data.modifier == TubeModifier.Cloak)
        {
            tubeCollider.enabled = false;
            var ctrl = GetComponentInChildren<TubeCloakController>();
            ctrl.Resize(extraHeight);
            ctrl.Activate(palette.Get(data.cloakTriggerColor), OnCloakLifted);
        }
    }

    // The sliced bottle sprite is always shorter than its borders, so Unity scales the whole bottle vertically.
    private LiquidShape BuildLiquidShape()
    {
        var v = tubeRenderer.size.y / _baseTubeHeight;
        return new LiquidShape
        {
            BodyHalfWidth = bodyHalfWidth,
            NeckHalfWidth = neckHalfWidth,
            BottomY = bottomY * v,
            BottomCurveHeight = bottomCurveHeight * v,
            ShoulderY = shoulderY * v,
            NeckY = neckY * v,
            LipY = lipY * v,
            EllipseRatio = ellipseRatio,
            BottomEllipseRatio = bottomEllipseRatio,
        };
    }

    // Everything but the stream draws in front of the other tubes while this one pours.
    private void CacheSortingOrders()
    {
        var all = GetComponentsInChildren<Renderer>(true);
        var count = 0;
        for (var i = 0; i < all.Length; i++)
        {
            if (!all[i].transform.IsChildOf(_stream.transform)) all[count++] = all[i];
        }

        _sortedRenderers = new Renderer[count];
        _baseSortingOrders = new int[count];
        for (var i = 0; i < count; i++)
        {
            _sortedRenderers[i] = all[i];
            _baseSortingOrders[i] = all[i].sortingOrder;
        }
    }

    private void SetSortingBoost(int boost)
    {
        for (var i = 0; i < _sortedRenderers.Length; i++)
        {
            _sortedRenderers[i].sortingOrder = _baseSortingOrders[i] + boost;
        }
    }

    private void OnCloakLifted()
    {
        tubeCollider.enabled = true;
    }

    private void DoTransferWater()
    {
        TransferWater(_pourTarget);
    }

    private void OnPourFlow(float poured)
    {
        _liquid.SetDrained(poured);
        _pourTarget._liquid.SetFilled(poured);
    }

    private void OnPourFlowEnd()
    {
        _liquid.EndDrain();
        _pourTarget._liquid.EndFill();
        _stream.End();
    }

    private void InvokePourComplete()
    {
        SetSortingBoost(0);
        EventBus<PourAnimationCompletedEvent>.Publish(new PourAnimationCompletedEvent { From = this, To = _pourTarget });
    }

    private void InvokeShakeComplete()
    {
        EventBus<ShakeCompletedEvent>.Publish(new ShakeCompletedEvent { Tube = this });
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        EventBus<TubeClickedEvent>.Publish(new TubeClickedEvent { Tube = this });
    }

    public void AccelerateCurrentPour(float timeScale)
    {
        _anim.AcceleratePour(timeScale);
    }

    public void SetSelected(bool selected)
    {
        _isSelected = selected;
        _anim.PlaySelect(selected);
        if (selected) HapticService.PlaySelect();
    }

    public void Shake()
    {
        float snapY;
        if (_isSelected)
        {
            snapY = _restLocalPos.y + settings.LiftAmount;
        }
        else
        {
            snapY = _restLocalPos.y;
        }

        transform.localPosition = new Vector3(_restLocalPos.x, snapY, _restLocalPos.z);
        _anim.PlayShake(_cachedInvokeShakeComplete);
    }

    public bool HasColor(Color color)
    {
        return _waterSlots.HasColor(color);
    }

    public void PlayEnterAnimation(float delay)
    {
        var worldRightX = _mainCamera.transform.position.x + _mainCamera.orthographicSize * _mainCamera.aspect + 2f;
        var localRightX = transform.parent.InverseTransformPoint(new Vector3(worldRightX, 0f, 0f)).x;

        transform.localPosition = new Vector3(localRightX, _restLocalPos.y, _restLocalPos.z);
        _scope.Add(transform.DOLocalMoveX(_restLocalPos.x, settings.EnterDuration)
            .SetDelay(delay)
            .SetEase(Ease.OutBack));
    }

    public Tween MarkSolved()
    {
        IsSolved = true;
        tubeCollider.enabled = false;
        EventBus<TubeSolvedEvent>.Publish(new TubeSolvedEvent { SolvedColor = TopColor });
        HapticService.PlayWin();

        if (fillVFX == null)
        {
            return _anim.PlayMarkSolved();
        }

        var main = fillVFX.main;
        main.startColor = TopColor;
        fillVFX.Play();
        return _anim.PlayMarkSolved(main.duration);
    }

    public void PourInto(TubeView target)
    {
        _pourTarget = target;
        _pourColor = TopColor;
        _pourAmount = Mathf.Min(TopColorCount, target.AvailableSlots);
        SetSortingBoost(settings.PourSortingBoost);

        var restWorldPos = transform.parent.TransformPoint(_restLocalPos);
        var callbacks = new PourAnimCallbacks(_cachedTransferWater, _cachedPourFlow, _cachedPourFlowEnd, _cachedInvokePourComplete);
        _anim.PlayPourInto(_liquid, target._liquid, _pourAmount, restWorldPos, callbacks);
    }

    private void TransferWater(TubeView target)
    {
        _waterSlots.RemoveTopWaters(_pourAmount);
        target._waterSlots.AddWaters(_pourColor, _pourAmount);

        _liquid.BeginDrain();
        target._liquid.BeginFill(_pourColor);
        _stream.Begin(_liquid, target._liquid, _anim.PourTiltSign, _pourColor);
    }

    private float ResizeTube(int capacity)
    {
        var extraHeight = (capacity - defaultCapacity) * settings.WaterSlotHeight;

        var size = tubeRenderer.size;
        tubeRenderer.size = new Vector2(size.x, size.y + extraHeight);

        tubeCollider.size = new Vector2(tubeCollider.size.x, tubeCollider.size.y + extraHeight);
        tubeCollider.offset = new Vector2(tubeCollider.offset.x, tubeCollider.offset.y + extraHeight * 0.5f);

        transform.localPosition -= new Vector3(0f, extraHeight * 0.5f, 0f);

        var t = settings.OutlineThickness;
        outlineRenderer.size = new Vector2(tubeRenderer.size.x + t * 2f, tubeRenderer.size.y + t * 2f);

        return extraHeight;
    }
}
