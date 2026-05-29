using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "Upgrade Card Rarity Presentation Catalog",
    menuName = ScriptableObjectMenuPaths.UPGRADE_CARD_RARITY_PRESENTATION_CATALOG,
    order = 0)]
public sealed class UpgradeCardRarityPresentationCatalogSO : CardQualityPresentationCatalogSO
{
    public static CardQualityPresentationProfile CreateBuiltinProfile(UpgradeCardRarity rarity)
    {
        return CardQualityPresentationCatalogSO.CreateBuiltinProfile(ContentTierResolver.FromUpgradeCardRarity(rarity).ToCardQuality());
    }
}
