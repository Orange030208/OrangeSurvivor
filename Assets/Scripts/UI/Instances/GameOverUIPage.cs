using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using UnityEngine;

public class GameOverUIPage : PageBase
{
    [SerializeField] private UIClickTarget restartButton;
    [SerializeField] private UIClickTarget menuButton;

    protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
    {
        restartButton.OnClicked += OnRestartClicked;
        menuButton.OnClicked += OnMenuClicked;
        return UniTask.CompletedTask;
    }

    protected override void OnClosed(CloseReason reason)
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
