using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class DropManager:MonoBehaviour
{
    [SerializeField] private Candy candyPrefab;
    [SerializeField] private Chest chestPrefab;

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

        Collection dropItem;
        int random = Random.Range(1, 101);
        if (random <= 95)
        {
            dropItem = candyPrefab;
        }
        else
        {
            dropItem = chestPrefab;
        }
        Instantiate(dropItem, deadEvent.Position, Quaternion.identity, transform);
    }
}
