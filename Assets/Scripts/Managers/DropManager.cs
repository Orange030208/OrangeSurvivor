using UnityEngine;
using Random = UnityEngine.Random;

public class DropManager : MonoBehaviour
{
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

        CollectionSO dropSO;
        int random = Random.Range(1, 101);
        if (random <= 95)
        {
            dropSO = coinSO;
        }
        else
        {
            dropSO = chestSO;
        }

        if (dropSO == null || dropSO.prefab == null)
        {
            Debug.LogError($"[DropManager] {dropSO?.name} has no prefab assigned.", this);
            return;
        }

        Collection instance = Instantiate(dropSO.prefab, deadEvent.Position, Quaternion.identity, transform);
        instance.Configure(dropSO);
    }
}
