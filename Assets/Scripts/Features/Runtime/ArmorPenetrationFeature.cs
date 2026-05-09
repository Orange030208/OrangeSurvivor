using System;
using UnityEngine;

[Serializable]
public sealed class ArmorPenetrationFeature : FeatureEffectBase
{
    [SerializeField] private float armorPenetrationPercent = 30f;

    public ArmorPenetrationFeature()
    {
        hitModifierTiming = HitModifierTiming.Deal;
    }

    public override bool CanModifyHit => true;
    public override int HitPriority => HitModifierPriority.Core - 1;
    public override string Description => $"对敌人护甲穿透 +{armorPenetrationPercent:0.#}%。";

    public override void ModifyHit(HitContext hitContext)
    {
        if (hitContext == null || hitContext.IsCancelled)
        {
            return;
        }

        hitContext.ArmorPenetrationPercent += armorPenetrationPercent;
    }
}
