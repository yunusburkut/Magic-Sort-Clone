using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "GameSettings", menuName = "WaterSort/Game Settings")]
public class GameSettings : ScriptableObject
{
    [Header("Water Layout")]
    [SerializeField, FormerlySerializedAs("waterSlotHeight")] private float _waterSlotHeight = 0.5f;
    [SerializeField, FormerlySerializedAs("waterYStackOffset")] private float _waterYStackOffset = 0.25f;
    [SerializeField, FormerlySerializedAs("waterRevealDuration")] private float _waterRevealDuration = 0.3f;
    [SerializeField] private float _waterSegmentOverlap = 0.05f;

    [Header("Tube Selection")]
    [SerializeField, FormerlySerializedAs("outlineColor")] private Color _outlineColor = new Color(1f, 0.85f, 0.3f, 1f);
    [SerializeField, FormerlySerializedAs("outlineThickness")] private float _outlineThickness = 0.1f;
    [SerializeField, FormerlySerializedAs("liftAmount")] private float _liftAmount = 0.3f;
    [SerializeField, FormerlySerializedAs("liftDuration")] private float _liftDuration = 0.2f;
    [SerializeField, FormerlySerializedAs("anticipationDip")] private float _anticipationDip = 0.15f;
    [SerializeField, FormerlySerializedAs("anticipationDuration")] private float _anticipationDuration = 0.06f;

    [Header("Pour Animation")]
    [SerializeField, FormerlySerializedAs("pourOffsetX")] private float _pourOffsetX = 0.8f;
    [SerializeField, FormerlySerializedAs("pourAngle")] private float _pourAngle = 120f;
    [SerializeField, FormerlySerializedAs("pourDuration")] private float _pourDuration = 0.25f;
    [SerializeField, FormerlySerializedAs("pourHeightOffset")] private float _pourHeightOffset = 0.5f;
    [SerializeField, FormerlySerializedAs("pourHoldDuration")] private float _pourHoldDuration = 0.4f;
    [SerializeField, FormerlySerializedAs("pourArcHeight")] private float _pourArcHeight = 0.8f;
    [SerializeField] private float _pourLiftAmount = 0.3f;
    [SerializeField] private float _pourLiftDuration = 0.12f;
    [SerializeField] private float _pourStreamInDuration = 0.08f;

    [Header("Shake Animation")]
    [SerializeField, FormerlySerializedAs("shakeMagnitude")] private float _shakeMagnitude = 0.08f;
    [SerializeField, FormerlySerializedAs("shakeDuration")] private float _shakeDuration = 0.04f;
    [SerializeField, FormerlySerializedAs("shakeDecay1")] private float _shakeDecay1 = 0.6f;
    [SerializeField, FormerlySerializedAs("shakeDecay2")] private float _shakeDecay2 = 0.4f;

    [Header("Solved Animation")]
    [SerializeField, FormerlySerializedAs("solvedSquashX")] private float _solvedSquashX = 1.15f;
    [SerializeField, FormerlySerializedAs("solvedSquashY")] private float _solvedSquashY = 0.85f;
    [SerializeField, FormerlySerializedAs("solvedSquashDuration")] private float _solvedSquashDuration = 0.08f;
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

    public float WaterSlotHeight => _waterSlotHeight;
    public float WaterSegmentOverlap => _waterSegmentOverlap;
    public float WaterYStackOffset => _waterYStackOffset;
    public float WaterRevealDuration => _waterRevealDuration;
    public Color OutlineColor => _outlineColor;
    public float OutlineThickness => _outlineThickness;
    public float LiftAmount => _liftAmount;
    public float LiftDuration => _liftDuration;
    public float AnticipationDip => _anticipationDip;
    public float AnticipationDuration => _anticipationDuration;
    public float PourOffsetX => _pourOffsetX;
    public float PourAngle => _pourAngle;
    public float PourDuration => _pourDuration;
    public float PourHeightOffset => _pourHeightOffset;
    public float PourHoldDuration => _pourHoldDuration;
    public float PourArcHeight => _pourArcHeight;
    public float PourLiftAmount => _pourLiftAmount;
    public float PourLiftDuration => _pourLiftDuration;
    public float PourStreamInDuration => _pourStreamInDuration;
    public float ShakeMagnitude => _shakeMagnitude;
    public float ShakeDuration => _shakeDuration;
    public float ShakeDecay1 => _shakeDecay1;
    public float ShakeDecay2 => _shakeDecay2;
    public float SolvedSquashX => _solvedSquashX;
    public float SolvedSquashY => _solvedSquashY;
    public float SolvedSquashDuration => _solvedSquashDuration;
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
}
