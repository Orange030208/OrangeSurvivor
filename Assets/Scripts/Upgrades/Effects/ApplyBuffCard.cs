using System;
using UnityEngine;

[Serializable]
public sealed class ApplyBuffCard : FeatureEffectBase
{
    [SerializeField] private BuffDataSO buffData;
    [SerializeField] private bool overrideDuration;
    [SerializeField] private BuffDurationPolicy durationPolicy = BuffDurationPolicy.Timed;
    [SerializeField] private float durationSeconds = 8f;

    public ApplyBuffCard()
    {
    }

    public ApplyBuffCard(BuffDataSO buffData, float durationSeconds)
    {
        this.buffData = buffData;
        overrideDuration = true;
        durationPolicy = BuffDurationPolicy.Timed;
        this.durationSeconds = durationSeconds;
    }

    public override string Description
    {
        get
        {
            string buffName = buffData != null ? buffData.DisplayName : "指定 Buff";
            return overrideDuration
                ? $"立即获得 {durationSeconds:0.#} 秒{buffName}。"
                : $"立即获得{buffName}。";
        }
    }

    public override void OnInstall()
    {
        BuffController buffController = Context?.GetComponent<BuffController>();
        if (buffController == null || buffData == null)
        {
            return;
        }

        BuffApplyRequest request = overrideDuration
            ? new BuffApplyRequest(buffData, durationPolicy, durationSeconds)
            : new BuffApplyRequest(buffData);
        buffController.ApplyBuff(request);
    }
}
