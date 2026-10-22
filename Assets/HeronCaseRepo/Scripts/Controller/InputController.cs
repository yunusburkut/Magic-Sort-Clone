using UnityEngine;

public class InputController : MonoBehaviour
{
    private FlowController _flowController;
    private ITubeInteractionController _levelController;

    public void Initialize(FlowController flowController, ITubeInteractionController levelController)
    {
        _flowController = flowController;
        _levelController = levelController;
        EventBus<TubeClickedEvent>.Subscribe(OnTubeClicked);
        EventBus<EmptySpaceClickedEvent>.Subscribe(OnEmptySpaceClicked);
    }

    private void OnDestroy()
    {
        EventBus<TubeClickedEvent>.Unsubscribe(OnTubeClicked);
        EventBus<EmptySpaceClickedEvent>.Unsubscribe(OnEmptySpaceClicked);
    }

    private void OnEmptySpaceClicked(EmptySpaceClickedEvent e)
    {
        if (_flowController.Current != FlowState.Gameplay)
        {
            return;
        }

        _levelController.OnEmptySpaceClicked();
    }

    private void OnTubeClicked(TubeClickedEvent e)
    {
        if (_flowController.Current != FlowState.Gameplay)
        {
            return;
        }

        _levelController.OnTubeClicked(e.Tube);
    }
}
