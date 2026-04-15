using UnityEngine;

public class GameOverUIPage : UIPageBase
{
    [SerializeField] private UIClickTarget restartButton;
    [SerializeField] private UIClickTarget menuButton;

    protected override void OnPageOpened(UIPageOpenContext context)
    {
        restartButton.OnClicked += OnRestartClicked;
        menuButton.OnClicked += OnMenuClicked;
    }

    protected override void OnPageClosed()
    {
        restartButton.OnClicked -= OnRestartClicked;
        menuButton.OnClicked -= OnMenuClicked;
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
