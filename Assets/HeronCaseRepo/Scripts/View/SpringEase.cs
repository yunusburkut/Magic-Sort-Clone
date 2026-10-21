using DG.Tweening;
using MoreMountains.Tools;
using UnityEngine;

// A DOTween ease baked from Feel's damped spring solver. It eases out of the start, swings softly past the end
// once and comes to rest, where OutElastic buzzes back and forth several times and reads as jitter.
// Frequency counts the swings over the whole tween, so the feel stays the same at any duration.
public sealed class SpringEase
{
    private const int Samples = 64;

    private readonly float[] _table = new float[Samples + 1];

    public EaseFunction Function { get; }

    public SpringEase(float damping, float frequency)
    {
        var value = 0f;
        var velocity = 0f;
        for (var i = 0; i <= Samples; i++)
        {
            _table[i] = value;
            MMMaths.Spring(ref value, 1f, ref velocity, damping, frequency, 1f / Samples);
        }

        // the spring is all but still by the end; land exactly on the target
        _table[Samples] = 1f;
        Function = Evaluate;
    }

    private float Evaluate(float time, float duration, float overshootOrAmplitude, float period)
    {
        var t = Mathf.Clamp01(time / duration) * Samples;
        var i = Mathf.Min((int)t, Samples - 1);
        return Mathf.LerpUnclamped(_table[i], _table[i + 1], t - i);
    }
}
