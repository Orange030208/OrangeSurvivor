using System;

/// <summary>
/// 商店商品的稳定身份。不同商品类型可以用自己的 VariantId 表达差异。
/// </summary>
public readonly struct ShopProductKey : IEquatable<ShopProductKey>
{
    public ShopProductKey(string productType, string contentId, string variantId = null)
    {
        ProductType = productType ?? string.Empty;
        ContentId = contentId ?? string.Empty;
        VariantId = variantId ?? string.Empty;
    }

    public string ProductType { get; }
    public string ContentId { get; }
    public string VariantId { get; }
    public string StableId => string.IsNullOrWhiteSpace(VariantId)
        ? $"{ProductType}:{ContentId}"
        : $"{ProductType}:{ContentId}:{VariantId}";

    public static ShopProductKey CreateWeapon(WeaponDataSO weaponData, int weaponLevel)
    {
        string contentId = weaponData != null && !string.IsNullOrWhiteSpace(weaponData.WeaponId)
            ? weaponData.WeaponId
            : weaponData != null ? weaponData.name : string.Empty;
        return new ShopProductKey("Weapon", contentId, $"Lv{WeaponLevelHelper.ClampLevel(weaponLevel)}");
    }

    public static ShopProductKey CreateAccessory(AccessoryDataSO accessoryData)
    {
        string contentId = accessoryData != null && !string.IsNullOrWhiteSpace(accessoryData.AccessoryId)
            ? accessoryData.AccessoryId
            : accessoryData != null ? accessoryData.name : string.Empty;
        return new ShopProductKey("Accessory", contentId);
    }

    public bool Equals(ShopProductKey other)
    {
        return string.Equals(ProductType, other.ProductType, StringComparison.Ordinal) &&
               string.Equals(ContentId, other.ContentId, StringComparison.Ordinal) &&
               string.Equals(VariantId, other.VariantId, StringComparison.Ordinal);
    }

    public override bool Equals(object obj)
    {
        return obj is ShopProductKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = ProductType != null ? ProductType.GetHashCode() : 0;
            hashCode = (hashCode * 397) ^ (ContentId != null ? ContentId.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (VariantId != null ? VariantId.GetHashCode() : 0);
            return hashCode;
        }
    }
}
