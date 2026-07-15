using System;
using Orange.GameServices;
using UnityEngine;

[Serializable]
public sealed class ShopVisitFreeRerollFeature : FeatureBase
{
    [SerializeField, Min(1)] private int freeRerollCount = 1;
    private ShopManager shopManager;

    public override string Title => "商店免费刷新";
    public override string Description => $"每次进入商店时，获得 {Mathf.Max(1, freeRerollCount)} 次免费刷新。";

    public override void OnInstall()
    {
        if (!GameServices.TryGet(out shopManager))
        {
            Debug.LogWarning($"[{nameof(ShopVisitFreeRerollFeature)}] {nameof(ShopManager)} is unavailable.");
            return;
        }

        shopManager.VisitOpened += OnVisitOpened;
    }

    public override void OnUninstall()
    {
        if (shopManager != null)
        {
            shopManager.VisitOpened -= OnVisitOpened;
            shopManager = null;
        }
    }

    private void OnVisitOpened()
    {
        if (freeRerollCount <= 0 || Context?.OwnerEntity is not Player player || shopManager.CurrentPlayer != player)
        {
            return;
        }

        shopManager.Board.GrantVisitFreeRerolls(freeRerollCount);
    }
}
