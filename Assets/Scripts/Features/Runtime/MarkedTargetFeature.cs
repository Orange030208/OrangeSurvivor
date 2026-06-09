using System;
using UnityEngine;

[Serializable]
public sealed class MarkedTargetFeature : HitModifierFeatureBase
{
    [SerializeField, Min(0f)] private float damageTakenBonusPercent = 20f;

    public MarkedTargetFeature()
    {
        hitModifierTiming = HitModifierTiming.Receive;
    }

    public override int HitPriority => HitModifierPriority.Parameter;
    public override string Title => "标记易伤";
    public override string Description => $"被标记者受到的有效命中伤害 +{DamageTakenBonusPercent:0.##}%。";

    private float DamageTakenBonusPercent => Mathf.Max(0f, damageTakenBonusPercent);

    public override void ModifyHit(HitContext hitContext)
    {
        if (hitContext == null || hitContext.IsCancelled || hitContext.IsDodged || hitContext.IsBlocked)
        {
            return;
        }

        hitContext.Damage *= 1f + PropValueUtility.PercentPointsToRatio(DamageTakenBonusPercent);
    }
}
