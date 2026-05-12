using UnityEngine;

public class DropManager : MonoBehaviour
{
    private static readonly CoinRewardData FixedCoinReward = new(1, 1);

    [SerializeField] private ContentPoolSO dropPool;

    private readonly ContentPoolRollService contentPoolRollService = new();
    private readonly ContentPoolRuntimeState dropRuntimeState = new();

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
        if (deadEvent.Entity is not Enemy)
        {
            return;
        }

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

    private CollectionSO RollDrop(Entity source, int waveNumber)
    {
        ContentFactSource factSource = CreateDropFactSource(source, waveNumber);
        ContentPoolSO configuredPool = ResolveConfiguredDropPool();
        if (configuredPool == null)
        {
            Debug.LogError($"[DropManager] Missing drop content pool in scene or {nameof(GameContentCatalogSO)}.", this);
            return null;
        }

        ContentRollResult configuredResult = contentPoolRollService.Roll(
            configuredPool,
            factSource,
            dropRuntimeState,
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

    private static ContentFactSource CreateDropFactSource(Entity source, int waveNumber)
    {
        if (source is Player player)
        {
            return ContentFactSource.ForPlayer(player, waveNumber);
        }

        ContentFactSource factSource = new();
        factSource.WaveNumber = Mathf.Max(1, waveNumber);
        if (source != null && source.TryGetComponent(out PropertiesManager propertiesManager))
        {
            factSource.PropertiesManager = propertiesManager;
        }

        return factSource;
    }

}
