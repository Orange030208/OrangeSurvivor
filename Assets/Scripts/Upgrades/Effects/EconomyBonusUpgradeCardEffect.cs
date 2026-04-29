using System;
using UnityEngine;

[Serializable]
public sealed class EconomyBonusUpgradeCardEffect : FeatureEffectBase
{
    [SerializeField] private float shopDiscountPercent;
    [SerializeField] private int freeShopRerolls;
    [SerializeField] private int waveGoldBonus;

    public EconomyBonusUpgradeCardEffect()
    {
    }

    public EconomyBonusUpgradeCardEffect(float shopDiscountPercent, int freeShopRerolls, int waveGoldBonus)
    {
        this.shopDiscountPercent = Mathf.Max(0f, shopDiscountPercent);
        this.freeShopRerolls = Mathf.Max(0, freeShopRerolls);
        this.waveGoldBonus = Mathf.Max(0, waveGoldBonus);
    }

    public override string Description
    {
        get
        {
            string description = string.Empty;
            if (shopDiscountPercent > 0f)
            {
                description += $"商店价格降低 {shopDiscountPercent:P0}。";
            }

            if (freeShopRerolls > 0)
            {
                description += $" 获得 {freeShopRerolls} 次免费刷新。";
            }

            if (waveGoldBonus > 0)
            {
                description += $" 每波结束获得 {waveGoldBonus} 金币。";
            }

            return string.IsNullOrWhiteSpace(description) ? "获得经济收益。" : description.Trim();
        }
    }

    public override void OnInstall()
    {
        if (Context == null)
        {
            return;
        }

        if (shopDiscountPercent > 0f)
        {
            if (Context.PropertiesManager != null)
            {
                Context.PropertiesManager.AddModifier(
                    $"{SourceId}_ShopPriceDiscount",
                    new PropModifierData(PropType.ShopPriceDiscount, shopDiscountPercent));
            }
            else
            {
                Debug.LogWarning("[EconomyBonusUpgradeCardEffect] Player is missing PropertiesManager.");
            }
        }

        if (freeShopRerolls > 0)
        {
            GameEventBus.Publish(new ShopFreeRerollsGrantedEvent(Context.OwnerEntity as Player, freeShopRerolls));
        }

        if (waveGoldBonus > 0)
        {
            if (Context.PropertiesManager != null)
            {
                Context.PropertiesManager.AddModifier(
                    $"{SourceId}_WaveGoldRewardBonus",
                    new PropModifierData(PropType.WaveGoldRewardBonus, waveGoldBonus));
            }
            else
            {
                Debug.LogWarning("[EconomyBonusUpgradeCardEffect] Player is missing PropertiesManager.");
            }
        }
    }
}
