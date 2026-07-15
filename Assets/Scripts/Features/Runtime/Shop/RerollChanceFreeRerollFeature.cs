using System;
using Orange.GameServices;
using UnityEngine;

[Serializable]
public sealed class RerollChanceFreeRerollFeature : FeatureBase
{
    [SerializeField, Range(0, 100)] private int chancePercent = 40;
    [SerializeField, Min(1)] private int freeRerollCount = 1;

    private ShopManager shopManager;

    public override string Title => "刷新返券";
    public override string Description =>
        $"每次刷新商店时，有 {Mathf.Clamp(chancePercent, 0, 100)}% 概率获得 {Mathf.Max(1, freeRerollCount)} 次免费刷新。";

    public override void OnInstall()
    {
        if (!GameServices.TryGet(out shopManager))
        {
            Debug.LogWarning($"[{nameof(RerollChanceFreeRerollFeature)}] {nameof(ShopManager)} is unavailable.");
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
        if (!IsShopOwner() ||
            freeRerollCount <= 0 ||
            UnityEngine.Random.value > Mathf.Clamp01(chancePercent / 100f))
        {
            return;
        }

        shopManager.Board.GrantVisitFreeRerolls(freeRerollCount);
    }

    private bool IsShopOwner()
    {
        return Context?.OwnerEntity is Player player && shopManager.CurrentPlayer == player;
    }
}
