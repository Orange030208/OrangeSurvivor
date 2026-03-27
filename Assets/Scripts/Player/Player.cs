using System;
using UnityEngine;

[RequireComponent(typeof(PlayerHealth))]
public class Player : Entity
{
    [Header("组件")]
    private PlayerHealth playerHealth;
    [SerializeField]private CircleCollider2D collider;
    
    public override Vector2 Center => (Vector2)transform.position + collider.offset;

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
    }

    public void TakeDamage(int damage)
    {
        playerHealth.TakeDamage(damage);
    }
}