using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using TMPro;
using UnityEngine;

public class StageCompleteUIPage : PageBase
{
    [SerializeField] private UIClickTarget restartButton;
    [SerializeField] private UIClickTarget menuButton;
    [SerializeField] private TextMeshProUGUI completedWavesText;
    [SerializeField] private TextMeshProUGUI survivalTimeText;
    [SerializeField] private TextMeshProUGUI killCountText;
    [SerializeField] private TextMeshProUGUI goldEarnedText;
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private TextMeshProUGUI mainWeaponNameText;
    [SerializeField] private TextMeshProUGUI summaryText;
    [SerializeField] private StageCompleteSummaryManager summaryManager;

    protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
    {
        restartButton.OnClicked += OnRestartClicked;
        menuButton.OnClicked += OnMenuClicked;
        RenderSnapshot();
        return UniTask.CompletedTask;
    }

    protected override void OnClosed(CloseReason reason)
    {
        restartButton.OnClicked -= OnRestartClicked;
        menuButton.OnClicked -= OnMenuClicked;
    }

    private void RenderSnapshot()
    {
        StageCompleteSummaryManager manager = ResolveSummaryManager();
        if (manager == null)
        {
            return;
        }

        ApplySnapshot(manager.CreateSnapshot());
    }

    private void ApplySnapshot(StageCompleteSnapshot snapshot)
    {
        if (completedWavesText != null)
        {
            completedWavesText.text = snapshot.CompletedWaves.ToString();
        }

        if (survivalTimeText != null)
        {
            survivalTimeText.text = FormatDuration(snapshot.SurvivalTime);
        }

        if (killCountText != null)
        {
            killCountText.text = snapshot.KillCount.ToString();
        }

        if (goldEarnedText != null)
        {
            goldEarnedText.text = snapshot.GoldEarned.ToString();
        }

        if (characterNameText != null)
        {
            characterNameText.text = string.IsNullOrWhiteSpace(snapshot.CharacterName) ? "-" : snapshot.CharacterName;
        }

        if (mainWeaponNameText != null)
        {
            mainWeaponNameText.text = string.IsNullOrWhiteSpace(snapshot.MainWeaponName) ? "-" : snapshot.MainWeaponName;
        }

        if (summaryText != null)
        {
            summaryText.text =
                $"\u5b8c\u6210\u6ce2\u6570: {snapshot.CompletedWaves}\n" +
                $"\u751f\u5b58\u65f6\u95f4: {FormatDuration(snapshot.SurvivalTime)}\n" +
                $"\u51fb\u6740\u6570: {snapshot.KillCount}\n" +
                $"\u83b7\u5f97\u91d1\u5e01: {snapshot.GoldEarned}\n" +
                $"\u89d2\u8272: {(string.IsNullOrWhiteSpace(snapshot.CharacterName) ? "-" : snapshot.CharacterName)}\n" +
                $"\u4e3b\u6b66\u5668: {(string.IsNullOrWhiteSpace(snapshot.MainWeaponName) ? "-" : snapshot.MainWeaponName)}";
        }
    }

    private static string FormatDuration(float durationSeconds)
    {
        int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(durationSeconds));
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        return $"{minutes:00}:{seconds:00}";
    }

    private void OnRestartClicked()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.WoodenButtonClicked);
        GameEventBus.Publish<StageCompleteRestartClickedEvent>();
    }

    private void OnMenuClicked()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.WoodenButtonClicked);
        GameEventBus.Publish<StageCompleteReturnToMenuClickedEvent>();
    }

    private StageCompleteSummaryManager ResolveSummaryManager()
    {
        if (summaryManager != null)
        {
            return summaryManager;
        }

        summaryManager = FindFirstObjectByType<StageCompleteSummaryManager>();
        return summaryManager;
    }
}
