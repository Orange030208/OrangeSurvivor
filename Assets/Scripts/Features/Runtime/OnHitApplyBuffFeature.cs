using System;
using UnityEngine;

public enum FeatureBuffApplyTarget
{
    Target = 0,
    Source = 1
}

[Serializable]
public sealed class OnHitApplyBuffFeature : FeatureBase
{
    [SerializeField] private BuffDataSO buffData;
    [SerializeField, Range(0f, 100f)] private float applyChancePercent = 100f;
    [SerializeField] private FeatureBuffApplyTarget applyTo = FeatureBuffApplyTarget.Target;
    [SerializeField] private bool overrideDuration;
    [SerializeField] private BuffDurationPolicy durationPolicy = BuffDurationPolicy.Timed;
    [SerializeField, Min(0f)] private float durationSeconds = 5f;
    [SerializeField] private HitSourceKind[] allowedSourceKinds =
    {
        HitSourceKind.Weapon,
        HitSourceKind.Projectile
    };

    public override string Title => "命中附加 Buff";
    public override string Description => BuildDescription();

    public override void OnInstall()
    {
        YokiFrame.EventKit.Type.Register<EntityDamagedEvent>(OnEntityDamaged);
    }

    public override void OnUninstall()
    {
        YokiFrame.EventKit.Type.UnRegister<EntityDamagedEvent>(OnEntityDamaged);
    }

    private void OnEntityDamaged(EntityDamagedEvent eventData)
    {
        HitResult result = eventData.HitResult;
        if (buffData == null ||
            result.Source != Context?.OwnerEntity ||
            result.FinalDamage <= 0f ||
            !FeatureRuntimeUtility.AllowsSourceKind(result.SourceKind, allowedSourceKinds))
        {
            return;
        }

        float chance = Mathf.Clamp01(applyChancePercent * PropValueUtility.PERCENT_POINT_TO_RATIO);
        if (chance <= 0f || UnityEngine.Random.value > chance)
        {
            return;
        }

        Entity buffTarget = applyTo == FeatureBuffApplyTarget.Source
            ? result.Source
            : result.Target;
        FeatureRuntimeUtility.ApplyBuff(buffTarget, buffData, overrideDuration, durationPolicy, durationSeconds);
    }

    private string BuildDescription()
    {
        if (buffData == null)
        {
            return "命中后未配置要施加的 Buff。";
        }

        string targetText = applyTo == FeatureBuffApplyTarget.Source ? "自身" : "目标";
        return $"造成有效命中后，有 {Mathf.Clamp(applyChancePercent, 0f, 100f):0.##}% 概率对{targetText}施加 {buffData.DisplayName}。";
    }
}
