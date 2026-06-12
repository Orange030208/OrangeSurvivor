using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using UnityEngine;
using UnityEngine.UI;

public class GameOverUIPage : PageBase
{
    [SerializeField] private Button restartButton;
    [SerializeField] private Button menuButton;

    protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
    {
        restartButton.onClick.AddListener(OnRestartClicked);
        menuButton.onClick.AddListener(OnMenuClicked);
        return UniTask.CompletedTask;
    }

    protected override void OnClosed(CloseReason reason)
    {
        restartButton.onClick.RemoveListener(OnRestartClicked);
        menuButton.onClick.RemoveListener(OnMenuClicked);
    }

    private void OnRestartClicked()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.UiConfirm);
        YokiFrame.EventKit.Enum.Send(GameFlowCommand.GameOverRestartClicked);
    }

    private void OnMenuClicked()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.UiCancel);
        YokiFrame.EventKit.Enum.Send(GameFlowCommand.GameOverReturnToMenuClicked);
    }
}
