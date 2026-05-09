using System;

[Serializable]
public sealed class InvulnerableFeature : FeatureEffectBase
{
    public InvulnerableFeature()
    {
        hitModifierTiming = HitModifierTiming.Receive;
    }

    public override bool CanModifyHit => true;
    public override int HitPriority => HitModifierPriority.Override;
    public override string Description => "阻挡所有受到的伤害。";

    public override void ModifyHit(HitContext hitContext)
    {
        if (hitContext == null || hitContext.IsCancelled)
        {
            return;
        }

        hitContext.IsBlocked = true;
        hitContext.Damage = 0f;
    }
}
