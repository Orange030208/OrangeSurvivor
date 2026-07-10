using System;
using UnityEngine;

[Serializable]
public sealed class ShopVisitFreeRerollFeature : FeatureBase
{
    [SerializeField, Min(1)] private int freeRerollCount = 1;
    [SerializeField] private bool requireNextWave = true;

    public override string Title => "商店免费刷新";
    public override string Description => requireNextWave
        ? $"每次波次结束并进入商店时，获得 {Mathf.Max(1, freeRerollCount)} 次免费刷新。"
        : $"每次波次结束时，获得 {Mathf.Max(1, freeRerollCount)} 次免费刷新。";

    public override void OnInstall()
    {
        YokiFrame.EventKit.Type.Register<WaveCompletedEvent>(OnWaveCompleted);
    }

    public override void OnUninstall()
    {
        YokiFrame.EventKit.Type.UnRegister<WaveCompletedEvent>(OnWaveCompleted);
    }

    private void OnWaveCompleted(WaveCompletedEvent eventData)
    {
        if (freeRerollCount <= 0 || requireNextWave && !eventData.HasNextWave)
        {
            return;
        }

        if (Context?.OwnerEntity is not Player player)
        {
            return;
        }

        YokiFrame.EventKit.Type.Send(new ShopFreeRerollsGrantedEvent(player, freeRerollCount));
    }
}
