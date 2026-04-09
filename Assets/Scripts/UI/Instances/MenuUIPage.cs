using UnityEngine;
using UnityEngine.UI;

public class MenuUIPage : UIPageBase
{
    [SerializeField] private Button startButton;

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
        GameEventBus.Publish(new GameStateChangeRequestEvent(GameState.WeaponSelection));
    }
}
