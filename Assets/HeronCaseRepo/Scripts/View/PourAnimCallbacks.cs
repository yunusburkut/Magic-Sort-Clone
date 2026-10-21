using DG.Tweening;

public readonly struct PourAnimCallbacks
{
    // tube is in place above the target and liquid starts to flow
    public readonly TweenCallback OnTransfer;
    // slots poured so far, every frame of the flow
    public readonly TweenCallback<float> OnFlow;
    public readonly TweenCallback OnFlowEnd;
    public readonly TweenCallback OnComplete;

    public PourAnimCallbacks(TweenCallback onTransfer, TweenCallback<float> onFlow, TweenCallback onFlowEnd, TweenCallback onComplete)
    {
        OnTransfer = onTransfer;
        OnFlow = onFlow;
        OnFlowEnd = onFlowEnd;
        OnComplete = onComplete;
    }
}
