using UnityEngine;
using UnityEngine.UI;

public class GameOverUIPage : UIPageBase
{
    [SerializeField] private Button restartButton;
    [SerializeField] private Button menuButton;

    protected override void OnPageOpened(UIPageOpenContext context)
    {
        restartButton?.onClick.AddListener(OnRestartClicked);
        menuButton?.onClick.AddListener(OnMenuClicked);
    }

    protected override void OnPageClosed()
    {
        restartButton?.onClick.RemoveListener(OnRestartClicked);
        menuButton?.onClick.RemoveListener(OnMenuClicked);
    }

    private void OnRestartClicked()
    {
        GameEventBus.Publish<GameOverRestartClickedEvent>();
    }

    private void OnMenuClicked()
    {
        GameEventBus.Publish<GameOverReturnToMenuClickedEvent>();
    }
}
