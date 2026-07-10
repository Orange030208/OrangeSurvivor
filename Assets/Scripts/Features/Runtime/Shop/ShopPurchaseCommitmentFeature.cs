using System;
using UnityEngine;

[Serializable]
public sealed class ShopPurchaseCommitmentFeature : FeatureBase
{
    [SerializeField, Min(1)] private int requiredPurchaseCount = 3;
    [SerializeField, Range(0, 100)] private int rebatePercent = 50;
    [SerializeField, Min(0)] private int penaltyBaseGold = 20;
    [SerializeField, Min(0)] private int penaltyGoldPerWave = 2;
    [SerializeField, Min(1)] private int fallbackWaveNumber = 1;

    private int currentWaveNumber = 1;
    private int purchaseCountThisShop;
    private int spentGoldThisShop;

    public override string Title => "商店购买承诺";
    public override string Description => BuildDescription();

    public override void OnInstall()
    {
        currentWaveNumber = Mathf.Max(1, fallbackWaveNumber);
        YokiFrame.EventKit.Type.Register<ShopItemPurchasedEvent>(OnShopItemPurchased);
        YokiFrame.EventKit.Type.Register<GameStateChangedEvent>(OnGameStateChanged);
        YokiFrame.EventKit.Type.Register<WaveStartedEvent>(OnWaveStarted);
        YokiFrame.EventKit.Type.Register<WaveCompletedEvent>(OnWaveCompleted);
        YokiFrame.EventKit.Type.Register<WaveRuntimeChangedEvent>(OnWaveRuntimeChanged);
    }

    public override void OnUninstall()
    {
        YokiFrame.EventKit.Type.UnRegister<ShopItemPurchasedEvent>(OnShopItemPurchased);
        YokiFrame.EventKit.Type.UnRegister<GameStateChangedEvent>(OnGameStateChanged);
        YokiFrame.EventKit.Type.UnRegister<WaveStartedEvent>(OnWaveStarted);
        YokiFrame.EventKit.Type.UnRegister<WaveCompletedEvent>(OnWaveCompleted);
        YokiFrame.EventKit.Type.UnRegister<WaveRuntimeChangedEvent>(OnWaveRuntimeChanged);
        ResetShopTracking();
    }

    private void OnShopItemPurchased(ShopItemPurchasedEvent eventData)
    {
        if (Context?.OwnerEntity is not Player player || eventData.Player != player || eventData.Price <= 0)
        {
            return;
        }

        purchaseCountThisShop++;
        spentGoldThisShop += eventData.Price;
    }

    private void OnGameStateChanged(GameStateChangedEvent eventData)
    {
        if (eventData.NewState == GameState.Shop)
        {
            ResetShopTracking();
            return;
        }

        if (eventData.OldState == GameState.Shop && eventData.NewState == GameState.Game)
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

        int penaltyGold = penaltyBaseGold + Mathf.Max(1, currentWaveNumber) * penaltyGoldPerWave;
        wallet.ChangeAmount(-Mathf.Min(wallet.CurrentAmount, penaltyGold));
    }

    private void OnWaveStarted(WaveStartedEvent eventData)
    {
        SetCurrentWave(eventData.CurrentWave);
    }

    private void OnWaveCompleted(WaveCompletedEvent eventData)
    {
        SetCurrentWave(eventData.WaveNumber);
    }

    private void OnWaveRuntimeChanged(WaveRuntimeChangedEvent eventData)
    {
        SetCurrentWave(eventData.CurrentWave);
    }

    private void SetCurrentWave(int waveNumber)
    {
        if (waveNumber > 0)
        {
            currentWaveNumber = waveNumber;
        }
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
