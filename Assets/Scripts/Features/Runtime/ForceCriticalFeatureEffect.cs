using UnityEngine;

[System.Serializable]
public sealed class ForceCriticalFeatureEffect : FeatureEffectBase
{
    private FeatureContext installedContext;
    public override bool CanModifyHit => true;
    public override int HitPriority => HitModifierPriority.Override;

    public override string FeatureDescription => "该实体造成的命中强制视为暴击。";

    public override void ModifyHit(HitContext hitContext)
    {
        hitContext.IsCritical = true;
    }
}
