using UnityEngine;
using UnityEngine.UI;

public class MenuUIPage : UIPageBase
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button characterSelectButton;

    protected override void OnPageOpened(UIPageOpenContext context)
    {
        startButton.onClick.AddListener(OnStartButtonClicked);
    }

    protected override void OnPageClosed()
    {
        startButton.onClick.RemoveListener(OnStartButtonClicked);
    }

    private void OnStartButtonClicked()
    {
        GameEventBus.Publish<MenuStartClickedEvent>();
    }
}
