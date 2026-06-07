using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[Serializable]
public sealed class GoldThresholdPropertyFeature : FeatureBase
{
    private const int MIN_GOLD_PER_STACK = 1;

    [SerializeField, Min(MIN_GOLD_PER_STACK)] private int goldPerStack = 100;
    [SerializeField, Min(0)] private int maxStacks = 10;
    [SerializeField] private List<PropModifierData> modifiersPerStack = new();

    private string runtimeSourceId;
    private int appliedStacks = -1;
    private bool requiresInitialRefresh;

    public int CurrentStacks => Mathf.Max(0, appliedStacks);
    public override string Title => "金币阶梯属性";
    public override string Description => BuildDescription();

    public override void OnInstall()
    {
        runtimeSourceId = ResolveRuntimeSourceId();
        CurrencyWallet wallet = Context?.CurrencyWallet;
        if (wallet != null)
        {
            wallet.OnAmountChanged += OnCurrencyAmountChanged;
        }

        RefreshStacks(force: true);
        requiresInitialRefresh = true;
    }

    public override void OnUninstall()
    {
        CurrencyWallet wallet = Context?.CurrencyWallet;
        if (wallet != null)
        {
            wallet.OnAmountChanged -= OnCurrencyAmountChanged;
        }

        RemoveAppliedModifiers();
        appliedStacks = -1;
        requiresInitialRefresh = false;
        runtimeSourceId = null;
    }

    public override void OnUpdate(float deltaTime)
    {
        if (!requiresInitialRefresh)
        {
            return;
        }

        requiresInitialRefresh = false;
        RefreshStacks(force: true);
    }

    private void OnCurrencyAmountChanged(int currentAmount, int changeAmount)
    {
        RefreshStacks(force: false);
    }

    private void RefreshStacks(bool force)
    {
        int nextStacks = ResolveCurrentStacks();
        if (!force && nextStacks == appliedStacks)
        {
            return;
        }

        ApplyStacks(nextStacks);
    }

    private int ResolveCurrentStacks()
    {
        CurrencyWallet wallet = Context?.CurrencyWallet;
        if (wallet == null || goldPerStack <= 0)
        {
            return 0;
        }

        int safeMaxStacks = Mathf.Max(0, maxStacks);
        if (safeMaxStacks == 0)
        {
            return 0;
        }

        int stackCount = Mathf.FloorToInt((float)wallet.CurrentAmount / Mathf.Max(MIN_GOLD_PER_STACK, goldPerStack));
        return Mathf.Clamp(stackCount, 0, safeMaxStacks);
    }

    private void ApplyStacks(int stackCount)
    {
        RemoveAppliedModifiers();
        appliedStacks = stackCount;

        if (stackCount <= 0 || Context?.PropertiesManager == null || modifiersPerStack == null || modifiersPerStack.Count == 0)
        {
            return;
        }

        List<PropModifierData> scaledModifiers = new(modifiersPerStack.Count);
        for (int i = 0; i < modifiersPerStack.Count; i++)
        {
            PropModifierData modifier = modifiersPerStack[i];
            scaledModifiers.Add(new PropModifierData(modifier.propType, modifier.modifierType, modifier.value * stackCount));
        }

        Context.PropertiesManager.AddModifiers(runtimeSourceId, scaledModifiers);
    }

    private void RemoveAppliedModifiers()
    {
        if (Context?.PropertiesManager == null || string.IsNullOrWhiteSpace(runtimeSourceId))
        {
            return;
        }

        Context.PropertiesManager.RemoveModifiers(runtimeSourceId);
    }

    private string ResolveRuntimeSourceId()
    {
        if (!string.IsNullOrWhiteSpace(runtimeSourceId))
        {
            return runtimeSourceId;
        }

        return string.IsNullOrWhiteSpace(SourceId)
            ? $"{nameof(GoldThresholdPropertyFeature)}_{GetHashCode()}"
            : $"{SourceId}:{nameof(GoldThresholdPropertyFeature)}_{GetHashCode()}";
    }

    private string BuildDescription()
    {
        if (modifiersPerStack == null || modifiersPerStack.Count == 0)
        {
            return "未配置金币阶梯加成。";
        }

        int safeGoldPerStack = Mathf.Max(MIN_GOLD_PER_STACK, goldPerStack);
        int safeMaxStacks = Mathf.Max(0, maxStacks);
        return $"每持有 {safeGoldPerStack} 金币获得 1 层，" +
               $"每层{BuildModifierSummary(modifiersPerStack)}，最多 {safeMaxStacks} 层，金币变化时实时更新。";
    }

    private static string BuildModifierSummary(IReadOnlyList<PropModifierData> propertyModifiers)
    {
        StringBuilder builder = new();
        for (int i = 0; i < propertyModifiers.Count; i++)
        {
            if (i > 0)
            {
                builder.Append('，');
            }

            builder.Append(propertyModifiers[i].GetAutoDescription());
        }

        return builder.ToString();
    }
}
