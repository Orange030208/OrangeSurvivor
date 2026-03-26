using System;
using UnityEngine;
using UnityEngine.Serialization;

public class EnemyMovement:MonoBehaviour
{
    private Player _player;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private SpriteRenderer spawnIndicator;
    [SerializeField] private float _speed;

    private void Start()
    {
        _player = FindObjectOfType<Player>();

        if (_player == null)
        {
            Debug.LogError("Player not found");
        }
        
        spriteRenderer.enabled = false;
        spawnIndicator.enabled = true;
        
    }

    private void Update()
    {
        FollowPlayer();

        TryAttack();
    }

    private void TryAttack()
    {
    }

    private void FollowPlayer()
    {
        Vector2 direction = (_player.transform.position - transform.position).normalized;
        Vector2 targetPos = (Vector2)transform.position + direction * _speed * Time.deltaTime;
        transform.position = targetPos;
    }
}