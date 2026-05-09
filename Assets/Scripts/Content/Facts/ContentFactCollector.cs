using System.Collections.Generic;
using UnityEngine;

public static class ContentFactCollector
{
    public static ContentFactSet Collect(ContentFactSource source, IEnumerable<FactDefinitionSO> definitions)
    {
        ContentFactSet factSet = new();
        source ??= new ContentFactSource();
        // 先写入内建事实 ID，避免 Modifier 只能依赖池条目显式收集到的 FactDefinitionSO。
        AddBuiltInFactIds(source, factSet);
        if (definitions == null)
        {
            return factSet;
        }

        HashSet<FactDefinitionSO> visited = new();
        foreach (FactDefinitionSO definition in definitions)
        {
            if (definition == null || !visited.Add(definition))
            {
                continue;
            }

            if (TryResolveBuiltInFact(source, definition, out ContentFactValue value))
            {
                factSet.Set(definition, value);
            }
        }

        return factSet;
    }

    private static bool TryResolveBuiltInFact(
        ContentFactSource source,
        FactDefinitionSO definition,
        out ContentFactValue value)
    {
        value = default;

        switch (definition.BuiltInKind)
        {
            case FactDefinitionBuiltInKind.CurrentWave:
                value = ContentFactValue.FromInt(Mathf.Max(1, source.WaveNumber));
                return true;
            case FactDefinitionBuiltInKind.Luck:
                value = ContentFactValue.FromFloat(GetPropertyValue(source, PropType.Luck));
                return true;
            case FactDefinitionBuiltInKind.ShopRefreshCount:
                value = ContentFactValue.FromInt(Mathf.Max(0, source.ShopRefreshCount));
                return true;
            case FactDefinitionBuiltInKind.ShopRerollCount:
                value = ContentFactValue.FromInt(Mathf.Max(0, source.ShopRerollCount));
                return true;
            case FactDefinitionBuiltInKind.Character:
                value = ContentFactValue.FromObject(source.CharacterData);
                return source.CharacterData != null;
            case FactDefinitionBuiltInKind.PlayerProperty:
                value = ContentFactValue.FromFloat(GetPropertyValue(source, definition.PropType));
                return true;
            case FactDefinitionBuiltInKind.UpgradeCardTagPickCount:
                value = ContentFactValue.FromInt(source.UpgradeRunState != null
                    ? source.UpgradeRunState.GetTagPickCount(definition.UpgradeCardTag)
                    : 0);
                return true;
            case FactDefinitionBuiltInKind.OwnedWeaponTagCount:
                value = ContentFactValue.FromInt(GetOwnedWeaponTagCount(source, definition.WeaponTag));
                return true;
            case FactDefinitionBuiltInKind.OwnedWeaponCount:
                value = ContentFactValue.FromInt(GetOwnedWeaponCount(source));
                return true;
            case FactDefinitionBuiltInKind.OwnedWeapon:
                value = ContentFactValue.FromBool(HasOwnedWeapon(source, definition.WeaponData));
                return definition.WeaponData != null;
            case FactDefinitionBuiltInKind.WaveId:
                value = ContentFactValue.FromString(source.WaveId);
                return !string.IsNullOrWhiteSpace(source.WaveId);
            case FactDefinitionBuiltInKind.WaveTrackId:
                value = ContentFactValue.FromString(source.WaveTrackId);
                return !string.IsNullOrWhiteSpace(source.WaveTrackId);
            case FactDefinitionBuiltInKind.WaveProgressPercent:
                value = ContentFactValue.FromFloat(Mathf.Clamp(source.WaveProgressPercent, 0f, 100f));
                return true;
            default:
                return false;
        }
    }

