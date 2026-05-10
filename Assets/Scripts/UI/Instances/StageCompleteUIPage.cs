using System;
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

    protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
    {
        restartButton.OnClicked += OnRestartClicked;
        menuButton.OnClicked += OnMenuClicked;
        StageCompletePageContext pageContext = context.GetPayload<StageCompletePageContext>()
            ?? throw new InvalidOperationException($"{nameof(StageCompleteUIPage)} requires {nameof(StageCompletePageContext)} payload.");
        ApplyResult(pageContext.Result);
        return UniTask.CompletedTask;
    }

    protected override void OnClosed(CloseReason reason)
    {
        restartButton.OnClicked -= OnRestartClicked;
        menuButton.OnClicked -= OnMenuClicked;
    }

    private void ApplyResult(StageCompleteResult result)
    {
        if (completedWavesText != null)
        {
            completedWavesText.text = result.CompletedWaves.ToString();
        }

        if (survivalTimeText != null)
        {
            survivalTimeText.text = FormatDuration(result.SurvivalTime);
        }

        if (killCountText != null)
        {
            killCountText.text = result.KillCount.ToString();
        }

        if (goldEarnedText != null)
        {
            goldEarnedText.text = result.GoldEarned.ToString();
        }

        if (characterNameText != null)
        {
            characterNameText.text = string.IsNullOrWhiteSpace(result.CharacterName) ? "-" : result.CharacterName;
        }

        if (mainWeaponNameText != null)
        {
            mainWeaponNameText.text = string.IsNullOrWhiteSpace(result.MainWeaponName) ? "-" : result.MainWeaponName;
        }

        if (summaryText != null)
        {
            summaryText.text =
                $"\u5b8c\u6210\u6ce2\u6570: {result.CompletedWaves}\n" +
                $"\u751f\u5b58\u65f6\u95f4: {FormatDuration(result.SurvivalTime)}\n" +
                $"\u51fb\u6740\u6570: {result.KillCount}\n" +
                $"\u83b7\u5f97\u91d1\u5e01: {result.GoldEarned}\n" +
                $"\u89d2\u8272: {(string.IsNullOrWhiteSpace(result.CharacterName) ? "-" : result.CharacterName)}\n" +
                $"\u4e3b\u6b66\u5668: {(string.IsNullOrWhiteSpace(result.MainWeaponName) ? "-" : result.MainWeaponName)}";
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
        AudioSfxBridge.RequestPlay(AudioSfxKey.UiConfirm);
        GameEventBus.Publish<StageCompleteRestartClickedEvent>();
    }

    private void OnMenuClicked()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.UiCancel);
        GameEventBus.Publish<StageCompleteReturnToMenuClickedEvent>();
    }

}
