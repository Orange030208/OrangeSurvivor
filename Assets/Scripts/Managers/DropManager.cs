using UnityEngine;

public class DropManager : MonoBehaviour
{
    private const int BASE_KILL_EXPERIENCE = 1;

    private static readonly CoinRewardData FixedCoinReward = new(1);

    [SerializeField] private ContentPoolSO dropPool;

    private readonly ContentPoolRollService contentPoolRollService = new();
    private readonly ContentHistoryState contentHistoryState = new();

    private void OnEnable()
    {
        GameEventBus.Subscribe<EntityDiedEvent>(OnEntityDied);
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<EntityDiedEvent>(OnEntityDied);
    }

    private void OnEntityDied(EntityDiedEvent deadEvent)
    {
        if (deadEvent.Reason == EntityDeathReason.WaveCleanup)
        {
            return;
        }

        if (deadEvent.Entity is not Enemy)
        {
            return;
        }

        TryGrantKillExperience(deadEvent.Source);

        RunProgressionSnapshot progressionSnapshot = RunProgressionRuntime.CurrentSnapshot;
        CollectionSO dropSO = RollDrop(deadEvent.Source, progressionSnapshot.WaveNumber);

        if (dropSO == null)
        {
            return;
        }

        if (dropSO.prefab == null)
        {
            Debug.LogError($"[DropManager] {dropSO?.name} has no prefab assigned.", this);
            return;
        }

        Collection instance = Instantiate(dropSO.prefab, deadEvent.Position, Quaternion.identity, transform);
        instance.Configure(dropSO);
        if (dropSO.prefab is Coin && instance is Coin coin)
        {
            coin.ConfigureReward(FixedCoinReward);
        }
    }

    public static bool TryGrantKillExperience(Entity source)
    {
        return TryGrantKillExperience(source, BASE_KILL_EXPERIENCE);
    }

    public static bool TryGrantKillExperience(Entity source, int baseExperience)
    {
        PlayerLevel playerLevel = ResolvePlayerLevel(source);
        if (playerLevel == null || baseExperience <= 0)
        {
            return false;
        }

        playerLevel.AddXP(baseExperience);
        return true;
    }

    private static PlayerLevel ResolvePlayerLevel(Entity source)
    {
        if (source == null)
        {
            return null;
        }

        if (source.TryGetComponent(out PlayerLevel playerLevel))
        {
            return playerLevel;
        }

        if (source is Weapon weapon && weapon.Owner != null &&
            weapon.Owner.TryGetComponent(out PlayerLevel ownerPlayerLevel))
        {
            return ownerPlayerLevel;
        }

        return null;
    }

    private CollectionSO RollDrop(Entity source, int waveNumber)
    {
        ContentPoolSO configuredPool = ResolveConfiguredDropPool();
        if (configuredPool == null)
        {
            Debug.LogError($"[DropManager] Missing drop content pool in scene or {nameof(GameContentCatalogSO)}.", this);
            return null;
        }

        ContentRollResult configuredResult = contentPoolRollService.Roll(
            configuredPool,
            CreateDropRollContext(configuredPool, source, waveNumber),
            1,
            entry => entry.Content is CollectionSO);
        return configuredResult.HasAny ? configuredResult.Items[0].Content as CollectionSO : null;
    }

    private ContentPoolSO ResolveConfiguredDropPool()
    {
        if (dropPool != null)
        {
            return dropPool;
        }

        if (GameContentRuntime.TryGetProvider(out IGameContentProvider provider) && provider.DropPool != null)
        {
            return provider.DropPool;
        }

        return null;
    }

    private ContentRollContext CreateDropRollContext(ContentPoolSO pool, Entity source, int waveNumber)
    {
        Player player = source as Player;
        RunProgressionSnapshot snapshot = RunProgressionRuntime.CurrentSnapshot;
        if (snapshot.WaveNumber != Mathf.Max(1, waveNumber))
        {
            snapshot = new RunProgressionSnapshot(
                waveNumber,
                snapshot.TotalWaves,
                snapshot.RunMinutes,
                snapshot.EndlessLoop,
                snapshot.DifficultyCoefficient,
                snapshot.EconomyCoefficient,
                snapshot.ShopPriceMultiplier,
                snapshot.DangerTier);
        }

        return new ContentRollContext(
            ContentPoolScopeIds.Drop,
            player,
            progressionSnapshot: snapshot,
            historyScope: CreateHistoryScope(pool),
            history: contentHistoryState,
            source: source,
            propertiesManager: source != null && source.TryGetComponent(out PropertiesManager propertiesManager)
                ? propertiesManager
                : null);
    }

    private static ContentHistoryScope CreateHistoryScope(ContentPoolSO pool)
    {
        string poolId = pool != null ? pool.name : ContentPoolScopeIds.Drop;
        return new ContentHistoryScope(ContentPoolScopeIds.Drop, poolId);
    }

}
