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
    private FeatureContext context;

    public override string FeatureTitle => "低血量爆炸";
    public override string FeatureDescription => "生命低于阈值时触发一次范围爆炸，受冷却限制。";
    public override FeatureCategory FeatureCategory => FeatureCategory.Trigger;
    public override FeaturePolarity FeaturePolarity => FeaturePolarity.Positive;

    public override void OnInstall(FeatureContext context)
    {
        this.context = context;
        healthComponent = context?.HealthComponent;
        cooldownEndTime = 0f;

        if (healthComponent != null)
        {
            healthComponent.OnHealthChanged += OnHealthChanged;
        }
    }

    public override void OnUninstall(FeatureContext context)
    {
        if (healthComponent != null)
        {
            healthComponent.OnHealthChanged -= OnHealthChanged;
        }

        this.context = null;
        healthComponent = null;
        cooldownEndTime = 0f;
    }

    private void OnHealthChanged(float currentHealth, float maxHealth)
    {
        TryTrigger(currentHealth, maxHealth);
    }

    private void TryTrigger(float currentHealth, float maxHealth)
    {
        if (context?.OwnerEntity == null || healthComponent == null)
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

        Collider2D[] colliders = Physics2D.OverlapCircleAll(context.Transform.position, explosionRadius, enemyLayerMask);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].TryGetComponent(out HealthComponent targetHealth))
            {
                targetHealth.TakeDamage(new DamageInfo(explosionDamage, targetHealth.transform.position, false));
            }
        }

        cooldownEndTime = Time.time + cooldown;
    }
}
