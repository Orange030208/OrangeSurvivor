using System.Collections.Generic;

public static class FeatureDescriptionBuilder
{
    private static readonly FeatureDisplayBuilder featureDisplayBuilder = new();

    public static List<string> BuildPropDescriptions(IReadOnlyList<PropEntry> propEntries)
    {
        TextListBlock block = featureDisplayBuilder.Build(propEntries, null).GetBlock<TextListBlock>();
        return ToLegacyDescriptions(block);
    }

    public static void AddPropDescriptions(List<string> descriptions, IReadOnlyList<PropEntry> propEntries)
    {
        AddBlockLines(descriptions, featureDisplayBuilder.Build(propEntries, null).GetBlock<TextListBlock>());
    }

    public static void AddFeatureDescriptions(List<string> descriptions, IReadOnlyList<FeatureEffectBase> featureEffects)
    {
        AddBlockLines(descriptions, featureDisplayBuilder.Build(null, featureEffects).GetBlock<TextListBlock>());
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

    private static List<string> ToLegacyDescriptions(TextListBlock block)
    {
        List<string> descriptions = new(block != null && block.Items != null ? block.Items.Count : 0);
        AddBlockLines(descriptions, block);
        return descriptions;
    }

    private static void AddBlockLines(List<string> descriptions, TextListBlock block)
    {
        if (descriptions == null || block?.Items == null)
        {
            return;
        }

        for (int i = 0; i < block.Items.Count; i++)
        {
            TextLineItem item = block.Items[i];
            if (item == null || string.IsNullOrWhiteSpace(item.Text))
            {
                continue;
            }

            descriptions.Add(item.Text);
        }
    }
}
