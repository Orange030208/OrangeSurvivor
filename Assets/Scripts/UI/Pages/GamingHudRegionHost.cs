using System;
using TMPro;
using UnityEngine;

public sealed class GamingHudRegionHost
{
    private readonly TextMeshProUGUI waveText;
    private readonly TextMeshProUGUI timerText;
    private readonly TextMeshProUGUI currencyText;
    private readonly CharacterStatusPanel characterStatusPanel;
    private readonly BuffBarUI buffBarUI;
    private readonly UITooltipPresenter tooltipPresenter;

    private PlayerLevel playerLevel;
    private bool bound;

    public GamingHudRegionHost(
        string ownerName,
        TextMeshProUGUI waveText,
        TextMeshProUGUI timerText,
        TextMeshProUGUI currencyText,
        CharacterStatusPanel characterStatusPanel,
        BuffBarUI buffBarUI,
        UITooltipPresenter tooltipPresenter)
    {
        string resolvedOwnerName = string.IsNullOrWhiteSpace(ownerName) ? nameof(GamingHudRegionHost) : ownerName;
        this.waveText = waveText ?? throw new MissingReferenceException($"{nameof(GamingUIPage)} '{resolvedOwnerName}' is missing wave text.");
        this.timerText = timerText ?? throw new MissingReferenceException($"{nameof(GamingUIPage)} '{resolvedOwnerName}' is missing timer text.");
        this.currencyText = currencyText ?? throw new MissingReferenceException($"{nameof(GamingUIPage)} '{resolvedOwnerName}' is missing currency text.");
        this.characterStatusPanel = characterStatusPanel ?? throw new MissingReferenceException($"{nameof(GamingUIPage)} '{resolvedOwnerName}' is missing character status panel.");
        this.buffBarUI = buffBarUI ?? throw new MissingReferenceException($"{nameof(GamingUIPage)} '{resolvedOwnerName}' is missing buff bar UI.");
        this.tooltipPresenter = tooltipPresenter ?? throw new MissingReferenceException($"{nameof(GamingUIPage)} '{resolvedOwnerName}' is missing tooltip presenter.");
    }

    public void Bind(GamingPageContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        Unbind();
        GameEventBus.Subscribe<WaveStartedEvent>(OnWaveStarted);
        GameEventBus.Subscribe<AllWavesCompletedEvent>(OnAllWavesCompleted);
        GameEventBus.Subscribe<WaveProgressEvent>(OnWaveProgress);
        GameEventBus.Subscribe<CurrencyChangedEvent>(OnCurrencyChanged);

        BindPlayer(context.Player);
        RefreshCurrencyDisplay(context.CurrencyWallet);

        buffBarUI.gameObject.SetActive(true);
        tooltipPresenter.gameObject.SetActive(true);

        GameEventBus.Publish<RequestWaveHudSnapshotEvent>();
        bound = true;
    }

    public void Unbind()
    {
        if (bound)
        {
            GameEventBus.Unsubscribe<WaveStartedEvent>(OnWaveStarted);
            GameEventBus.Unsubscribe<AllWavesCompletedEvent>(OnAllWavesCompleted);
            GameEventBus.Unsubscribe<WaveProgressEvent>(OnWaveProgress);
            GameEventBus.Unsubscribe<CurrencyChangedEvent>(OnCurrencyChanged);
            bound = false;
        }

        UnbindPlayerLevel();
        characterStatusPanel.Unbind();
        buffBarUI.UnbindPlayer();
        tooltipPresenter.HideImmediate();
    }

    private void BindPlayer(Player player)
    {
        UnbindPlayerLevel();
        characterStatusPanel.BindPlayer(player);
        buffBarUI.BindPlayer(player);

        playerLevel = player != null ? player.GetComponent<PlayerLevel>() : null;
        if (playerLevel == null)
        {
            return;
        }

        playerLevel.SnapshotChanged += OnPlayerLevelSnapshotChanged;
        OnPlayerLevelSnapshotChanged(playerLevel.CreateSnapshot());
    }

    private void UnbindPlayerLevel()
    {
        if (playerLevel == null)
        {
            return;
        }

        playerLevel.SnapshotChanged -= OnPlayerLevelSnapshotChanged;
        playerLevel = null;
    }

    private void OnCurrencyChanged(CurrencyChangedEvent eventData)
    {
        RefreshCurrencyDisplay(eventData.Wallet);
    }

    private void RefreshCurrencyDisplay(CurrencyWallet wallet)
    {
        currencyText.text = wallet != null ? wallet.CurrentAmount.ToString() : "0";
    }

    private void OnPlayerLevelSnapshotChanged(PlayerLevelSnapshot snapshot)
    {
        characterStatusPanel.SetLevel(snapshot.CurrentLevel);
        characterStatusPanel.SetXp(snapshot.CurrentXP, snapshot.RequiredXP);
        characterStatusPanel.SetUpgradePoint(snapshot.UnspentUpgradePoints);
    }

    private void OnWaveStarted(WaveStartedEvent eventData)
    {
        waveText.text = $"波次 {eventData.CurrentWave}/{eventData.TotalWaves}";
    }

    private void OnAllWavesCompleted()
    {
        waveText.text = "所有波次已完成!";
        timerText.text = string.Empty;
    }

    private void OnWaveProgress(WaveProgressEvent eventData)
    {
        timerText.text = $"{Mathf.RoundToInt(eventData.RemainingTime)}s / {Mathf.RoundToInt(eventData.TotalTime)}s";
    }
}
