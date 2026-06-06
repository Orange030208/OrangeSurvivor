using System;

public sealed class ItemInfoViewDataBuilder
{
    private readonly WeaponInfoBuilder weaponInfoBuilder = new();
    private readonly AccessoryInfoBuilder accessoryInfoBuilder = new();
    private readonly InfoDocumentService infoDocumentService = new();

    public ItemInfoViewData Build(Weapon runtimeWeapon)
    {
        InfoDocument document = weaponInfoBuilder.Build(WeaponInfoSource.FromRuntime(runtimeWeapon));
        return BuildFromDocument(document, ResolveItemTypeText(ItemType.Weapon));
    }

    public ItemInfoViewData Build(WeaponDataSO weaponData, int level)
    {
        InfoDocument document = weaponInfoBuilder.Build(WeaponInfoSource.FromData(weaponData, level));
        return BuildFromDocument(document, ResolveItemTypeText(ItemType.Weapon));
    }

    public ItemInfoViewData Build(AccessoryDataSO accessoryData)
    {
        InfoDocument document = accessoryInfoBuilder.Build(accessoryData);
        return BuildFromDocument(document, ResolveItemTypeText(ItemType.Accessory));
    }

    public ItemInfoViewData Build(ItemDataSO itemData)
    {
        if (itemData == null)
        {
            return default;
        }

        if (itemData is WeaponDataSO weaponData)
        {
            return Build(weaponData, WeaponLevelHelper.MinLevel);
        }

        if (itemData is AccessoryDataSO accessoryData)
        {
            return Build(accessoryData);
        }

        if (itemData is IInfoDocumentSource infoDocumentSource)
        {
            return BuildFromDocument(infoDocumentSource.BuildInfoDocument(), ResolveItemTypeText(itemData.ItemType));
        }

        if (infoDocumentService.TryBuild(itemData, out InfoDocument document))
        {
            return BuildFromDocument(document, ResolveItemTypeText(itemData.ItemType));
        }

        return new ItemInfoViewData(
            itemData.ItemName,
            ResolveItemTypeText(itemData.ItemType),
            string.Empty,
            itemData.ManualDescription);
    }

    private static ItemInfoViewData BuildFromDocument(InfoDocument document, string fallbackTypeText)
    {
        if (document == null)
        {
            return new ItemInfoViewData(
                string.Empty,
                fallbackTypeText,
                string.Empty,
                string.Empty);
        }

        return new ItemInfoViewData(
            document.Title,
            ResolveTypeText(document, fallbackTypeText),
            ResolveTagText(document),
            InfoDocumentTextFormatter.ToRichText(document, includeHeader: false));
    }

    private static string ResolveTagText(InfoDocument document)
    {
        return document != null && document.Tags != null && document.Tags.Count > 0
            ? string.Join(" / ", document.Tags)
            : string.Empty;
    }

    private static string ResolveTypeText(InfoDocument document, string fallbackTypeText)
    {
        if (document == null)
        {
            return fallbackTypeText ?? string.Empty;
        }

        return document.Kind switch
        {
            InfoDocumentKind.Weapon => ResolveItemTypeText(ItemType.Weapon),
            InfoDocumentKind.Accessory => ResolveItemTypeText(ItemType.Accessory),
            _ => fallbackTypeText ?? string.Empty
        };
    }

    private static string ResolveItemTypeText(ItemType itemType)
    {
        return itemType switch
        {
            ItemType.Weapon => "武器",
            ItemType.Accessory => "饰品",
            _ => string.Empty
        };
    }
}
