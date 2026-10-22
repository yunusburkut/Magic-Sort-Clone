using System.Collections.Generic;
using HeronCaseRepo.Scripts.Services;
using UnityEngine;

public class LevelController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private LevelData levelData;
    [SerializeField] private WaterColorPalette colorPalette;

    [Header("Prefabs")]
    [SerializeField] private TubeView tubePrefab;

    [Header("Layout")]
    [SerializeField] private float tubeSpacing = 1.5f;
    [SerializeField] private float rowSpacing = 10f;
    [SerializeField] private int tubesPerRow = 4;
    [SerializeField] private Transform tubesContainer;
    [SerializeField] private GameSettings settings;

    private ITubeInteractionController _levelController;
    private WinConditionChecker _winChecker;
    private DeadlockChecker _deadlockChecker;
    private readonly List<TubeView> _tubeViews = new List<TubeView>();
    private readonly List<TubeData> _builtTubes = new List<TubeData>();
    private Camera _camera;
    private BoardTapArea _tapArea;

    public void Initialize(ITubeInteractionController levelController, WinConditionChecker winChecker, DeadlockChecker deadlockChecker)
    {
        _levelController = levelController;
        _winChecker = winChecker;
        _deadlockChecker = deadlockChecker;
        _camera = Camera.main;

        // every bottle's cell reaches halfway to its neighbors in the row and to the next row
        _tapArea = new GameObject("BoardTapArea").AddComponent<BoardTapArea>();
        _tapArea.transform.SetParent(transform, false);
        _tapArea.Setup(_camera, new Vector2(tubeSpacing, rowSpacing));
        _tapArea.SetTubes(_tubeViews);

        EventBus<DeadlockEvent>.Subscribe(RestartLevel);
    }
    

    public void StartLevel()
    {
        ClearLevel();
        if (levelData.Tubes.Count > 0)
        {
            SpawnLevel(levelData.Tubes);
        }
        else
        {
            LevelDataBuilder.Build(levelData, _builtTubes);
            SpawnLevel(_builtTubes);
        }
    }

    public void ClearLevel()
    {
        for (var i = 0; i < _tubeViews.Count; i++)
            Destroy(_tubeViews[i].gameObject);
        _tubeViews.Clear();
    }
    
    private void RestartLevel(DeadlockEvent e)
    {
        ClearLevel();
        SpawnLevel(levelData.Tubes);
    }

    private void SpawnLevel(List<TubeData> tubeDataList)
    {
        var count = tubeDataList.Count;
        var rowCount = Mathf.CeilToInt((float)count / tubesPerRow);
        var animIndex = 0;

        var camPos = _camera.transform.position;
        var layoutOriginY = camPos.y + (rowCount - 1) * rowSpacing * 0.5f;

        for (var row = 0; row < rowCount; row++)
        {
            var rowStart = row * tubesPerRow;
            var rowSize = Mathf.Min(tubesPerRow, count - rowStart);
            var startX = camPos.x - (rowSize - 1) * tubeSpacing * 0.5f;
            var y = layoutOriginY - row * rowSpacing;

            for (var col = 0; col < rowSize; col++)
            {
                var i = rowStart + col;
                var pos = new Vector3(startX + col * tubeSpacing, y, 0);
                var tubeView = Instantiate(tubePrefab, pos, Quaternion.identity, tubesContainer);
#if UNITY_EDITOR
                tubeView.name = $"Tube_{i}";
#endif
                tubeView.Init(tubeDataList[i], colorPalette);
                tubeView.PlayEnterAnimation(animIndex * settings.EnterStaggerDelay);
                _tubeViews.Add(tubeView);
                animIndex++;
            }
        }

        _levelController.Initialize(_tubeViews);
        _winChecker.Initialize(_tubeViews);
        _deadlockChecker.Initialize(_tubeViews);
    }
}
