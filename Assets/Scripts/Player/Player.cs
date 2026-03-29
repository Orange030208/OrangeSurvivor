using System;
using UnityEngine;

[RequireComponent(typeof(PlayerHealth), typeof(PlayerLevel))]
public class Player : Entity
{
    [Header("组件")]
    private PlayerHealth _playerHealth;
    private PlayerLevel _playerLevel;
    [SerializeField]private CircleCollider2D collider;
    
    public override Vector2 Center => (Vector2)transform.position + collider.offset;
    
    public bool IsLevelUpInCurrentWave => _playerLevel.IsLevelUpInCurrentWave;
    
    public int LevelUpValue => _playerLevel.LevelUpValue;

    private void Awake()
    {
        _playerHealth = GetComponent<PlayerHealth>();
        _playerLevel = GetComponent<PlayerLevel>();
    }

    public void TakeDamage(int damage)
    {
        _playerHealth.TakeDamage(damage);
    }
    
}