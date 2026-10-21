using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "GameSettings", menuName = "WaterSort/Game Settings")]
public class GameSettings : ScriptableObject
{
    [Header("Water Layout")]
    [SerializeField, FormerlySerializedAs("waterSlotHeight")] private float _waterSlotHeight = 0.5f;
    [SerializeField, FormerlySerializedAs("waterRevealDuration")] private float _waterRevealDuration = 0.3f;
    [SerializeField] private Color _hiddenLiquidColor = new Color(0.0566f, 0.0566f, 0.0566f, 1f);

    [Header("Liquid Slosh")]
    [SerializeField] private float _sloshFrequency = 1.6f;
    [SerializeField] private float _sloshDamping = 0.12f;
    [SerializeField] private float _sloshAccelGain = 0.15f;
    [SerializeField] private float _sloshVerticalGain = 0.03f;
    [SerializeField] private float _sloshSpinGain = 2.5f;
    [SerializeField] private float _sloshSwirl = 0.1f;
    [SerializeField] private float _sloshMaxSlope = 0.22f;
    [SerializeField] private float _sloshMaxAccel = 300f;
    [SerializeField] private float _sloshIdle = 0.06f;
    [SerializeField] private float _sloshImpactKick = 1.5f;

    [Header("Tube Selection")]
    [SerializeField, FormerlySerializedAs("outlineColor")] private Color _outlineColor = new Color(1f, 0.85f, 0.3f, 1f);
    [SerializeField, FormerlySerializedAs("outlineThickness")] private float _outlineThickness = 0.1f;
    [SerializeField, FormerlySerializedAs("liftAmount")] private float _liftAmount = 0.3f;
    [SerializeField, FormerlySerializedAs("liftDuration")] private float _liftDuration = 0.2f;
    [SerializeField, FormerlySerializedAs("anticipationDip")] private float _anticipationDip = 0.15f;
    [SerializeField, FormerlySerializedAs("anticipationDuration")] private float _anticipationDuration = 0.06f;

    [Header("Pour Animation")]
    [SerializeField] private float _pourWindUpDuration = 0.06f;
    [SerializeField] private float _pourWindUpDistance = 0.3f;
    [SerializeField] private float _pourWindUpLean = 6f;
    [SerializeField] private float _pourMoveDuration = 0.14f;
    [SerializeField] private float _pourFlowBaseDuration = 0.2f;
    [SerializeField] private float _pourFlowDurationPerSlot = 0.3f;
    [SerializeField] private float _pourFlowSlotExponent = 0.85f;
    [SerializeField] private float _pourFlowSpeedUp = 1.35f;
    [SerializeField] private float _pourFlowRise = 0.35f;
    [SerializeField] private float _pourReturnDuration = 0.4f;
    [SerializeField, FormerlySerializedAs("pourArcHeight")] private float _pourArcHeight = 0.8f;
    [SerializeField] private float _pourLipHeight = 1f;
    [SerializeField] private float _pourLipSideOffset = 0.15f;
    [SerializeField] private float _pourMaxAngle = 115f;
    [SerializeField] private int _pourSortingBoost = 100;

    [Header("Pour Stream")]
    [SerializeField] private float _streamLipWidth = 0.5f;
    [SerializeField] private float _streamEndWidth = 0.36f;
    [SerializeField] private float _streamFallDuration = 0.13f;
    [SerializeField] private float _streamMouthMargin = 0.2f;
    [SerializeField] private float _streamStartThin = 0.4f;
    [SerializeField] private float _streamEndThin = 0.45f;
    [SerializeField, FormerlySerializedAs("_splashMistRate")] private float _splashFoamRate = 16f;
    [SerializeField] private float _splashSprayRate = 9f;

