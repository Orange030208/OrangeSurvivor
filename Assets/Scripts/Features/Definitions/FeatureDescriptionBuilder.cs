using System.Collections.Generic;

public static class FeatureDescriptionBuilder
{
    public static List<string> BuildPropDescriptions(IReadOnlyList<PropEntry> propEntries)
    {
        List<string> descriptions = new(propEntries?.Count ?? 0);
        AddPropDescriptions(descriptions, propEntries);
        return descriptions;
    }

    public static void AddPropDescriptions(List<string> descriptions, IReadOnlyList<PropEntry> propEntries)
    {
        if (descriptions == null || propEntries == null)
        {
            return;
        }

        for (int i = 0; i < propEntries.Count; i++)
        {
            descriptions.Add(propEntries[i].GetAutoDescription());
        }
    }

    public static void AddFeatureDescriptions(List<string> descriptions, IReadOnlyList<FeatureEffectBase> featureEffects)
    {
        if (descriptions == null || featureEffects == null)
        {
            return;
        }

        for (int i = 0; i < featureEffects.Count; i++)
        {
            FeatureEffectBase effect = featureEffects[i];
            if (effect == null || string.IsNullOrEmpty(effect.FeatureDescription))
            {
                continue;
            }

            descriptions.Add(effect.FeatureDescription);
        }
    }

    public static string BuildAccessoryOwnedDescription(AccessoryDataSO accessory)
    {
        string coloredName = ColorHelper.WrapRichTextColor(accessory.ItemName, ColorHelper.GetColorByRarity(accessory.Rarity));
        return $"初始拥有 {coloredName}";
    }

    public static string BuildInitialWeaponDescription(WeaponDataSO weaponData, int level)
    {
        string coloredWeaponName = ColorHelper.WrapRichTextColor(weaponData.ItemName, ColorHelper.GetColorByLevel(level));
        return $"初始携带1个 {coloredWeaponName}";
    }
}