    private static void AddBuiltInFactIds(ContentFactSource source, ContentFactSet factSet)
    {
        // 这些事实是所有内容池共享的基础上下文，按稳定 ID 注入，方便跨池 Modifier 统一读取。
        factSet.Set(ContentFactIds.CurrentWave, ContentFactValue.FromInt(Mathf.Max(1, source.WaveNumber)));
        factSet.Set(ContentFactIds.Luck, ContentFactValue.FromFloat(GetPropertyValue(source, PropType.Luck)));
        factSet.Set(ContentFactIds.ShopRefreshCount, ContentFactValue.FromInt(Mathf.Max(0, source.ShopRefreshCount)));
        factSet.Set(ContentFactIds.ShopRerollCount, ContentFactValue.FromInt(Mathf.Max(0, source.ShopRerollCount)));
        factSet.Set(ContentFactIds.OwnedWeaponCount, ContentFactValue.FromInt(GetOwnedWeaponCount(source)));
        factSet.Set(ContentFactIds.WaveProgressPercent, ContentFactValue.FromFloat(Mathf.Clamp(source.WaveProgressPercent, 0f, 100f)));

        if (source.CharacterData != null)
        {
            factSet.Set(ContentFactIds.Character, ContentFactValue.FromObject(source.CharacterData));
        }

        if (!string.IsNullOrWhiteSpace(source.WaveId))
        {
            factSet.Set(ContentFactIds.WaveId, ContentFactValue.FromString(source.WaveId));
        }

        if (!string.IsNullOrWhiteSpace(source.WaveTrackId))
        {
            factSet.Set(ContentFactIds.WaveTrackId, ContentFactValue.FromString(source.WaveTrackId));
        }
    }

    private static float GetPropertyValue(ContentFactSource source, PropType propType)
    {
        PropertiesManager propertiesManager = source.PropertiesManager;
        if (propertiesManager == null && source.Player != null)
        {
            propertiesManager = source.Player.GetComponent<PropertiesManager>();
        }

        return propertiesManager != null ? propertiesManager.GetPropValue(propType) : 0f;
    }

    private static int GetOwnedWeaponCount(ContentFactSource source)
    {
        if (source.OwnedWeapons != null)
        {
            return source.OwnedWeapons.Count;
        }

        WeaponsHolder weaponsHolder = source.WeaponsHolder;
        if (weaponsHolder == null && source.Player != null)
        {
            weaponsHolder = source.Player.GetComponent<WeaponsHolder>();
        }

        return weaponsHolder != null ? weaponsHolder.EquippedWeapons.Count : 0;
    }

    private static int GetOwnedWeaponTagCount(ContentFactSource source, WeaponTag tag)
    {
        int count = 0;
        if (source.OwnedWeapons != null)
        {
            for (int i = 0; i < source.OwnedWeapons.Count; i++)
            {
                if (source.OwnedWeapons[i] != null && source.OwnedWeapons[i].HasTag(tag))
                {
                    count++;
                }
            }

            return count;
        }

        WeaponsHolder weaponsHolder = source.WeaponsHolder;
        if (weaponsHolder == null && source.Player != null)
        {
            weaponsHolder = source.Player.GetComponent<WeaponsHolder>();
        }

        if (weaponsHolder == null)
        {
            return 0;
        }

        IReadOnlyList<EquippedWeaponInfo> equippedWeapons = weaponsHolder.EquippedWeapons;
        for (int i = 0; i < equippedWeapons.Count; i++)
        {
            WeaponDataSO weaponData = equippedWeapons[i].WeaponData;
            if (weaponData != null && weaponData.HasTag(tag))
            {
                count++;
            }
        }

        return count;
    }

    private static bool HasOwnedWeapon(ContentFactSource source, WeaponDataSO targetWeapon)
    {
        if (targetWeapon == null)
        {
            return false;
        }

        if (source.OwnedWeapons != null)
        {
            for (int i = 0; i < source.OwnedWeapons.Count; i++)
            {
                if (IsSameWeapon(source.OwnedWeapons[i], targetWeapon))
                {
                    return true;
                }
            }

            return false;
        }

        WeaponsHolder weaponsHolder = source.WeaponsHolder;
        if (weaponsHolder == null && source.Player != null)
        {
            weaponsHolder = source.Player.GetComponent<WeaponsHolder>();
        }

        if (weaponsHolder == null)
        {
            return false;
        }

        IReadOnlyList<EquippedWeaponInfo> equippedWeapons = weaponsHolder.EquippedWeapons;
        for (int i = 0; i < equippedWeapons.Count; i++)
        {
            if (IsSameWeapon(equippedWeapons[i].WeaponData, targetWeapon))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsSameWeapon(WeaponDataSO left, WeaponDataSO right)
    {
        if (left == null || right == null)
        {
            return false;
        }

        if (left == right)
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(left.ItemName) &&
               string.Equals(left.ItemName, right.ItemName, System.StringComparison.Ordinal);
    }
}
