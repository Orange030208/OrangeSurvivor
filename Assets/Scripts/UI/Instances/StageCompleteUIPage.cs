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

    protected override void OnPageOpened(UIPageOpenContext context)
    {
        GameEventBus.Subscribe<StageCompleteSnapshotEvent>(OnStageCompleteSnapshot);
        restartButton.OnClicked += OnRestartClicked;
        menuButton.OnClicked += OnMenuClicked;
        GameEventBus.Publish<RequestStageCompleteSnapshotEvent>();
    }

    protected override void OnPageClosed()
    {
        GameEventBus.Unsubscribe<StageCompleteSnapshotEvent>(OnStageCompleteSnapshot);
        restartButton.OnClicked -= OnRestartClicked;
        menuButton.OnClicked -= OnMenuClicked;
    }

    private void OnStageCompleteSnapshot(StageCompleteSnapshotEvent eventData)
    {
        if (completedWavesText != null)
        {
            completedWavesText.text = eventData.CompletedWaves.ToString();
        }

        if (survivalTimeText != null)
        {
            survivalTimeText.text = FormatDuration(eventData.SurvivalTime);
        }

        if (killCountText != null)
        {
            killCountText.text = eventData.KillCount.ToString();
        }

        if (goldEarnedText != null)
        {
            goldEarnedText.text = eventData.GoldEarned.ToString();
        }

        if (characterNameText != null)
        {
            characterNameText.text = string.IsNullOrWhiteSpace(eventData.CharacterName) ? "-" : eventData.CharacterName;
        }

        if (mainWeaponNameText != null)
        {
            mainWeaponNameText.text = string.IsNullOrWhiteSpace(eventData.MainWeaponName) ? "-" : eventData.MainWeaponName;
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
        GameEventBus.Publish<StageCompleteRestartClickedEvent>();
    }

    private void OnMenuClicked()
    {
        GameEventBus.Publish<StageCompleteReturnToMenuClickedEvent>();
    }
}
