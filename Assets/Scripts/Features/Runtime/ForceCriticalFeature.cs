using UnityEngine;

[System.Serializable]
public sealed class ForceCriticalFeature : HitModifierFeatureBase
{
    public override int HitPriority => HitModifierPriority.Override;
    public override string Description => "该实体造成的命中强制视为暴击。";

    public override void ModifyHit(HitContext hitContext)
    {
        hitContext.IsCritical = true;
    }
}
