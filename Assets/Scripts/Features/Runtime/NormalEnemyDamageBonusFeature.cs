using System;
using UnityEngine;

[Serializable]
public sealed class NormalEnemyDamageBonusFeature : FeatureEffectBase
{
    [SerializeField] private float damageBonusPercent = 80f;

    public NormalEnemyDamageBonusFeature()
    {
        hitModifierTiming = HitModifierTiming.Deal;
    }

    public override bool CanModifyHit => true;
    public override int HitPriority => HitModifierPriority.Parameter;
    public override string Description => $"对普通敌人伤害 +{damageBonusPercent:0.#}%。";

    public override void ModifyHit(HitContext hitContext)
    {
        if (hitContext == null ||
            hitContext.IsCancelled ||
            hitContext.Request.Target is not Enemy targetEnemy ||
            targetEnemy.Role != EnemyRole.Normal)
        {
            return;
        }

        float damageBonusRatio = PropValueUtility.PercentPointsToRatio(damageBonusPercent);
        hitContext.Damage *= 1f + damageBonusRatio;
    }
}
