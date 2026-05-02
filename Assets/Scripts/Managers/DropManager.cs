using UnityEngine;
using Random = UnityEngine.Random;

public class DropManager : MonoBehaviour
{
    private const float BASE_CHEST_DROP_CHANCE = 0.01f;
    private const float CHEST_DROP_CHANCE_PER_LUCK = 0.00005f;

    [SerializeField] private CollectionSO coinSO;
    [SerializeField] private CollectionSO chestSO;

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

        if (dropSO == null || dropSO.prefab == null)
        {
            Debug.LogError($"[DropManager] {dropSO?.name} has no prefab assigned.", this);
            return;
        }

        Collection instance = Instantiate(dropSO.prefab, deadEvent.Position, Quaternion.identity, transform);
        instance.Configure(dropSO);
    }

    private CollectionSO RollDrop(Entity source)
    {
        float chestDropChance = ResolveChestDropChance(source);
        return Random.value < chestDropChance ? chestSO : coinSO;
    }

    private float ResolveChestDropChance(Entity source)
    {
        float luck = 0f;
        if (source != null && source.TryGetComponent(out PropertiesManager propertiesManager))
        {
            luck = Mathf.Max(0f, propertiesManager.GetPropValue(PropType.Luck));
        }

        return Mathf.Clamp01(BASE_CHEST_DROP_CHANCE + luck * CHEST_DROP_CHANCE_PER_LUCK);
    }
}
