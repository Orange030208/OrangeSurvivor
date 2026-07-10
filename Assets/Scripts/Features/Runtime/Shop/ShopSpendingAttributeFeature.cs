using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[Serializable]
public sealed class ShopSpendingAttributeFeature : FeatureBase
{
    [SerializeField, Min(1)] private int goldPerStack = 20;
    [SerializeField, Min(1)] private int maxStacks = 5;
    [SerializeField] private List<PropModifierData> modifiersPerStack = new();

    private string runtimeSourceId;
    private int currentShopSpentGold;
    private int appliedStacks;

    public override string Title => "商店消费转属性";
    public override string Description => BuildDescription();

    public override void OnInstall()
    {
        runtimeSourceId = ResolveRuntimeSourceId();
        YokiFrame.EventKit.Type.Register<ShopItemPurchasedEvent>(OnShopItemPurchased);
        YokiFrame.EventKit.Type.Register<GameStateChangedEvent>(OnGameStateChanged);
    }

    public override void OnUninstall()
    {
        YokiFrame.EventKit.Type.UnRegister<ShopItemPurchasedEvent>(OnShopItemPurchased);
        YokiFrame.EventKit.Type.UnRegister<GameStateChangedEvent>(OnGameStateChanged);
        RemoveAppliedModifiers();
        currentShopSpentGold = 0;
        appliedStacks = 0;
        runtimeSourceId = null;
    }

    private void OnShopItemPurchased(ShopItemPurchasedEvent eventData)
    {
        if (Context?.OwnerEntity is not Player player || eventData.Player != player || eventData.Price <= 0)
        {
            return;
        }

        currentShopSpentGold += eventData.Price;
    }

    private void OnGameStateChanged(GameStateChangedEvent eventData)
    {
        if (eventData.NewState == GameState.Shop)
        {
            RemoveAppliedModifiers();
            currentShopSpentGold = 0;
            return;
        }

        if (eventData.OldState == GameState.Shop && eventData.NewState == GameState.Game)
        {
            ApplyModifiersForCurrentShopSpending();
        }
    }

    private void ApplyModifiersForCurrentShopSpending()
    {
        RemoveAppliedModifiers();
        int stacks = ResolveStacks(currentShopSpentGold);
        if (stacks <= 0 || Context?.AttributeManager == null || modifiersPerStack == null || modifiersPerStack.Count == 0)
        {
            return;
        }

        List<PropModifierData> scaledModifiers = new(modifiersPerStack.Count);
        for (int i = 0; i < modifiersPerStack.Count; i++)
        {
            PropModifierData modifier = modifiersPerStack[i];
            scaledModifiers.Add(new PropModifierData(modifier.propType, modifier.modifierType, modifier.value * stacks));
        }

        Context.AttributeManager.AddModifiers(runtimeSourceId, scaledModifiers);
        appliedStacks = stacks;
    }

    private void RemoveAppliedModifiers()
    {
        if (appliedStacks > 0 && Context?.AttributeManager != null && !string.IsNullOrWhiteSpace(runtimeSourceId))
        {
            Context.AttributeManager.RemoveModifiers(runtimeSourceId);
        }

        appliedStacks = 0;
    }

    private int ResolveStacks(int spentGold)
    {
        if (goldPerStack <= 0 || maxStacks <= 0 || spentGold <= 0)
        {
            return 0;
        }

        return Mathf.Clamp(spentGold / goldPerStack, 0, maxStacks);
    }

    private string ResolveRuntimeSourceId()
    {
        if (!string.IsNullOrWhiteSpace(runtimeSourceId))
        {
            return runtimeSourceId;
        }

        return string.IsNullOrWhiteSpace(SourceId)
            ? $"{nameof(ShopSpendingAttributeFeature)}_{GetHashCode()}"
            : $"{SourceId}:{nameof(ShopSpendingAttributeFeature)}_{GetHashCode()}";
    }

    private string BuildDescription()
    {
        string modifierSummary = BuildModifierSummary(modifiersPerStack);
        if (string.IsNullOrWhiteSpace(modifierSummary))
        {
            return $"每波在商店每花费 {Mathf.Max(1, goldPerStack)} 金币，下一波获得属性强化，最多 {Mathf.Max(1, maxStacks)} 层。";
        }

        return $"每波在商店每花费 {Mathf.Max(1, goldPerStack)} 金币，下一波获得{modifierSummary}，最多 {Mathf.Max(1, maxStacks)} 层。";
    }

    private static string BuildModifierSummary(IReadOnlyList<PropModifierData> modifiers)
    {
        if (modifiers == null || modifiers.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new();
        for (int i = 0; i < modifiers.Count; i++)
        {
            if (i > 0)
            {
                builder.Append('，');
            }

            builder.Append(modifiers[i].GetAutoDescription());
        }

        return builder.ToString();
    }
}
