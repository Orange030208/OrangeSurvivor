using System;
using Orange.GameServices;
using UnityEngine;

/// <summary>
/// 单局结算统计服务。
/// 通过 GameServices 生命周期统一管理，替代场景中的 StageCompleteSummaryManager 壳对象。
/// </summary>
[Serializable]
public sealed class RunSummaryService : GameService
{
    [SerializeField] private CurrencyWallet wallet;

    private int completedWaves;
    private float survivalTime;
    private int killCount;
    private int goldEarned;
    private string characterName = string.Empty;
    private string mainWeaponName = string.Empty;
    private bool isRunActive;

    public override GameServiceTickMode TickMode => GameServiceTickMode.UnscaledUpdate;

    protected override void OnAttach()
    {
        YokiFrame.EventKit.Type.Register<GameStateChangedEvent>(OnGameStateChanged);
        AddCleanup(() => YokiFrame.EventKit.Type.UnRegister<GameStateChangedEvent>(OnGameStateChanged));

        YokiFrame.EventKit.Type.Register<WaveCompletedEvent>(OnWaveCompleted);
        AddCleanup(() => YokiFrame.EventKit.Type.UnRegister<WaveCompletedEvent>(OnWaveCompleted));

        YokiFrame.EventKit.Type.Register<EntityDiedEvent>(OnEntityDied);
        AddCleanup(() => YokiFrame.EventKit.Type.UnRegister<EntityDiedEvent>(OnEntityDied));

        YokiFrame.EventKit.Type.Register<PlayerSpawnedEvent>(OnPlayerSpawned);
        AddCleanup(() => YokiFrame.EventKit.Type.UnRegister<PlayerSpawnedEvent>(OnPlayerSpawned));

        TryBindWallet();
    }

    protected override void OnUpdate(float deltaTime)
    {
        if (isRunActive)
        {
            survivalTime += deltaTime;
        }
    }

    protected override void OnDispose()
    {
        UnbindWallet();
    }

    public StageCompleteResult CreateResult()
    {
        CaptureLoadoutSummary();
        return new StageCompleteResult(
            completedWaves,
            survivalTime,
            killCount,
            goldEarned,
            characterName,
            mainWeaponName);
    }

    private void OnPlayerSpawned(PlayerSpawnedEvent eventData)
    {
        BindWallet(eventData.Player != null ? eventData.Player.GetComponent<CurrencyWallet>() : null);
    }

    private void OnGameStateChanged(GameStateChangedEvent eventData)
    {
        if (eventData.NewState == GameState.Game && eventData.OldState != GameState.Shop)
        {
            ResetSummary();
            CaptureLoadoutSummary();
            TryBindWallet();
            isRunActive = true;
            return;
        }

        if (eventData.NewState == GameState.GameOver ||
            eventData.NewState == GameState.StageComplete ||
            eventData.NewState == GameState.Menu)
        {
            isRunActive = false;
        }
    }

    private void OnWaveCompleted(WaveCompletedEvent eventData)
    {
        completedWaves = Mathf.Max(completedWaves, eventData.WaveNumber);
    }

    private void OnEntityDied(EntityDiedEvent eventData)
    {
        if (!isRunActive || eventData.Reason == EntityDeathReason.WaveCleanup || eventData.Entity is not Enemy)
        {
            return;
        }

        killCount++;
    }

    private void OnCurrencyAmountChanged(int currentAmount, int changeAmount)
    {
        if (!isRunActive || changeAmount <= 0)
        {
            return;
        }

        goldEarned += changeAmount;
    }

    private void ResetSummary()
    {
        completedWaves = 0;
        survivalTime = 0f;
        killCount = 0;
        goldEarned = 0;
        characterName = string.Empty;
        mainWeaponName = string.Empty;
    }

    private void CaptureLoadoutSummary()
    {
        Player player = UnityEngine.Object.FindFirstObjectByType<Player>();
        CharacterDataSO characterData = player != null ? player.CharacterData : null;
        if (characterData == null && GameContentRuntime.TryGetProvider(out IGameContentProvider provider))
        {
            characterData = provider.DefaultCharacter;
        }

        characterName = characterData != null ? characterData.CharacterName : string.Empty;

        if (player == null)
        {
            return;
        }

        BindWallet(player.GetComponent<CurrencyWallet>());
        WeaponsHolder weaponsHolder = player.GetComponent<WeaponsHolder>();
        if (weaponsHolder == null || weaponsHolder.EquippedWeapons.Count == 0)
        {
            return;
        }

        WeaponDataSO weaponData = weaponsHolder.EquippedWeapons[0].WeaponData;
        mainWeaponName = weaponData != null ? weaponData.ItemName : string.Empty;
    }

    private void TryBindWallet()
    {
        Player player = UnityEngine.Object.FindFirstObjectByType<Player>();
        CurrencyWallet resolvedWallet = player != null ? player.GetComponent<CurrencyWallet>() : null;
        if (resolvedWallet == null)
        {
            resolvedWallet = wallet;
        }

        if (resolvedWallet != null)
        {
            BindWallet(resolvedWallet);
        }
    }

    private void BindWallet(CurrencyWallet newWallet)
    {
        UnbindWallet();
        wallet = newWallet;
        if (wallet != null)
        {
            wallet.OnAmountChanged += OnCurrencyAmountChanged;
        }
    }

    private void UnbindWallet()
    {
        if (wallet == null)
        {
            return;
        }

        wallet.OnAmountChanged -= OnCurrencyAmountChanged;
        wallet = null;
    }
}
