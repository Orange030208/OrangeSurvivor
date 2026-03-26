using System;
using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(EnemyMovement))]
public class Enemy : MonoBehaviour
{
    private Player _player;
    private EnemyMovement _movement;
    [SerializeField] private ParticleSystem passAwayParticles;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private SpriteRenderer spawnIndicator;
    private bool _hasSpawned = false;
    
    [Header("攻击")]
    [SerializeField] private int damage;
    [SerializeField] private float attackFrequency;
    [SerializeField] private float attackDetectionRadius;
    private float attackDelay;
    private float attackTimer;

    private void Start()
    {
        _player = FindObjectOfType<Player>();

        _movement = GetComponent<EnemyMovement>();

        if (_player == null)
        {
            Debug.LogError("Player not found");
        }

        SetRendersVisibility(false);
        
        transform.DOScale(1.2f, .3f).SetLoops(5, LoopType.Yoyo).SetEase(Ease.InOutSine)
            .OnComplete(SpawnSequenceComplete);
        
        attackDelay = 1f / attackFrequency;
        Debug.Log($"攻击延迟{attackDelay}");
    }

    private void Update()
    {
        if (attackTimer >= attackDelay)
        {
            TryAttack();
        }
        else
        {
            Wait();
        }
    }

    private void SpawnSequenceComplete()
    {
        SetRendersVisibility(true);
        _hasSpawned = true;
        _movement.SetTarget(_player);
    }

    private void SetRendersVisibility(bool visible)
    {
        spriteRenderer.enabled = visible;
        spawnIndicator.enabled = !visible;
    }
    
    private void Wait()
    {
        attackTimer += Time.deltaTime;
    }
    
    private void TryAttack()
    {
        float distanceToPlayer = Vector2.Distance(_player.transform.position, transform.position);

        if (distanceToPlayer < attackDetectionRadius)
        {
            Attack();
        }
    }
    
    private void Attack()
    {
        Debug.Log($"攻击 伤害为{damage}");
        attackTimer = 0;
    }

    private void PassAway()
    {
        passAwayParticles.transform.SetParent(null);
        passAwayParticles.Play();

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDetectionRadius);
    }
}