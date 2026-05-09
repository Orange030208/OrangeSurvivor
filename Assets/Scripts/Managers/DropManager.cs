using UnityEngine;

public class DropManager : MonoBehaviour
{
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

        CollectionSO dropSO = RollDrop(deadEvent.Source);

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
    }

    private CollectionSO RollDrop(Entity source)
    {
        ContentFactSource factSource = CreateDropFactSource(source);
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

    private static ContentFactSource CreateDropFactSource(Entity source)
    {
        if (source is Player player)
        {
            return ContentFactSource.ForPlayer(player);
        }

        ContentFactSource factSource = new();
        if (source != null && source.TryGetComponent(out PropertiesManager propertiesManager))
        {
            factSource.PropertiesManager = propertiesManager;
        }

        return factSource;
    }
}
