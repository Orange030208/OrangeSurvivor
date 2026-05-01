using TMPro;
using UnityEngine;

public class StageCompleteUIPage : UIPageBase
{
    [SerializeField] private UIClickTarget restartButton;
    [SerializeField] private UIClickTarget menuButton;
    [SerializeField] private TextMeshProUGUI completedWavesText;
    [SerializeField] private TextMeshProUGUI survivalTimeText;
    [SerializeField] private TextMeshProUGUI killCountText;
    [SerializeField] private TextMeshProUGUI goldEarnedText;
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private TextMeshProUGUI mainWeaponNameText;
    [SerializeField] private StageCompleteSummaryManager summaryManager;

    protected override void OnPageOpened(UIPageOpenContext context)
    {
        restartButton.OnClicked += OnRestartClicked;
        menuButton.OnClicked += OnMenuClicked;
        RenderSnapshot();
    }

    protected override void OnPageClosed()
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
