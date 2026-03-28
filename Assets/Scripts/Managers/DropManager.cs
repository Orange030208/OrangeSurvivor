using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class DropManager:MonoBehaviour
{
    [SerializeField] private Candy candyPrefab;
    [SerializeField] private Cash cashPrefab;
    [SerializeField] private Chest chestPrefab;

    private void OnEnable()
    {
        Enemy.onDeath += EnemyPassAwayCallback;
    }

    private void OnDisable()
    {
        Enemy.onDeath -= EnemyPassAwayCallback;
    }
    
    private void EnemyPassAwayCallback(DeadInfo deadInfo)
    {
        Collector dropItem;
        int random = Random.Range(1, 101);
        if (random <= 33)
        {
            dropItem = candyPrefab;
        }
        else if (random <= 66)
        {
            dropItem = cashPrefab;
        }
        else
        {
            dropItem = chestPrefab;
        }
        Instantiate(dropItem,deadInfo.deadPosition,Quaternion.identity,transform);
    }
}