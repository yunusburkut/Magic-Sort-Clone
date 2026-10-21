using MoreMountains.Feedbacks;
using UnityEngine;

// Soft camera zoom punches when a tube is solved and when the level is won. A Feel spring drives the ortho
// size, so the zoom eases in and breathes back out instead of snapping. It only ever zooms in, so the edges
// of the background never show.
[RequireComponent(typeof(Camera))]
public sealed class CameraFeel : MonoBehaviour
{
    // fraction of the ortho size the zoom peaks at
    [SerializeField] private float solvedZoom = 0.015f;
    [SerializeField] private float levelZoom = 0.04f;
    [SerializeField, Range(0.1f, 1f)] private float damping = 0.7f;
    [SerializeField] private float frequency = 0.9f;

    private readonly MMSpringFloat _zoom = new MMSpringFloat();
    private Camera _camera;
    private float _baseSize;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        _baseSize = _camera.orthographicSize;
        _zoom.Damping = damping;
        _zoom.Frequency = frequency;
        _zoom.MoveToInstant(0f);
    }

    private void OnEnable()
    {
        EventBus<TubeSolvedEvent>.Subscribe(OnTubeSolved);
        EventBus<LevelCompletedEvent>.Subscribe(OnLevelCompleted);
    }

    private void OnDisable()
    {
        EventBus<TubeSolvedEvent>.Unsubscribe(OnTubeSolved);
        EventBus<LevelCompletedEvent>.Unsubscribe(OnLevelCompleted);
        _zoom.MoveToInstant(0f);
        _camera.orthographicSize = _baseSize;
    }

    private void Update()
    {
        _zoom.UpdateSpringValue(Time.deltaTime);
        _camera.orthographicSize = _baseSize * (1f - Mathf.Max(0f, _zoom.CurrentValue));
    }

    private void OnTubeSolved(TubeSolvedEvent e) => Punch(solvedZoom);

    private void OnLevelCompleted(LevelCompletedEvent e) => Punch(levelZoom);

    // A velocity kick on a resting spring peaks at about 0.46 * kick / omega when the damping is near 0.7.
    private void Punch(float zoom)
    {
        _zoom.Bump(zoom * 2f * Mathf.PI * frequency / 0.46f);
    }
}
