using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 武器序列资源读取器：
/// - 从 WeaponDataSO 读取序列关键帧所引用的投射物、音效、特效配置；
/// - 让 Weapon 基类不再承担边缘序列资源查询职责。
/// 扩展说明：后续如果序列事件改为按字典、ID 或外部表查询，只需替换这里，不要回改 Weapon 子类调用点。
/// </summary>
public sealed class WeaponSequenceResourceResolver
{
    private readonly WeaponDataSO weaponData;

    public WeaponSequenceResourceResolver(WeaponDataSO weaponData)
    {
        this.weaponData = weaponData;
    }

    public bool TryGetProjectile(int eventKey, out ProjectileSpawnPayload payload)
    {
        payload = ProjectileSpawnPayload.Default;
        if (weaponData == null || eventKey < 0)
        {
            return false;
        }

        IReadOnlyList<WeaponSequenceProjectileDefinition> definitions = weaponData.SequenceProjectileList;
        if (definitions == null || eventKey >= definitions.Count)
        {
            return false;
        }

        payload = definitions[eventKey].ToPayload();
        return true;
    }

    public bool TryGetSfx(int eventKey, out WeaponSequenceSfxDefinition definition)
    {
        definition = default;
        if (weaponData == null || eventKey < 0)
        {
            return false;
        }

        IReadOnlyList<WeaponSequenceSfxDefinition> definitions = weaponData.SequenceSfxList;
        if (definitions == null || eventKey >= definitions.Count)
        {
            return false;
        }

        definition = definitions[eventKey];
        return true;
    }

    public bool TryGetVfx(int eventKey, out WeaponSequenceVfxDefinition definition)
    {
        definition = default;
        if (weaponData == null || eventKey < 0)
        {
            return false;
        }

        IReadOnlyList<WeaponSequenceVfxDefinition> definitions = weaponData.SequenceVfxList;
        if (definitions == null || eventKey >= definitions.Count)
        {
            return false;
        }

        definition = definitions[eventKey];
        return true;
    }
}
