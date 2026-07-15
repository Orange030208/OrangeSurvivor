using System;
using System.Collections.Generic;
using System.Text;
using Orange.GameServices;
using UnityEngine;

[Serializable]
public sealed class ShopSpendingAttributeFeature : FeatureBase
{
    [SerializeField, Min(1)] private int goldPerStack = 20;
    [SerializeField, Min(1)] private int maxStacks = 5;
    [SerializeField] private List<PropModifierData> modifiersPerStack = new();

    private ShopManager shopManager;
    private string runtimeSourceId;
    private int currentShopSpentGold;
    private int appliedStacks;

    public override string Title => "商店消费转属性";
    public override string Description => BuildDescription();

    public override void OnInstall()
    {
        runtimeSourceId = ResolveRuntimeSourceId();
        if (!GameServices.TryGet(out shopManager))
        {
            Debug.LogWarning($"[{nameof(ShopSpendingAttributeFeature)}] {nameof(ShopManager)} is unavailable.");
            return;
        }

        shopManager.VisitOpened += OnVisitOpened;
        shopManager.PurchaseCompleted += OnPurchaseCompleted;
        shopManager.VisitClosing += OnVisitClosing;
    }

    public override void OnUninstall()
    {
        if (shopManager != null)
        {
            shopManager.VisitOpened -= OnVisitOpened;
            shopManager.PurchaseCompleted -= OnPurchaseCompleted;
            shopManager.VisitClosing -= OnVisitClosing;
            shopManager = null;
        }

        RemoveAppliedModifiers();
        currentShopSpentGold = 0;
        appliedStacks = 0;
        runtimeSourceId = null;
    }

    private void OnPurchaseCompleted(ShopPurchaseSuccess purchase)
    {
        int price = purchase.Price;
        if (!IsShopOwner() || price <= 0)
        {
            return;
        }

        currentShopSpentGold += price;
    }

    private void OnVisitOpened()
    {
        if (IsShopOwner())
        {
            RemoveAppliedModifiers();
            currentShopSpentGold = 0;
        }
    }

    private void OnVisitClosing()
    {
        if (IsShopOwner())
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

    private bool IsShopOwner()
    {
        return Context?.OwnerEntity is Player player && shopManager.CurrentPlayer == player;
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
