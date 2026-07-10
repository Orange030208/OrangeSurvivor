using System;
using UnityEngine;

[Serializable]
public sealed class RerollChanceFreeRerollFeature : FeatureBase
{
    [SerializeField, Range(0, 100)] private int chancePercent = 40;
    [SerializeField, Min(1)] private int freeRerollCount = 1;

    public override string Title => "刷新返券";
    public override string Description =>
        $"每次刷新商店时，有 {Mathf.Clamp(chancePercent, 0, 100)}% 概率获得 {Mathf.Max(1, freeRerollCount)} 次免费刷新。";

    public override void OnInstall()
    {
        YokiFrame.EventKit.Type.Register<ShopRerolledEvent>(OnShopRerolled);
    }

    public override void OnUninstall()
    {
        YokiFrame.EventKit.Type.UnRegister<ShopRerolledEvent>(OnShopRerolled);
    }

    private void OnShopRerolled(ShopRerolledEvent eventData)
    {
        if (Context?.OwnerEntity is not Player player ||
            eventData.Player != player ||
            freeRerollCount <= 0 ||
            UnityEngine.Random.value > Mathf.Clamp01(chancePercent / 100f))
        {
            return;
        }

        YokiFrame.EventKit.Type.Send(new ShopFreeRerollsGrantedEvent(player, freeRerollCount));
    }
}
