using System;

public sealed class ShopExtractionCandidate : IHasContentTier
{
    private ShopExtractionCandidate(IShopProduct product)
    {
        Product = product ?? throw new ArgumentNullException(nameof(product));
        EntryId = product.Key.StableId;
        Tier = ContentTierResolver.FromQualityValue((int)product.Tier);
    }

    public string EntryId { get; }
    public IShopProduct Product { get; }
    public ContentTier Tier { get; }

    public static ShopExtractionCandidate CreateAccessory(AccessoryDataSO accessory)
    {
        if (accessory == null)
        {
            return null;
        }

        return new ShopExtractionCandidate(new AccessoryShopProduct(accessory));
    }

    public static ShopExtractionCandidate CreateWeapon(WeaponDataSO weapon, int level)
    {
        if (weapon == null)
        {
            return null;
        }

        return new ShopExtractionCandidate(new WeaponShopProduct(weapon, level));
    }
}
