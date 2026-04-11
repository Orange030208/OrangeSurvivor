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

    public static void AddSpecialFeatureDescriptions(List<string> descriptions, IReadOnlyList<IFeatureDefinition> featureDefinitions)
    {
        if (descriptions == null || featureDefinitions == null)
        {
            return;
        }

        for (int i = 0; i < featureDefinitions.Count; i++)
        {
            IFeatureDefinition definition = featureDefinitions[i];
            if (definition == null)
            {
                continue;
            }

            descriptions.Add(definition.FeatureDescription);
        }
    }

    public static void AddPropFeatureViews(List<FeatureViewData> features, IReadOnlyList<PropEntry> propEntries)
    {
        if (features == null || propEntries == null)
        {
            return;
        }

        for (int i = 0; i < propEntries.Count; i++)
        {
            PropEntry entry = propEntries[i];
            features.Add(new FeatureViewData(
                entry.GetDisplayName(),
                entry.GetAutoDescription(),
                FeatureCategory.Property,
                entry.value >= 0 ? FeaturePolarity.Positive : FeaturePolarity.Negative));
        }
    }

    public static void AddFeatureEffectViews(List<FeatureViewData> features, IReadOnlyList<FeatureEffectBase> featureEffects)
    {
        if (features == null || featureEffects == null)
        {
            return;
        }

        for (int i = 0; i < featureEffects.Count; i++)
        {
            FeatureEffectBase effect = featureEffects[i];
            if (effect == null)
            {
                continue;
            }

            features.Add(new FeatureViewData(
                effect.FeatureTitle,
                effect.FeatureDescription,
                effect.FeatureCategory,
                effect.FeaturePolarity));
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
