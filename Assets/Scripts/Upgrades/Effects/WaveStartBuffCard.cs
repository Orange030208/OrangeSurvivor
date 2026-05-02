using System;
using UnityEngine;

[Serializable]
public sealed class WaveStartBuffCard : FeatureEffectBase
{
    [SerializeField] private BuffDataSO buffData;
    [SerializeField] private float durationSeconds = 8f;
    [SerializeField] private bool applyImmediately = true;

    public WaveStartBuffCard()
    {
    }

    public WaveStartBuffCard(BuffDataSO buffData, float durationSeconds, bool applyImmediately)
    {
        this.buffData = buffData;
        this.durationSeconds = durationSeconds;
        this.applyImmediately = applyImmediately;
    }

    public override string Description
    {
        get
        {
            string buffName = buffData != null ? buffData.DisplayName : "指定 Buff";
            return $"每波开始获得 {durationSeconds:0.#} 秒{buffName}。";
        }
    }

    public override void OnInstall()
    {
        BuffController buffController = Context?.GetComponent<BuffController>();
        if (buffController == null || buffData == null)
        {
            return;
        }

        buffController.RegisterWaveStartBuff(
            new BuffApplyRequest(buffData, BuffDurationPolicy.Timed, durationSeconds),
            applyImmediately);
    }
}