    [Header("Squash & Stretch (height factor, the width keeps the area)")]
    [SerializeField] private float _squashCrouch = 0.93f;
    [SerializeField] private float _squashJumpStretch = 1.05f;
    [SerializeField] private float _squashFallStretch = 1.03f;
    [SerializeField] private float _squashLand = 0.94f;
    [SerializeField] private float _squashReceive = 0.97f;
    [SerializeField] private float _squashSettleDuration = 0.35f;
    [SerializeField] private float _enterSquash = 0.97f;
    [SerializeField] private float _enterLean = 5f;
    [SerializeField] private float _shakeLean = 2.5f;
    // the spring the body settles back into shape with: damping 1 glides in, lower swings past the rest shape;
    // frequency is swings per settle, whatever its duration
    [SerializeField, Range(0.1f, 1f)] private float _settleDamping = 0.6f;
    [SerializeField] private float _settleFrequency = 1.5f;

    [Header("Shake Animation")]
    [SerializeField, FormerlySerializedAs("shakeMagnitude")] private float _shakeMagnitude = 0.08f;
    [SerializeField, FormerlySerializedAs("shakeDuration")] private float _shakeDuration = 0.04f;
    [SerializeField, FormerlySerializedAs("shakeDecay1")] private float _shakeDecay1 = 0.6f;
    [SerializeField, FormerlySerializedAs("shakeDecay2")] private float _shakeDecay2 = 0.4f;

    [Header("Solved Animation")]
    [SerializeField, FormerlySerializedAs("solvedSquashY")] private float _solvedSquashY = 0.82f;
    [SerializeField, FormerlySerializedAs("solvedSquashDuration")] private float _solvedSquashDuration = 0.22f;
    [SerializeField] private float _solvedChargeDuration = 0.06f;
    [SerializeField] private float _solvedChargeTremble = 0.02f;
    [SerializeField] private float _solvedStretch = 1.12f;
    [SerializeField] private float _solvedHopHeight = 0.6f;
    [SerializeField] private float _solvedHopDuration = 0.36f;
    [SerializeField] private float _solvedLandSquash = 0.88f;
    [SerializeField, FormerlySerializedAs("solvedBounceDuration")] private float _solvedBounceDuration = 0.5f;

    [Header("Enter Animation")]
    [SerializeField] private float _enterDuration = 0.6f;
    [SerializeField] private float _enterStaggerDelay = 0.08f;

    [Header("Cloak Animation")]
    [SerializeField] private float _cloakAnticipationDip = 0.15f;
    [SerializeField] private float _cloakAnticipationDuration = 0.1f;
    [SerializeField] private float _cloakAnticipationSquashX = 1.1f;
    [SerializeField] private float _cloakAnticipationSquashY = 0.85f;
    [SerializeField] private float _cloakStretchDuration = 0.08f;
    [SerializeField] private float _cloakStretchX = 0.7f;
    [SerializeField] private float _cloakStretchY = 1.4f;
    [SerializeField] private float _cloakLaunchHeight = 14f;
    [SerializeField] private float _cloakLaunchDuration = 0.45f;
    [SerializeField] private float _cloakScaleReturnDuration = 0.3f;
    [SerializeField] private float _cloakRotation = 8f;

    [Header("Game")]
    [SerializeField, FormerlySerializedAs("queuedPourSpeedMultiplier")] private float _queuedPourSpeedMultiplier = 1.75f;
    // a tap on a tube that is still busy is kept this long and replayed once the tube is free
    [SerializeField] private float _inputBufferWindow = 0.35f;

