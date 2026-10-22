using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour, ITubeInteractionController
{
    [Header("Settings")]
    [SerializeField] private GameSettings settings;

    private TubeView _selectedTube;
    private PourCoordinator _pourCoordinator;

    // the last tap that hit a busy tube, replayed when that tube frees up within the buffer window
    private TubeView _bufferedTube;
    private float _bufferedAt;

    private void OnEnable()
    {
        EventBus<ShakeCompletedEvent>.Subscribe(OnShakeComplete);
    }

    private void OnDisable()
    {
        EventBus<ShakeCompletedEvent>.Unsubscribe(OnShakeComplete);
    }

    private void OnDestroy()
    {
        if (_pourCoordinator != null)
        {
            _pourCoordinator.TubeFreed -= OnTubeFreed;
            _pourCoordinator.Dispose();
        }
    }

    public void Initialize(List<TubeView> tubes)
    {
        _selectedTube = null;
        _bufferedTube = null;

        if (_pourCoordinator != null)
        {
            _pourCoordinator.TubeFreed -= OnTubeFreed;
            _pourCoordinator.Dispose();
        }

        _pourCoordinator = new PourCoordinator(settings.QueuedPourSpeedMultiplier);
        _pourCoordinator.TubeFreed += OnTubeFreed;
    }

    public void OnTubeClicked(TubeView tube)
    {
        if (_pourCoordinator.IsLocked(tube))
        {
            BufferTap(tube);
            return;
        }

        // a newer tap wins over an older one still waiting
        _bufferedTube = null;

        if (_selectedTube == null)
        {
            if (_pourCoordinator.HasActivePour(tube))
            {
                BufferTap(tube);
                return;
            }

            if (tube.IsEmpty || tube.IsSolved)
            {
                return;
            }

            _selectedTube = tube;
            tube.SetSelected(true);
            tube.Punch();
            return;
        }

        if (_selectedTube == tube)
        {
            tube.SetSelected(false);
            _selectedTube = null;
            return;
        }

        // the tube picked to pour into answers the tap too, whether the pour goes ahead or not
        tube.Punch();

        if (_pourCoordinator.HasActivePour(tube))
        {
            _pourCoordinator.TryQueuePour(_selectedTube, tube);
            _selectedTube.SetSelected(false);
            _selectedTube = null;
            return;
        }

        if (!MoveValidator.CanPour(_selectedTube, tube))
        {
            var shakeTarget = _selectedTube;
            _selectedTube = null;
            _pourCoordinator.Lock(shakeTarget);
            shakeTarget.Shake();
            return;
        }

        var from = _selectedTube;
        _selectedTube = null;
        from.SetSelected(false);
        _pourCoordinator.StartPour(from, tube);
    }

    // a tap away from the bottles puts the lifted one back down
    public void OnEmptySpaceClicked()
    {
        // and cancels a tap still waiting, like any newer tap
        _bufferedTube = null;

        if (_selectedTube == null)
        {
            return;
        }

        _selectedTube.SetSelected(false);
        _selectedTube = null;
    }

    private void OnShakeComplete(ShakeCompletedEvent e)
    {
        // a tap on the shaking tube was a reaction to the shake, not a new move
        if (_bufferedTube == e.Tube) _bufferedTube = null;

        _pourCoordinator.Unlock(e.Tube);
        e.Tube.SetSelected(false);
    }

    // A tap on a busy tube is not thrown away: it lands as soon as the tube is free, so the player never feels
    // locked out by an animation.
    private void BufferTap(TubeView tube)
    {
        _bufferedTube = tube;
        _bufferedAt = Time.time;
    }

    private void OnTubeFreed(TubeView tube)
    {
        if (_bufferedTube != tube)
        {
            return;
        }

        _bufferedTube = null;
        if (Time.time - _bufferedAt <= settings.InputBufferWindow)
        {
            OnTubeClicked(tube);
        }
    }
}
