using UnityEngine;

public class MenuUIPage : UIPageBase
{
    [SerializeField] private UIClickTarget startButton;
    [SerializeField] private UIClickTarget characterSelectButton;

    protected override void OnPageOpened(UIPageOpenContext context)
    {
        startButton.OnClicked += OnStartButtonOnClicked;
    }

    protected override void OnPageClosed()
    {
        startButton.OnClicked -= OnStartButtonOnClicked;
    }

    private void OnStartButtonOnClicked()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.WoodenButtonClicked);
        GameEventBus.Publish<MenuStartClickedEvent>();
    }
}
