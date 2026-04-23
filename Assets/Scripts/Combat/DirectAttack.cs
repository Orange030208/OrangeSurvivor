using UnityEngine;

public sealed class DirectAttack : AttackBase
{
    private float damage = 1f;
    private float attackFrequency = 1f;

    public void SetDamage(float value)
    {
        damage = Mathf.Max(0f, value);
    }

    public void SetAttackFrequency(float value)
    {
        attackFrequency = Mathf.Max(0.01f, value);
    }

    protected override float GetAttackInterval()
    {
        return 1f / Mathf.Max(0.01f, attackFrequency);
    }

    protected override void ExecuteAttack(Entity target)
    {
        if (target == null)
        {
            return;
        }

        HealthComponent healthComponent = target.GetComponent<HealthComponent>();
        if (healthComponent == null || healthComponent.OwnerEntity == null)
        {
            return;
        }

        HitSpec hitSpec = new HitSpec(Mathf.Max(0f, damage), 0f, 1f);
        HitService.Apply(new HitRequest(
            Owner,
            healthComponent.OwnerEntity,
            hitSpec,
            healthComponent.transform.position,
            HitSourceKind.Direct,
            GetType().Name));
    }
}