    public float WaterSlotHeight => _waterSlotHeight;
    public float WaterRevealDuration => _waterRevealDuration;
    public Color HiddenLiquidColor => _hiddenLiquidColor;
    public float SloshFrequency => _sloshFrequency;
    public float SloshDamping => _sloshDamping;
    public float SloshAccelGain => _sloshAccelGain;
    public float SloshVerticalGain => _sloshVerticalGain;
    public float SloshSpinGain => _sloshSpinGain;
    public float SloshSwirl => _sloshSwirl;
    public float SloshMaxSlope => _sloshMaxSlope;
    public float SloshMaxAccel => _sloshMaxAccel;
    public float SloshIdle => _sloshIdle;
    public float SloshImpactKick => _sloshImpactKick;
    public Color OutlineColor => _outlineColor;
    public float OutlineThickness => _outlineThickness;
    public float LiftAmount => _liftAmount;
    public float LiftDuration => _liftDuration;
    public float AnticipationDip => _anticipationDip;
    public float AnticipationDuration => _anticipationDuration;
    public float PourWindUpDuration => _pourWindUpDuration;
    public float PourWindUpDistance => _pourWindUpDistance;
    public float PourWindUpLean => _pourWindUpLean;
    public float PourMoveDuration => _pourMoveDuration;
    public float PourFlowBaseDuration => _pourFlowBaseDuration;
    public float PourFlowDurationPerSlot => _pourFlowDurationPerSlot;
    public float PourFlowSlotExponent => _pourFlowSlotExponent;
    public float PourFlowSpeedUp => _pourFlowSpeedUp;
    public float PourFlowRise => _pourFlowRise;
    public float PourReturnDuration => _pourReturnDuration;
    public float PourArcHeight => _pourArcHeight;
    public float PourLipHeight => _pourLipHeight;
    public float PourLipSideOffset => _pourLipSideOffset;
    public float PourMaxAngle => _pourMaxAngle;
    public int PourSortingBoost => _pourSortingBoost;
    public float StreamLipWidth => _streamLipWidth;
    public float StreamEndWidth => _streamEndWidth;
    public float StreamFallDuration => _streamFallDuration;
    public float StreamMouthMargin => _streamMouthMargin;
    public float StreamStartThin => _streamStartThin;
    public float StreamEndThin => _streamEndThin;
    public float SplashFoamRate => _splashFoamRate;
    public float SplashSprayRate => _splashSprayRate;
    public float SquashCrouch => _squashCrouch;
    public float SquashJumpStretch => _squashJumpStretch;
    public float SquashFallStretch => _squashFallStretch;
    public float SquashLand => _squashLand;
    public float SquashReceive => _squashReceive;
    public float SquashSettleDuration => _squashSettleDuration;
    public float EnterSquash => _enterSquash;
    public float EnterLean => _enterLean;
    public float ShakeLean => _shakeLean;
    public float SettleDamping => _settleDamping;
    public float SettleFrequency => _settleFrequency;
    public float ShakeMagnitude => _shakeMagnitude;
    public float ShakeDuration => _shakeDuration;
    public float ShakeDecay1 => _shakeDecay1;
    public float ShakeDecay2 => _shakeDecay2;
    public float SolvedSquashY => _solvedSquashY;
    public float SolvedSquashDuration => _solvedSquashDuration;
    public float SolvedChargeDuration => _solvedChargeDuration;
    public float SolvedChargeTremble => _solvedChargeTremble;
    public float SolvedStretch => _solvedStretch;
    public float SolvedHopHeight => _solvedHopHeight;
    public float SolvedHopDuration => _solvedHopDuration;
    public float SolvedLandSquash => _solvedLandSquash;
    public float SolvedBounceDuration => _solvedBounceDuration;
    public float EnterDuration => _enterDuration;
    public float EnterStaggerDelay => _enterStaggerDelay;
    public float CloakAnticipationDip => _cloakAnticipationDip;
    public float CloakAnticipationDuration => _cloakAnticipationDuration;
    public float CloakAnticipationSquashX => _cloakAnticipationSquashX;
    public float CloakAnticipationSquashY => _cloakAnticipationSquashY;
    public float CloakStretchDuration => _cloakStretchDuration;
    public float CloakStretchX => _cloakStretchX;
    public float CloakStretchY => _cloakStretchY;
    public float CloakLaunchHeight => _cloakLaunchHeight;
    public float CloakLaunchDuration => _cloakLaunchDuration;
    public float CloakScaleReturnDuration => _cloakScaleReturnDuration;
    public float CloakRotation => _cloakRotation;
    public float QueuedPourSpeedMultiplier => _queuedPourSpeedMultiplier;
    public float InputBufferWindow => _inputBufferWindow;
}
