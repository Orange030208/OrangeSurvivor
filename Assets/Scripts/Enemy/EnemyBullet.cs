using System;
using UnityEngine;

public class EnemyBullet:Bullet
{
    protected override void OnTrigger(Collider2D collider)
    {
        if (collider.TryGetComponent(out HealthComponent healthComponent))
        {
            healthComponent.TakeDamage(_damage);
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        OnTrigger(collider);
    }
}
