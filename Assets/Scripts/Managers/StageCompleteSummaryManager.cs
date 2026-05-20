using UnityEngine;

/// <summary>
/// 单局统计管理器：
/// - 监听运行时事件，汇总本局基础结算数据；
/// - 在结算页打开时提供只读结果；
/// - 只负责统计，不负责切状态与页面开关。
/// </summary>
public class StageCompleteSummaryManager : MonoBehaviour
{
    [SerializeField] private CurrencyWallet wallet;

    private int completedWaves;
    private float survivalTime;
    private int killCount;
    private int goldEarned;
    private string characterName = string.Empty;
    private string mainWeaponName = string.Empty;

    private bool isRunActive;

    private void OnEnable()
    {
        GameEventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        GameEventBus.Subscribe<WaveCompletedEvent>(OnWaveCompleted);
        GameEventBus.Subscribe<EntityDiedEvent>(OnEntityDied);
        GameEventBus.Subscribe<CurrencyChangedEvent>(OnCurrencyChanged);
        GameEventBus.Subscribe<PlayerSpawnedEvent>(OnPlayerSpawned);

        TryBindWallet();
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        GameEventBus.Unsubscribe<WaveCompletedEvent>(OnWaveCompleted);
        GameEventBus.Unsubscribe<EntityDiedEvent>(OnEntityDied);
        GameEventBus.Unsubscribe<CurrencyChangedEvent>(OnCurrencyChanged);
        GameEventBus.Unsubscribe<PlayerSpawnedEvent>(OnPlayerSpawned);
    }

    private void Update()
    {
        if (!isRunActive)
        {
            return;
        }

        survivalTime += Time.unscaledDeltaTime;
    }

    private void OnPlayerSpawned(PlayerSpawnedEvent eventData)
    {
        wallet = eventData.Player != null ? eventData.Player.GetComponent<CurrencyWallet>() : null;
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

        if (eventData.NewState == GameState.GameOver || eventData.NewState == GameState.StageComplete || eventData.NewState == GameState.Menu)
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

    private void OnCurrencyChanged(CurrencyChangedEvent eventData)
    {
        if (!isRunActive || eventData.Wallet != wallet || eventData.ChangeAmount <= 0)
        {
            return;
        }

        goldEarned += eventData.ChangeAmount;
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
        Player player = FindFirstObjectByType<Player>();
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

        wallet = player.GetComponent<CurrencyWallet>();
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
        if (wallet != null)
        {
            return;
        }

        Player player = FindFirstObjectByType<Player>();
        if (player == null)
        {
            return;
        }

        wallet = player.GetComponent<CurrencyWallet>();
    }
}
