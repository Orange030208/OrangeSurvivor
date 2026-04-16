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
        AudioSfxBridge.RequestPlay(AudioSfxKey.WoodenButtonClicked);
        GameEventBus.Publish<GameOverRestartClickedEvent>();
    }

    private void OnMenuClicked()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.WoodenButtonClicked);
        GameEventBus.Publish<GameOverReturnToMenuClickedEvent>();
    }
}
