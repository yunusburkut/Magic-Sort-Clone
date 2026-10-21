using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum FlowState { Menu, Gameplay, Win }

public class FlowController : MonoBehaviour
{
    [SerializeField] private GameObject menu;
    [SerializeField] private Button menuButton;

    [SerializeField] private GameObject win;
    [SerializeField] private Button winButton;

    [SerializeField] private TextMeshProUGUI stateText;

    private LevelController _levelController;

    public FlowState Current { get; private set; }

    public void Initialize(LevelController levelController)
    {
        _levelController = levelController;

        menuButton.onClick.AddListener(OnMenuButtonClicked);
        winButton.onClick.AddListener(OnWinButtonClicked);

        EventBus<LevelCompletedEvent>.Subscribe(OnLevelCompleted);

        EnterState(FlowState.Menu);
    }

    private void OnDestroy()
    {
        EventBus<LevelCompletedEvent>.Unsubscribe(OnLevelCompleted);
    }

    private void OnMenuButtonClicked()
    {
        _levelController.StartLevel();
        EnterState(FlowState.Gameplay);
    }

    private void OnWinButtonClicked()
    {
        _levelController.ClearLevel();
        EnterState(FlowState.Menu);
    }

    private void OnLevelCompleted(LevelCompletedEvent e)
    {
        EnterState(FlowState.Win);
    }

    private void EnterState(FlowState state)
    {
        Current = state;
        menu.SetActive(state == FlowState.Menu);
        win.SetActive(state == FlowState.Win);

        stateText.text = Current.ToString();

        if (state == FlowState.Menu) PopIn(menu.transform);
        else if (state == FlowState.Win) PopIn(win.transform);
    }

    // the panel grows in with a soft overshoot instead of blinking on
    private static void PopIn(Transform panel)
    {
        panel.DOKill();
        panel.localScale = Vector3.one * 0.7f;
        panel.DOScale(1f, 0.35f).SetEase(Ease.OutBack, 1.3f).SetLink(panel.gameObject);
    }
}