using System;
using UnityEngine;

[Serializable]
public sealed class HeartSteelLivingAlloyFeature : HitModifierFeatureBase
{
    [SerializeField] private string targetWeaponId = "Weapon_NeonShield";
    [SerializeField, Min(0f)] private float damageBonusPercentPer100MaxHealth = 8f;

    public HeartSteelLivingAlloyFeature()
    {
        hitModifierTiming = HitModifierTiming.Deal;
    }

    public override int HitPriority => HitModifierPriority.Parameter;
    public override string Title => "活体合金";
    public override string Description => BuildDescription();

    private float DamageBonusPercentPer100MaxHealth => Mathf.Max(0f, damageBonusPercentPer100MaxHealth);

    public override void ModifyHit(HitContext hitContext)
    {
        if (hitContext == null ||
            hitContext.IsCancelled ||
            hitContext.IsDodged ||
            hitContext.IsBlocked ||
            hitContext.Request.Source != Context?.OwnerEntity ||
            hitContext.Request.DamageSource is not Weapon weapon ||
            !IsTargetWeapon(weapon))
        {
            return;
        }

        float damageBonusPercent = CalculateDamageBonusPercent();
        if (damageBonusPercent <= 0f)
        {
            return;
        }

        hitContext.Damage *= 1f + PropValueUtility.PercentPointsToRatio(damageBonusPercent);
    }

    private bool IsTargetWeapon(Weapon weapon)
    {
        return weapon != null &&
               weapon.WeaponData != null &&
               !string.IsNullOrWhiteSpace(targetWeaponId) &&
               string.Equals(weapon.WeaponData.WeaponId, targetWeaponId, StringComparison.Ordinal);
    }

    private float CalculateDamageBonusPercent()
    {
        return ResolveCurrentMaxHealth() / 100f * DamageBonusPercentPer100MaxHealth;
    }

    private float ResolveCurrentMaxHealth()
    {
        HealthComponent healthComponent = Context?.HealthComponent;
        if (healthComponent != null)
        {
            return healthComponent.MaxHealth;
        }

        AttributeManager AttributeManager = Context?.AttributeManager;
        return AttributeManager != null
            ? AttributeManager.GetAttributeValue(PropType.MaxHealth)
            : 0f;
    }

    private string BuildDescription()
    {
        string weaponText = string.IsNullOrWhiteSpace(targetWeaponId) ? "目标武器" : targetWeaponId;
        return $"{weaponText} 伤害提高；每 100 最大生命使伤害 +{DamageBonusPercentPer100MaxHealth:0.##}%。";
    }
}
