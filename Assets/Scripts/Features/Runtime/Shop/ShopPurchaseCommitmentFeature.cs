using System;
using Orange.GameServices;
using UnityEngine;

[Serializable]
public sealed class ShopPurchaseCommitmentFeature : FeatureBase
{
    [SerializeField, Min(1)] private int requiredPurchaseCount = 3;
    [SerializeField, Range(0, 100)] private int rebatePercent = 50;
    [SerializeField, Min(0)] private int penaltyBaseGold = 20;
    [SerializeField, Min(0)] private int penaltyGoldPerWave = 2;

    private ShopManager shopManager;
    private int purchaseCountThisShop;
    private int spentGoldThisShop;

    public override string Title => "商店购买承诺";
    public override string Description => BuildDescription();

    public override void OnInstall()
    {
        if (!GameServices.TryGet(out shopManager))
        {
            Debug.LogWarning($"[{nameof(ShopPurchaseCommitmentFeature)}] {nameof(ShopManager)} is unavailable.");
            return;
        }

        shopManager.VisitOpened += OnVisitOpened;
        shopManager.PurchaseCompleted += OnPurchaseCompleted;
        shopManager.VisitClosing += OnVisitClosing;
    }

    public override void OnUninstall()
    {
        if (shopManager != null)
        {
            shopManager.VisitOpened -= OnVisitOpened;
            shopManager.PurchaseCompleted -= OnPurchaseCompleted;
            shopManager.VisitClosing -= OnVisitClosing;
            shopManager = null;
        }

        ResetShopTracking();
    }

    private void OnPurchaseCompleted(ShopPurchaseSuccess purchase)
    {
        int price = purchase.Price;
        if (!IsShopOwner() || price <= 0)
        {
            return;
        }

        purchaseCountThisShop++;
        spentGoldThisShop += price;
    }

    private void OnVisitOpened()
    {
        if (IsShopOwner())
        {
            ResetShopTracking();
        }
    }

    private void OnVisitClosing()
    {
        if (IsShopOwner())
        {
            ResolveCommitment();
            ResetShopTracking();
        }
    }

    private void ResolveCommitment()
    {
        CurrencyWallet wallet = Context?.CurrencyWallet;
        if (wallet == null || purchaseCountThisShop <= 0)
        {
            return;
        }

        if (purchaseCountThisShop >= Mathf.Max(1, requiredPurchaseCount))
        {
            int rebateGold = Mathf.RoundToInt(spentGoldThisShop * Mathf.Clamp(rebatePercent, 0, 100) / 100f);
            wallet.ChangeAmount(rebateGold);
            return;
        }

        int penaltyGold = penaltyBaseGold + Mathf.Max(1, RunProgressionRuntime.CurrentSnapshot.WaveNumber) * penaltyGoldPerWave;
        wallet.ChangeAmount(-Mathf.Min(wallet.CurrentAmount, penaltyGold));
    }

    private bool IsShopOwner()
    {
        return Context?.OwnerEntity is Player player && shopManager.CurrentPlayer == player;
    }

    private void ResetShopTracking()
    {
        purchaseCountThisShop = 0;
        spentGoldThisShop = 0;
    }

    private string BuildDescription()
    {
        int safeRequiredPurchaseCount = Mathf.Max(1, requiredPurchaseCount);
        int safeRebatePercent = Mathf.Clamp(rebatePercent, 0, 100);
        int safePenaltyBaseGold = Mathf.Max(0, penaltyBaseGold);
        int safePenaltyGoldPerWave = Mathf.Max(0, penaltyGoldPerWave);
        return $"每波商店结束时，若本波购买至少 {safeRequiredPurchaseCount} 件商品，返还本波消费金币的 {safeRebatePercent}%；" +
               $"若购买了 1-{safeRequiredPurchaseCount - 1} 件商品，则失去 {safePenaltyBaseGold} + 当前波次 * {safePenaltyGoldPerWave} 金币；" +
               "若没有购买商品，则无影响。";
    }
}
