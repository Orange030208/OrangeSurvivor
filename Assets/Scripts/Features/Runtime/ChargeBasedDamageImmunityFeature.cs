using System;
using UnityEngine;

[Serializable]
public sealed class ChargeBasedDamageImmunityFeature : HitModifierFeatureBase, IWaveStartResettableFeatureEffect
{
    [SerializeField, Min(0)] private int maxCharges = 1;
    [SerializeField] private bool refreshOnWaveStart = true;

    private int remainingCharges;

    public ChargeBasedDamageImmunityFeature()
    {
        hitModifierTiming = HitModifierTiming.Receive;
    }

    public override int HitPriority => HitModifierPriority.Override;
    public int MaxCharges => Mathf.Max(0, maxCharges);
    public int RemainingCharges => Mathf.Max(0, remainingCharges);
    public override string Title => "次数免伤";
    public override string Description => BuildDescription();

    public override void OnInstall()
    {
        ResetCharges();
        if (refreshOnWaveStart)
        {
            GameEventBus.Subscribe<WaveStartedEvent>(OnWaveStarted);
        }
    }

    public override void OnUninstall()
    {
        if (refreshOnWaveStart)
        {
            GameEventBus.Unsubscribe<WaveStartedEvent>(OnWaveStarted);
        }
    }

    public override void ModifyHit(HitContext hitContext)
    {
        if (hitContext == null ||
            hitContext.IsCancelled ||
            hitContext.IsDodged ||
            hitContext.IsBlocked ||
            RemainingCharges <= 0)
        {
            return;
        }

        hitContext.IsBlocked = true;
        remainingCharges = Mathf.Max(0, remainingCharges - 1);
    }

    public void ResetForWaveStart()
    {
        ResetCharges();
    }

    public void ResetCharges()
    {
        remainingCharges = MaxCharges;
    }

    private void OnWaveStarted(WaveStartedEvent eventData)
    {
        ResetCharges();
    }

    private string BuildDescription()
    {
        if (MaxCharges <= 0)
        {
            return "不提供伤害免疫次数。";
        }

        return refreshOnWaveStart
            ? $"免疫 {MaxCharges} 次受到的伤害，每波开始时刷新次数。"
            : $"免疫 {MaxCharges} 次受到的伤害。";
    }
}
