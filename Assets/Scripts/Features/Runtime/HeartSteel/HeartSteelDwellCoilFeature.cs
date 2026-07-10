using System;
using UnityEngine;

[Serializable]
public sealed class HeartSteelDwellCoilFeature : FeatureBase, IHeartSteelDwellTuningProvider
{
    [SerializeField] private string targetWeaponId = "Weapon_NeonShield";
    [SerializeField, Range(0.01f, 1f)] private float requiredDwellSecondsMultiplier = 0.75f;
    [SerializeField, Min(0f)] private float extraLingerSeconds = 0.75f;
    [SerializeField, Min(0f)] private float minRequiredDwellSeconds = 0.5f;
    [SerializeField, Min(0f)] private float maxLingerSeconds = 3f;

    public override string Title => "蓄势线圈";
    public override string Description => BuildDescription();

    private float RequiredDwellSecondsMultiplier => Mathf.Clamp(requiredDwellSecondsMultiplier, 0.01f, 1f);
    private float ExtraLingerSeconds => Mathf.Max(0f, extraLingerSeconds);
    private float MinRequiredDwellSeconds => Mathf.Max(0f, minRequiredDwellSeconds);
    private float MaxLingerSeconds => Mathf.Max(0f, maxLingerSeconds);

    public bool AppliesTo(string weaponId)
    {
        return string.IsNullOrWhiteSpace(targetWeaponId) ||
               string.Equals(targetWeaponId, weaponId, StringComparison.Ordinal);
    }

    public HeartSteelDwellSettings Apply(HeartSteelDwellSettings settings)
    {
        float resolvedRequiredDwellSeconds = Mathf.Max(
            MinRequiredDwellSeconds,
            settings.RequiredDwellSeconds * RequiredDwellSecondsMultiplier);
        float resolvedLingerSeconds = settings.LingerSeconds + ExtraLingerSeconds;
        if (MaxLingerSeconds > 0f)
        {
            resolvedLingerSeconds = Mathf.Min(MaxLingerSeconds, resolvedLingerSeconds);
        }

        return new HeartSteelDwellSettings(
            resolvedRequiredDwellSeconds,
            resolvedLingerSeconds);
    }

    private string BuildDescription()
    {
        string weaponText = string.IsNullOrWhiteSpace(targetWeaponId) ? "心之钢武器" : targetWeaponId;
        float reductionPercent = (1f - RequiredDwellSecondsMultiplier) * 100f;
        return $"{weaponText} 的心之钢蓄势时间缩短 {reductionPercent:0.##}%，蓄势标记额外保留 {ExtraLingerSeconds:0.##} 秒。";
    }
}
