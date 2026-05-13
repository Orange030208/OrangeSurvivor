using System;
using UnityEngine;

[Serializable]
public sealed class LethalDamageGuardFeature : FeatureEffectBase
{
    [SerializeField] private float shieldAmount = 500f;
    [SerializeField] private float cooldownSeconds = 30f;

    private float currentShieldHealth;
    private float cooldownEndTime;

    public LethalDamageGuardFeature()
    {
        hitModifierTiming = HitModifierTiming.Receive;
    }

    public override bool CanModifyHit => true;
    public override int HitPriority => HitModifierPriority.Finally - 1;
    public override string Description => $"濒死时阻挡本次伤害，并获得一层吸收 {shieldAmount:0.#} 点伤害的护盾，冷却 {cooldownSeconds:0.#} 秒。";

    public override void OnInstall()
    {
        currentShieldHealth = 0f;
        cooldownEndTime = 0f;
    }

    public override void OnUninstall()
    {
        currentShieldHealth = 0f;
        cooldownEndTime = 0f;
    }

    public override void ModifyHit(HitContext hitContext)
    {
        HealthComponent healthComponent = Context?.HealthComponent;
        if (hitContext == null ||
            hitContext.IsCancelled ||
            hitContext.IsDodged ||
            hitContext.IsBlocked ||
            healthComponent == null)
        {
            return;
        }

        float finalDamage = PredictFinalDamage(hitContext);
        if (finalDamage <= 0f)
        {
            return;
        }

        if (TryAbsorbWithActiveShield(hitContext, finalDamage))
        {
            return;
        }

        if (!CanActivateShield(finalDamage, healthComponent))
        {
            return;
        }

        ActivateShield();
        hitContext.IsBlocked = true;
        hitContext.Damage = 0f;
    }

    private bool TryAbsorbWithActiveShield(HitContext hitContext, float finalDamage)
    {
        if (currentShieldHealth <= 0f)
        {
            return false;
        }

        float absorbedDamage = Mathf.Min(finalDamage, currentShieldHealth);
        currentShieldHealth -= absorbedDamage;
        ApplyFinalDamageOverride(hitContext, finalDamage - absorbedDamage);
        return true;
    }

    private bool CanActivateShield(float finalDamage, HealthComponent healthComponent)
    {
        return currentShieldHealth <= 0f &&
               Time.time >= cooldownEndTime &&
               finalDamage >= healthComponent.CurrentHealth;
    }

    private void ActivateShield()
    {
        currentShieldHealth = Mathf.Max(0f, shieldAmount);
        cooldownEndTime = Time.time + Mathf.Max(0f, cooldownSeconds);
    }

    private static void ApplyFinalDamageOverride(HitContext hitContext, float finalDamage)
    {
        float multiplier = GetFinalDamageMultiplier(hitContext);
        if (multiplier <= 0f || finalDamage <= 0f)
        {
            hitContext.IsBlocked = true;
            hitContext.Damage = 0f;
            return;
        }

        hitContext.Damage = finalDamage / multiplier;
        hitContext.IsBlocked = false;
    }

    private static float PredictFinalDamage(HitContext hitContext)
    {
        float multiplier = GetFinalDamageMultiplier(hitContext);
        if (multiplier <= 0f)
        {
            return 0f;
        }

        return PropValueUtility.ClampNonNegative(hitContext.Damage) * multiplier;
    }

    private static float GetFinalDamageMultiplier(HitContext hitContext)
    {
        float critMultiplier = hitContext.IsCritical
            ? PropValueUtility.ClampEffectiveCriticalMultiplier(hitContext.CritMultiplier)
            : 1f;
        float damageReductionMultiplier = PropValueUtility.ClampNonNegative(1f - hitContext.DamageReduction);
        return critMultiplier * damageReductionMultiplier;
    }
}
