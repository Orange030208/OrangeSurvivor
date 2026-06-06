using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GamingUIPage : PageBase
{
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI currencyText;
    [SerializeField] private PlayerHealthPanel playerHealthPanel;
    [SerializeField] private PlayerExperiencePanel playerExperiencePanel;
    [SerializeField] private Button menuButton;
    [SerializeField] private BuffBarUI buffBarUI;

    private GamingPageContext currentContext;
    private PlayerLevel playerLevel;
    private CurrencyWallet currencyWallet;
    private bool hudEventsBound;

    protected override void OnCreate()
    {
        base.OnCreate();
        ValidateConfiguration();
    }

    protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
    {
        currentContext = context.GetPayload<GamingPageContext>()
            ?? throw new InvalidOperationException($"{nameof(GamingUIPage)} requires {nameof(GamingPageContext)} payload.");

        BindHud(currentContext);
        GameInput input = GameInput.Instance;
        if (input != null)
        {
            input.PausePerformed += OnPauseClicked;
        }
        menuButton.onClick.AddListener(OnPauseClicked);
        return UniTask.CompletedTask;
    }

    protected override void OnClosed(CloseReason reason)
    {
        UnbindHud();
        GameInput input = GameInput.Instance;
        if (input != null)
        {
            input.PausePerformed -= OnPauseClicked;
        }

        menuButton.onClick.RemoveListener(OnPauseClicked);
        currentContext = null;
    }

    private void OnPauseClicked()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.UiConfirm);
        GameEventBus.Publish(new PauseGameRequestedEvent());
    }


    private void BindHud(GamingPageContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        UnbindHud();
        GameEventBus.Subscribe<WaveStartedEvent>(OnWaveStarted);
        GameEventBus.Subscribe<AllWavesCompletedEvent>(OnAllWavesCompleted);
        GameEventBus.Subscribe<WaveProgressEvent>(OnWaveProgress);
        hudEventsBound = true;

        BindPlayerHud(context.Player);
        BindCurrencyWallet(context.CurrencyWallet);
        ApplyWaveHud(context.WaveHudViewData);
    }

    private void UnbindHud()
    {
        UnbindCurrencyWallet();

        if (hudEventsBound)
        {
            GameEventBus.Unsubscribe<WaveStartedEvent>(OnWaveStarted);
            GameEventBus.Unsubscribe<AllWavesCompletedEvent>(OnAllWavesCompleted);
            GameEventBus.Unsubscribe<WaveProgressEvent>(OnWaveProgress);
            hudEventsBound = false;
        }

        UnbindPlayerLevel();
        playerHealthPanel.Unbind();
        playerExperiencePanel.Unbind();
        buffBarUI.EndSession();
    }

    private void BindPlayerHud(Player player)
    {
        UnbindPlayerLevel();
        playerHealthPanel.BindPlayer(player);
        buffBarUI.BeginSession(player);

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

    private void OnCurrencyAmountChanged(int currentAmount, int changeAmount)
    {
        RefreshCurrencyDisplay(currencyWallet);
    }

    private void BindCurrencyWallet(CurrencyWallet newCurrencyWallet)
    {
        UnbindCurrencyWallet();
        currencyWallet = newCurrencyWallet;

        if (currencyWallet != null)
        {
            currencyWallet.OnAmountChanged += OnCurrencyAmountChanged;
        }

        RefreshCurrencyDisplay(currencyWallet);
    }

    private void UnbindCurrencyWallet()
    {
        if (currencyWallet == null)
        {
            return;
        }

        currencyWallet.OnAmountChanged -= OnCurrencyAmountChanged;
        currencyWallet = null;
    }

    private void RefreshCurrencyDisplay(CurrencyWallet wallet)
    {
        currencyText.text = wallet != null ? wallet.CurrentAmount.ToString() : "0";
    }

    private void OnPlayerLevelSnapshotChanged(PlayerLevelSnapshot snapshot)
    {
        playerExperiencePanel.SetLevel(snapshot.CurrentLevel);
        playerExperiencePanel.SetExperience(snapshot.CurrentXP, snapshot.RequiredXP);
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
        ApplyWaveTimer(eventData.ShowTimer, eventData.RemainingTime, eventData.TotalTime);
    }

    private void ApplyWaveHud(WaveHudViewData waveHudViewData)
    {
        if (waveHudViewData.HasStarted)
        {
            waveText.text = $"波次 {waveHudViewData.CurrentWave}/{waveHudViewData.TotalWaves}";
            ApplyWaveTimer(waveHudViewData.ShowTimer, waveHudViewData.RemainingTime, waveHudViewData.TotalTime);
            return;
        }

        waveText.text = "准备开始";
        timerText.text = string.Empty;
    }

    private void ApplyWaveTimer(bool showTimer, float remainingTime, float totalTime)
    {
        timerText.text = showTimer
            ? $"{Mathf.RoundToInt(remainingTime)}s / {Mathf.RoundToInt(totalTime)}s"
            : string.Empty;
    }

    private void ValidateConfiguration()
    {
        if (waveText == null)
        {
            throw new MissingReferenceException($"{nameof(GamingUIPage)} '{name}' is missing wave text.");
        }

        if (timerText == null)
        {
            throw new MissingReferenceException($"{nameof(GamingUIPage)} '{name}' is missing timer text.");
        }

        if (playerHealthPanel == null)
        {
            throw new MissingReferenceException($"{nameof(GamingUIPage)} '{name}' is missing player health panel.");
        }

        if (playerExperiencePanel == null)
        {
            throw new MissingReferenceException($"{nameof(GamingUIPage)} '{name}' is missing player experience panel.");
        }

        if (currencyText == null)
        {
            throw new MissingReferenceException($"{nameof(GamingUIPage)} '{name}' is missing currency text.");
        }

        if (menuButton == null)
        {
            throw new MissingReferenceException($"{nameof(GamingUIPage)} '{name}' is missing menu button.");
        }

        if (buffBarUI == null)
        {
            throw new MissingReferenceException($"{nameof(GamingUIPage)} '{name}' is missing buff bar UI.");
        }
    }
}
