using UnityEngine;

[System.Serializable]
public class LowHealthExplosionFeatureEffect : FeatureEffectBase
{
    [Range(0.01f, 1f)]
    [SerializeField] private float healthThreshold = 0.3f;
    [SerializeField] private float cooldown = 20f;
    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private float explosionDamage = 20f;
    [SerializeField] private LayerMask enemyLayerMask;

    private float cooldownEndTime;
    private HealthComponent healthComponent;

    public override string Description => "生命低于阈值时触发一次范围爆炸，受冷却限制。";

    public override void OnInstall()
    {
        healthComponent = Context?.HealthComponent;
        cooldownEndTime = 0f;

        if (healthComponent != null)
        {
            healthComponent.OnHealthChanged += OnHealthChanged;
        }
    }

    public override void OnUninstall()
    {
        if (healthComponent != null)
        {
            healthComponent.OnHealthChanged -= OnHealthChanged;
        }
        
        healthComponent = null;
        cooldownEndTime = 0f;
    }

    private void OnHealthChanged(float currentHealth, float maxHealth)
    {
        TryTrigger(currentHealth, maxHealth);
    }

    private void TryTrigger(float currentHealth, float maxHealth)
    {
        if (Context?.OwnerEntity == null || healthComponent == null)
        {
            return;
        }

        if (Time.time < cooldownEndTime)
        {
            return;
        }

        if (maxHealth <= 0f)
        {
            return;
        }

        float healthRatio = currentHealth / maxHealth;
        if (healthRatio > healthThreshold)
        {
            return;
        }
        
        UnityEngine.Debug.Log("爆炸了");

        Collider2D[] colliders = Physics2D.OverlapCircleAll(
            Context.Transform.position,
            explosionRadius,
            ResolveEnemyLayerMask());
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].TryGetComponent(out HealthComponent targetHealth))
            {
                Entity targetEntity = targetHealth.GetComponent<Entity>();
                Vector2 knockbackDirection = targetEntity != null
                    ? targetEntity.Center - (Vector2)Context.Transform.position
                    : (Vector2)(targetHealth.transform.position - Context.Transform.position);
                HitService.Apply(new HitRequest(
                    Context.OwnerEntity,
                    targetEntity,
                    new HitSpec(explosionDamage, 0f, 1f),
                    targetHealth.transform.position,
                    knockbackDirection,
                    HitSourceKind.Explosion,
                    GetType().Name));
            }
        }

        cooldownEndTime = Time.time + cooldown;
    }

    private LayerMask ResolveEnemyLayerMask()
    {
        return enemyLayerMask.value != 0 ? enemyLayerMask : LayerMask.GetMask("Enemy");
    }
}
