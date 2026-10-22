using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public class TubeView : MonoBehaviour, IPointerClickHandler
{
    private static readonly int GlowId = Shader.PropertyToID("_Glow");
    private static readonly int GlowLineId = Shader.PropertyToID("_GlowLine");
    private static readonly int FlashId = Shader.PropertyToID("_Flash");
    private static readonly int ShineId = Shader.PropertyToID("_Shine");
    private static readonly int SweepPeriodId = Shader.PropertyToID("_SweepPeriod");
    private static readonly int SweepStartId = Shader.PropertyToID("_SweepStart");

    private const float SolvedFlashDuration = 0.35f;
    private const float SolvedShineDuration = 0.5f;

    // the idle shine runs on each bottle's own clock: a rare pass at its own pace, the first one a random moment in
    private const float IdleShinePeriodMin = 15f;
    private const float IdleShinePeriodMax = 20f;
    private const float IdleShineMaxDelay = 10f;

    [Header("References")]
    // squash & stretch target holding the glass, cloak and liquid; the root only moves and tilts
    [SerializeField] private Transform body;
    [SerializeField] private SpriteRenderer tubeRenderer;
    [SerializeField] private BoxCollider2D tubeCollider;

    [Header("Liquid")]
    [SerializeField] private Material liquidMaterial;
    [SerializeField] private Material pourStreamMaterial;
    [SerializeField] private Material splashMaterial;
    [SerializeField] private int liquidSortingOrder = 10;
    [SerializeField] private int streamSortingOrder = 20;
    [SerializeField] private int sparkSortingOrder = 45;

    [Header("Cork (rendered on the bottle sprite's canvas, pivot on its top edge)")]
    [SerializeField] private Sprite capSprite;
    // the cork once pushed in: the part behind the lip faded, drawn over the glass
    [SerializeField] private Sprite capSeatedSprite;
    [SerializeField] private Material capMaterial;
    [SerializeField] private int capSortingOrder = 31;

    // Fitted by Tools/AssetGen/make_assets.py: the glass silhouette shrunk by the same gap along the walls and under
    // the bottom. The 9-sliced sprite only stretches its straight wall, so the bottom is measured from the sprite's
    // bottom edge and the shoulder, neck and lip from its top edge.
    [Header("Bottle Interior (tube-local)")]
    [SerializeField] private float bodyHalfWidth = 0.806f;
    [SerializeField] private float neckHalfWidth = 0.309f;
    [SerializeField] private float bottomInset = 0.134f;
    [SerializeField] private float bottomCurveHeight = 0.47f;
    [SerializeField] private float shoulderDrop = 1.185f;
    [SerializeField] private float neckDrop = 0.661f;
    [SerializeField] private float lipDrop = 0.467f;
    [SerializeField] private float ellipseRatio = 0.24f;
    [SerializeField] private float bottomEllipseRatio = 0.24f;

    [Header("Settings")]
    [SerializeField] private GameSettings settings;
    [SerializeField] private int defaultCapacity = 3;

    private TubeWaterSlots _waterSlots;
    private TubeLiquid _liquid;
    private PourStreamView _stream;
    private TubeSparksView _sparks;
    private SpriteRenderer _cap;
    private MaterialPropertyBlock _glassBlock;
    private bool _isSelected;
    private Vector3 _restLocalPos;

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
    private TweenCallback _cachedSolvedSeat;
    private TweenCallback _cachedSolvedPeak;
    private TweenCallback<float> _cachedSetGlassFlash;
    private TweenCallback<float> _cachedSetGlassShine;
    private TweenCallback _cachedPourLandHaptic;

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

    // off while the cloak hides the bottle and once it is solved
    public bool IsTappable => tubeCollider.enabled;

    // where the bottle stands in the layout, wherever its animations have taken it
    public Vector3 RestWorldPosition => transform.parent.TransformPoint(_restLocalPos);

    private void Awake()
    {
        _mainCamera = Camera.main;
        _scope = new TweenScope();
        _glassBlock = new MaterialPropertyBlock();
        _anim = new TubeAnimController(transform, body, SetGlassGlow, settings, _scope);
        _cachedTransferWater = DoTransferWater;
        _cachedPourFlow = OnPourFlow;
        _cachedPourFlowEnd = OnPourFlowEnd;
        _cachedInvokePourComplete = InvokePourComplete;
        _cachedInvokeShakeComplete = InvokeShakeComplete;
        _cachedSolvedSeat = OnSolvedSeat;
        _cachedSolvedPeak = OnSolvedPeak;
        _cachedSetGlassFlash = SetGlassFlash;
        _cachedSetGlassShine = SetGlassShine;
        _cachedPourLandHaptic = HapticService.PlayPourLand;

        _liquid = CreateChild<TubeLiquid>("Liquid", body);
        _stream = CreateChild<PourStreamView>("PourStream", transform);
        _sparks = CreateChild<TubeSparksView>("SolvedSparks", transform);

        _cap = CreateChild<SpriteRenderer>("Cap", body);
        _cap.sprite = capSprite;
        _cap.sharedMaterial = capMaterial;
        _cap.sortingOrder = capSortingOrder;
        _cap.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        _scope.KillAll();
    }

    private static T CreateChild<T>(string childName, Transform parent) where T : Component
    {
        var go = new GameObject(childName);
        go.transform.SetParent(parent, false);
        return go.AddComponent<T>();
    }

    public void Init(TubeData data, WaterColorPalette palette)
    {
        var extraHeight = ResizeTube(data.capacity);
        _restLocalPos = transform.localPosition;
        _anim.SetRestLocalPos(_restLocalPos);
        _anim.SetBottom(-tubeRenderer.size.y * 0.5f);
        PlaceCap();

        _liquid.Setup(BuildLiquidShape(), settings, liquidMaterial, liquidSortingOrder);
        _stream.Setup(settings, pourStreamMaterial, splashMaterial, streamSortingOrder);
        _sparks.Setup(splashMaterial, sparkSortingOrder);
        CacheSortingOrders();
        // shader time is Time.time under URP
        SetGlassFloat(SweepPeriodId, Random.Range(IdleShinePeriodMin, IdleShinePeriodMax));
        SetGlassFloat(SweepStartId, Time.time + Random.Range(0f, IdleShineMaxDelay));

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

    private LiquidShape BuildLiquidShape()
    {
        var half = tubeRenderer.size.y * 0.5f;
        return new LiquidShape
        {
            BodyHalfWidth = bodyHalfWidth,
            NeckHalfWidth = neckHalfWidth,
            BottomY = -half + bottomInset,
            BottomCurveHeight = bottomCurveHeight,
            ShoulderY = half - shoulderDrop,
            NeckY = half - neckDrop,
            LipY = half - lipDrop,
            EllipseRatio = ellipseRatio,
            BottomEllipseRatio = bottomEllipseRatio,
        };
    }

    // The cork shares the bottle sprite's pixel grid and sits on its top edge, which the 9-slice never stretches.
    private void PlaceCap()
    {
        var size = tubeRenderer.size;
        var s = size.x * capSprite.pixelsPerUnit / tubeRenderer.sprite.rect.width;
        var scale = new Vector3(s, s, 1f);
        _anim.SetCap(_cap, capSeatedSprite, tubeRenderer.sortingOrder - 1, new Vector3(0f, size.y * 0.5f, 0f), scale,
            capSprite.bounds.size.y * s);
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
        _stream.SetProgress(poured / Mathf.Max(1, _pourAmount));
    }

    private void OnPourFlowEnd()
    {
        _liquid.EndDrain();
        _pourTarget._liquid.EndFill();
        _stream.End();
        EventBus<PourFlowEndedEvent>.Publish(new PourFlowEndedEvent { From = this, To = _pourTarget });
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

    // The selected tube's glass lights up, brightest where the liquid ends (TubeGlass.shader).
    private void SetGlassGlow(float glow)
    {
        tubeRenderer.GetPropertyBlock(_glassBlock);
        _glassBlock.SetFloat(GlowId, glow);
        _glassBlock.SetFloat(GlowLineId, _liquid.SurfaceLocalY);
        tubeRenderer.SetPropertyBlock(_glassBlock);
    }

    private void SetGlassFlash(float flash) => SetGlassFloat(FlashId, flash);

    // 0..1 runs the band from below the bottle to past its top, so it is off at both ends
    private void SetGlassShine(float shine) => SetGlassFloat(ShineId, shine);

    private void SetGlassFloat(int id, float value)
    {
        tubeRenderer.GetPropertyBlock(_glassBlock);
        _glassBlock.SetFloat(id, value);
        tubeRenderer.SetPropertyBlock(_glassBlock);
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
        HapticService.PlayInvalid();
    }

    public bool HasColor(Color color)
    {
        return _waterSlots.HasColor(color);
    }

    public void PlayEnterAnimation(float delay)
    {
        var worldRightX = _mainCamera.transform.position.x + _mainCamera.orthographicSize * _mainCamera.aspect + 2f;
        var localRightX = transform.parent.InverseTransformPoint(new Vector3(worldRightX, 0f, 0f)).x;

        _anim.PlayEnter(localRightX, delay);
    }

    public Tween MarkSolved()
    {
        IsSolved = true;
        tubeCollider.enabled = false;
        EventBus<TubeSolvedEvent>.Publish(new TubeSolvedEvent { SolvedColor = TopColor });
        HapticService.PlayWin();

        return _anim.PlayMarkSolved(_cachedSolvedSeat, _cachedSolvedPeak);
    }

    // the cork snaps in: the bottle flashes and a few glints pop around the cork
    private void OnSolvedSeat()
    {
        _sparks.Pop(_liquid.MouthWorld, Mathf.Abs(transform.lossyScale.x));
        _scope.Add(DOVirtual.Float(1f, 0f, SolvedFlashDuration, _cachedSetGlassFlash).SetEase(Ease.OutQuad));
    }

    // the top of the hop: the big burst, while one band of shine runs up the bottle
    private void OnSolvedPeak()
    {
        var bounds = tubeRenderer.bounds;
        _sparks.Burst(_liquid.MouthWorld, bounds.center, bounds.extents, Mathf.Abs(transform.lossyScale.x));
        _scope.Add(DOVirtual.Float(0f, 1f, SolvedShineDuration, _cachedSetGlassShine).SetEase(Ease.InOutSine));
    }

    public void PourInto(TubeView target)
    {
        _pourTarget = target;
        _pourColor = TopColor;
        _pourAmount = Mathf.Min(TopColorCount, target.AvailableSlots);
        SetSortingBoost(settings.PourSortingBoost);

        var callbacks = new PourAnimCallbacks(_cachedTransferWater, _cachedPourFlow, _cachedPourFlowEnd, _cachedInvokePourComplete);
        _anim.PlayPourInto(_liquid, target._liquid, _pourAmount, RestWorldPosition, callbacks);
    }

    private void TransferWater(TubeView target)
    {
        _waterSlots.RemoveTopWaters(_pourAmount);
        target._waterSlots.AddWaters(_pourColor, _pourAmount);

        _liquid.BeginDrain();
        target._liquid.BeginFill(_pourColor);
        _stream.Begin(_liquid, target._liquid, _anim.PourTiltSign, _pourColor);
        target._anim.PlayReceive(settings.StreamFallDuration, _cachedPourLandHaptic);
    }

    private float ResizeTube(int capacity)
    {
        var extraHeight = (capacity - defaultCapacity) * settings.WaterSlotHeight;

        var size = tubeRenderer.size;
        tubeRenderer.size = new Vector2(size.x, size.y + extraHeight);

        tubeCollider.size = new Vector2(tubeCollider.size.x, tubeCollider.size.y + extraHeight);
        tubeCollider.offset = new Vector2(tubeCollider.offset.x, tubeCollider.offset.y + extraHeight * 0.5f);

        transform.localPosition -= new Vector3(0f, extraHeight * 0.5f, 0f);

        return extraHeight;
    }
}
