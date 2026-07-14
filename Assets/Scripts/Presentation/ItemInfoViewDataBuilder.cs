public sealed class ItemInfoViewDataBuilder
{
    private readonly WeaponInfoBuilder weaponInfoBuilder = new();
    private readonly AccessoryInfoBuilder accessoryInfoBuilder = new();
    private readonly InfoDocumentService infoDocumentService = new();

    public ItemInfoViewData Build(Weapon runtimeWeapon)
    {
        InfoDocument document = weaponInfoBuilder.Build(WeaponInfoSource.FromRuntime(runtimeWeapon));
        string displayName = runtimeWeapon != null && runtimeWeapon.WeaponData != null
            ? ItemNameStyleUtility.GetWeaponDisplayName(runtimeWeapon.WeaponData.ItemName, runtimeWeapon.Tier)
            : string.Empty;
        return BuildFromDocument(document, string.Empty, displayName);
    }

    public ItemInfoViewData Build(WeaponDataSO weaponData, int level)
    {
        InfoDocument document = weaponInfoBuilder.Build(WeaponInfoSource.FromData(weaponData, level));
        string displayName = weaponData != null
            ? ItemNameStyleUtility.GetWeaponDisplayName(weaponData.ItemName, level)
            : string.Empty;
        return BuildFromDocument(document, string.Empty, displayName);
    }

    public ItemInfoViewData Build(AccessoryDataSO accessoryData)
    {
        InfoDocument document = accessoryInfoBuilder.Build(accessoryData);
        string displayName = accessoryData != null
            ? ItemNameStyleUtility.GetAccessoryDisplayName(accessoryData.ItemName, accessoryData.Tier)
            : string.Empty;
        return BuildFromDocument(document, string.Empty, displayName);
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

    public ItemInfoViewData Build(InfoDocument document, string fallbackTypeText, string overrideName = null)
    {
        return BuildFromDocument(document, fallbackTypeText, overrideName);
    }

    private static ItemInfoViewData BuildFromDocument(
        InfoDocument document,
        string fallbackTypeText,
        string overrideName = null)
    {
        if (document == null)
        {
            return new ItemInfoViewData(
                overrideName ?? string.Empty,
                fallbackTypeText,
                string.Empty,
                string.Empty);
        }

        return new ItemInfoViewData(
            string.IsNullOrWhiteSpace(overrideName) ? ResolveTitle(document) : overrideName,
            fallbackTypeText ?? string.Empty,
            ResolveTagText(document),
            InfoDocumentTextFormatter.ToRichText(document, includeHeader: false));
    }

    private static string ResolveTagText(InfoDocument document)
    {
        if (document?.Items == null)
        {
            return string.Empty;
        }

        for (int i = 0; i < document.Items.Count; i++)
        {
            InfoItem item = document.Items[i];
            if (item.Type == InfoItemType.TagText && !string.IsNullOrWhiteSpace(item.Content))
            {
                return item.Decoder.DecodeText(item.Content);
            }
        }

        return string.Empty;
    }

    private static string ResolveTitle(InfoDocument document)
    {
        if (document == null)
        {
            return string.Empty;
        }

        if (document.Items == null)
        {
            return string.Empty;
        }

        for (int i = 0; i < document.Items.Count; i++)
        {
            InfoItem item = document.Items[i];
            if (item.Type == InfoItemType.Title && !string.IsNullOrWhiteSpace(item.Content))
            {
                return item.Decoder.DecodeText(item.Content);
            }
        }

        return string.Empty;
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
