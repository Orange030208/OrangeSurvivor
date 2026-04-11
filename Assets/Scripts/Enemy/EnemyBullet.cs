using UnityEngine;

public class EnemyBullet : Bullet
{
    protected override void OnTriggerEnter2D(Collider2D collider)
    {
        if (!collider.TryGetComponent(out HealthComponent healthComponent))
        {
            return;
        }

        healthComponent.TakeDamage(launchContext.Hit.Damage);
        Destroy(gameObject);
    }
}
