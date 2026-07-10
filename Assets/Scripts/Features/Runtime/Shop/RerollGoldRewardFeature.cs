using System;
using UnityEngine;

[Serializable]
public sealed class RerollGoldRewardFeature : FeatureBase
{
    [SerializeField, Min(0)] private int paidRerollBaseGold = 5;
    [SerializeField, Min(0)] private int fallbackWaveNumber = 1;

    private int currentWaveNumber = 1;

    public override string Title => "刷新金币回响";
    public override string Description =>
        $"每次付费刷新获得 {Mathf.Max(0, paidRerollBaseGold)} + 当前波次金币；免费刷新只获得当前波次金币。";

    public override void OnInstall()
    {
        currentWaveNumber = Mathf.Max(1, fallbackWaveNumber);
        YokiFrame.EventKit.Type.Register<ShopRerolledEvent>(OnShopRerolled);
        YokiFrame.EventKit.Type.Register<WaveStartedEvent>(OnWaveStarted);
        YokiFrame.EventKit.Type.Register<WaveCompletedEvent>(OnWaveCompleted);
        YokiFrame.EventKit.Type.Register<WaveRuntimeChangedEvent>(OnWaveRuntimeChanged);
    }

    public override void OnUninstall()
    {
        YokiFrame.EventKit.Type.UnRegister<ShopRerolledEvent>(OnShopRerolled);
        YokiFrame.EventKit.Type.UnRegister<WaveStartedEvent>(OnWaveStarted);
        YokiFrame.EventKit.Type.UnRegister<WaveCompletedEvent>(OnWaveCompleted);
        YokiFrame.EventKit.Type.UnRegister<WaveRuntimeChangedEvent>(OnWaveRuntimeChanged);
    }

    private void OnShopRerolled(ShopRerolledEvent eventData)
    {
        if (Context?.OwnerEntity is not Player player || eventData.Player != player)
        {
            return;
        }

        CurrencyWallet wallet = Context.CurrencyWallet;
        if (wallet == null)
        {
            return;
        }

        int waveNumber = Mathf.Max(1, currentWaveNumber);
        int rewardGold = eventData.UsedFreeReroll
            ? waveNumber
            : Mathf.Max(0, paidRerollBaseGold) + waveNumber;

        wallet.ChangeAmount(rewardGold);
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
}
