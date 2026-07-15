using System;
using Orange.GameServices;
using UnityEngine;

[Serializable]
public sealed class RerollGoldRewardFeature : FeatureBase
{
    [SerializeField, Min(0)] private int paidRerollBaseGold = 5;
    private ShopManager shopManager;

    public override string Title => "刷新金币回响";
    public override string Description =>
        $"每次付费刷新获得 {Mathf.Max(0, paidRerollBaseGold)} + 当前波次金币；免费刷新只获得当前波次金币。";

    public override void OnInstall()
    {
        if (!GameServices.TryGet(out shopManager))
        {
            Debug.LogWarning($"[{nameof(RerollGoldRewardFeature)}] {nameof(ShopManager)} is unavailable.");
            return;
        }

        shopManager.RerollCompleted += OnRerollCompleted;
    }

    public override void OnUninstall()
    {
        if (shopManager != null)
        {
            shopManager.RerollCompleted -= OnRerollCompleted;
            shopManager = null;
        }
    }

    private void OnRerollCompleted()
    {
        if (!IsShopOwner())
        {
            return;
        }

        CurrencyWallet wallet = Context.CurrencyWallet;
        if (wallet == null)
        {
            return;
        }

        int waveNumber = Mathf.Max(1, RunProgressionRuntime.CurrentSnapshot.WaveNumber);
        int rewardGold = shopManager.Board.IsCurrentRerollFree
            ? waveNumber
            : Mathf.Max(0, paidRerollBaseGold) + waveNumber;

        wallet.ChangeAmount(rewardGold);
    }

    private bool IsShopOwner()
    {
        return Context?.OwnerEntity is Player player && shopManager.CurrentPlayer == player;
    }
}
