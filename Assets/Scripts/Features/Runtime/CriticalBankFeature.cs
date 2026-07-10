using System;
using UnityEngine;

[Serializable]
public sealed class CriticalBankFeature : HitModifierFeatureBase, IDamageDealtFeatureEffect
{
    private enum PendingConsumptionMode
    {
        None = 0,
        FullBank = 1,
        PartialBank = 2
    }

    [SerializeField, Min(1)] private int maxBankPoints = 100;
    [SerializeField, Min(1)] private int pointsPerNonCriticalHit = 20;
    [SerializeField, Min(0f)] private float partialCritDamageBonusPercent = 50f;
    [SerializeField] private HitSourceKind[] allowedSourceKinds =
    {
        HitSourceKind.Weapon,
        HitSourceKind.Projectile
    };

    private int currentBankPoints;
    private PendingConsumptionMode pendingConsumptionMode;
    private int pendingPointsToConsume;
    private Entity pendingTarget;
    private HitSourceKind pendingSourceKind;

    public override int HitPriority => HitModifierPriority.Parameter;
    public int CurrentBankPoints => Mathf.Max(0, currentBankPoints);
    public override string Title => "暴击蓄能";
    public override string Description => BuildDescription();

    public override void OnInstall()
    {
        currentBankPoints = Mathf.Clamp(currentBankPoints, 0, MaxBankPoints);
        ClearPendingConsumption();
    }

    public override void OnUninstall()
    {
        ClearPendingConsumption();
    }

    public override void ModifyHit(HitContext hitContext)
    {
        ClearPendingConsumption();
        if (hitContext == null ||
            hitContext.IsCancelled ||
            hitContext.Request.Source != Context?.OwnerEntity ||
            !FeatureRuntimeUtility.AllowsSourceKind(hitContext.Request.SourceKind, allowedSourceKinds))
        {
            return;
        }

        int safeMaxBankPoints = MaxBankPoints;
        if (currentBankPoints >= safeMaxBankPoints)
        {
            hitContext.IsCritical = true;
            RegisterPendingConsumption(PendingConsumptionMode.FullBank, safeMaxBankPoints, hitContext);
            return;
        }

        if (hitContext.IsCritical && currentBankPoints > 0)
        {
            float fillRatio = Mathf.Clamp01((float)currentBankPoints / safeMaxBankPoints);
            float multiplierBonus = PropValueUtility.PercentPointsToRatio(partialCritDamageBonusPercent) * fillRatio;
            hitContext.CritMultiplier = PropValueUtility.ClampEffectiveCriticalMultiplier(hitContext.CritMultiplier + multiplierBonus);
            RegisterPendingConsumption(PendingConsumptionMode.PartialBank, currentBankPoints, hitContext);
        }
    }

    private int MaxBankPoints => Mathf.Max(1, maxBankPoints);
    private int PointsPerNonCriticalHit => Mathf.Max(1, pointsPerNonCriticalHit);

    public void OnDamageDealt(HitResult result)
    {
        if (result.Source != Context?.OwnerEntity ||
            result.FinalDamage <= 0f ||
            !FeatureRuntimeUtility.AllowsSourceKind(result.SourceKind, allowedSourceKinds))
        {
            return;
        }

        if (TryConsumePending(result))
        {
            ClearPendingConsumption();
            return;
        }

        ClearPendingConsumption();
        if (!result.IsCritical)
        {
            currentBankPoints = Mathf.Min(MaxBankPoints, currentBankPoints + PointsPerNonCriticalHit);
        }
    }

    private void RegisterPendingConsumption(PendingConsumptionMode mode, int pointsToConsume, HitContext hitContext)
    {
        pendingConsumptionMode = mode;
        pendingPointsToConsume = Mathf.Max(0, pointsToConsume);
        pendingTarget = hitContext.Request.Target;
        pendingSourceKind = hitContext.Request.SourceKind;
    }

    private bool TryConsumePending(HitResult result)
    {
        if (pendingConsumptionMode == PendingConsumptionMode.None ||
            result.Target != pendingTarget ||
            result.SourceKind != pendingSourceKind ||
            !result.IsCritical)
        {
            return false;
        }

        currentBankPoints = Mathf.Max(0, currentBankPoints - pendingPointsToConsume);
        return true;
    }

    private void ClearPendingConsumption()
    {
        pendingConsumptionMode = PendingConsumptionMode.None;
        pendingPointsToConsume = 0;
        pendingTarget = null;
        pendingSourceKind = default;
    }

    private string BuildDescription()
    {
        return $"非暴击有效命中获得 {PointsPerNonCriticalHit} 点蓄能，满 {MaxBankPoints} 点时下一次命中必定暴击；未满时若自然暴击，则按蓄能比例提高暴击伤害。";
    }
}
