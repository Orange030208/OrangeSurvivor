using System;
using System.Collections.Generic;

/// <summary>
/// 商店的唯一运行时状态源。Feature 通过语义化方法修改 Board，ShopManager 负责推进流程并提交核心结果。
/// </summary>
public sealed class ShopBoard
{
    private readonly List<ShopOfferState> offers = new();
    private readonly Dictionary<string, string> rerollBlocks = new();
    private readonly Dictionary<string, float> visitPriceMultipliers = new();
    private readonly HashSet<ShopProductKey> excludedProductKeys = new();
    private int nextOfferId = 1;
    private int visitFreeRerollCount;
    private int paidRerollCountThisVisit;
    private bool isGeneratingOffers;

    public ShopFlowStage Stage { get; private set; } = ShopFlowStage.Closed;
    public int VisitId { get; private set; }
    public IReadOnlyList<ShopOfferState> Offers => offers;
    public bool IsVisitOpen => Stage != ShopFlowStage.Closed;
    public bool IsReady => Stage == ShopFlowStage.Ready;
    public bool IsRerollBlocked => rerollBlocks.Count > 0;
    public int VisitFreeRerollCount => visitFreeRerollCount;
    public int PaidRerollCountThisVisit => paidRerollCountThisVisit;

    // 仅在对应的操作阶段有效；操作完成或商店关闭时统一重置。
    public int CurrentRerollCost { get; private set; }
    public bool IsCurrentRerollFree { get; private set; }
    public bool IsCurrentOperationRejected { get; private set; }
    public string CurrentOperationRejectMessage { get; private set; } = string.Empty;

    public bool TryBeginVisit(int visitId)
    {
        if (Stage != ShopFlowStage.Closed)
        {
            return false;
        }

        VisitId = Math.Max(1, visitId);
        visitPriceMultipliers.Clear();
        visitFreeRerollCount = 0;
        paidRerollCountThisVisit = 0;
        rerollBlocks.Clear();
        isGeneratingOffers = false;
        ResetCurrentActionState();
        Stage = ShopFlowStage.Opening;
        return true;
    }

    public bool CompleteVisitOpening()
    {
        return TrySetStage(ShopFlowStage.Opening, ShopFlowStage.Ready);
    }

    public bool TryBeginPurchase()
    {
        if (!TrySetStage(ShopFlowStage.Ready, ShopFlowStage.Purchasing))
        {
            return false;
        }

        ResetCurrentActionState();
        return true;
    }

    public bool TryBeginReroll(bool isFreeReroll, int paidCost)
    {
        if (!TrySetStage(ShopFlowStage.Ready, ShopFlowStage.Rerolling))
        {
            return false;
        }

        ResetCurrentActionState();
        IsCurrentRerollFree = isFreeReroll;
        CurrentRerollCost = Math.Max(0, paidCost);
        return true;
    }

    public bool TryBeginLock()
    {
        if (!TrySetStage(ShopFlowStage.Ready, ShopFlowStage.Locking))
        {
            return false;
        }

        ResetCurrentActionState();
        return true;
    }

    public bool CompleteOperation()
    {
        if (Stage != ShopFlowStage.Purchasing &&
            Stage != ShopFlowStage.Rerolling &&
            Stage != ShopFlowStage.Locking)
        {
            return false;
        }

        ResetCurrentActionState();
        Stage = ShopFlowStage.Ready;
        return true;
    }

    public bool TryBeginClosing()
    {
        return TrySetStage(ShopFlowStage.Ready, ShopFlowStage.Closing);
    }

    public bool CompleteClosing()
    {
        if (!TrySetStage(ShopFlowStage.Closing, ShopFlowStage.Closed))
        {
            return false;
        }

        ClearVisitState();
        return true;
    }

    public void BeginOfferGeneration()
    {
        excludedProductKeys.Clear();
        isGeneratingOffers = true;
    }

    public void EndOfferGeneration()
    {
        isGeneratingOffers = false;
    }

    public void ExcludeCurrentCandidate(ShopProductKey productKey)
    {
        if (isGeneratingOffers)
        {
            excludedProductKeys.Add(productKey);
        }
    }

    public bool IsCurrentCandidateAllowed(IShopProduct product)
    {
        return product != null && !excludedProductKeys.Contains(product.Key);
    }

    public ShopOfferState CreateOffer(IShopProduct product)
    {
        return new ShopOfferState(nextOfferId++, product);
    }

    public void ReplaceOffers(IReadOnlyList<ShopOfferState> nextOffers)
    {
        offers.Clear();
        if (nextOffers == null)
        {
            return;
        }

        for (int i = 0; i < nextOffers.Count; i++)
        {
            ShopOfferState offer = nextOffers[i];
            if (offer?.Product == null)
            {
                continue;
            }

            offer.SetSlotIndex(offers.Count);
            offers.Add(offer);
        }
    }

    public bool TryGetOffer(int offerId, out ShopOfferState offer)
    {
        for (int i = 0; i < offers.Count; i++)
        {
            if (offers[i].OfferId == offerId)
            {
                offer = offers[i];
                return true;
            }
        }

        offer = null;
        return false;
    }

    public void SetVisitPriceMultiplier(string sourceId, float multiplier)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
        {
            throw new ArgumentException("商店价格修饰来源不能为空。", nameof(sourceId));
        }

        visitPriceMultipliers[sourceId] = Math.Max(0f, multiplier);
    }

    public void RemoveVisitPriceMultiplier(string sourceId)
    {
        if (!string.IsNullOrWhiteSpace(sourceId))
        {
            visitPriceMultipliers.Remove(sourceId);
        }
    }

    public bool SetOfferPriceMultiplier(int offerId, string sourceId, float multiplier)
    {
        if (!TryGetOffer(offerId, out ShopOfferState offer))
        {
            return false;
        }

        offer.SetStatePriceMultiplier(sourceId, multiplier);
        return true;
    }

    public bool RemoveOfferPriceMultiplier(int offerId, string sourceId)
    {
        if (!TryGetOffer(offerId, out ShopOfferState offer))
        {
            return false;
        }

        offer.RemoveStatePriceMultiplier(sourceId);
        return true;
    }

    public int ApplyVisitPriceModifiers(int price)
    {
        float multiplier = 1f;
        foreach (KeyValuePair<string, float> pair in visitPriceMultipliers)
        {
            multiplier *= pair.Value;
        }

        return PropValueUtility.ResolveNonNegativePrice(Math.Max(0, price) * multiplier);
    }

    public void RejectCurrentOperation(string message = null)
    {
        if (IsOperationInProgress())
        {
            IsCurrentOperationRejected = true;
            CurrentOperationRejectMessage = string.IsNullOrWhiteSpace(message)
                ? "商店操作已被拒绝"
                : message;
        }
    }

    public void GrantVisitFreeRerolls(int count)
    {
        visitFreeRerollCount = Math.Max(0, visitFreeRerollCount + Math.Max(0, count));
    }

    public void GrantCurrentRerollFree()
    {
        if (Stage == ShopFlowStage.Rerolling)
        {
            IsCurrentRerollFree = true;
        }
    }

    public void SetCurrentRerollCost(int cost)
    {
        if (Stage == ShopFlowStage.Rerolling)
        {
            CurrentRerollCost = Math.Max(0, cost);
        }
    }

    public bool TryConsumeVisitFreeReroll()
    {
        if (visitFreeRerollCount <= 0)
        {
            return false;
        }

        visitFreeRerollCount--;
        return true;
    }

    public void RecordPaidReroll()
    {
        paidRerollCountThisVisit++;
    }

    public void AddRerollBlock(string sourceId, string reason)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
        {
            throw new ArgumentException("商店刷新阻塞来源不能为空。", nameof(sourceId));
        }

        rerollBlocks[sourceId] = reason ?? string.Empty;
    }

    public void RemoveRerollBlock(string sourceId)
    {
        if (!string.IsNullOrWhiteSpace(sourceId))
        {
            rerollBlocks.Remove(sourceId);
        }
    }

    private bool TrySetStage(ShopFlowStage expectedStage, ShopFlowStage nextStage)
    {
        if (Stage != expectedStage)
        {
            return false;
        }

        Stage = nextStage;
        return true;
    }

    private bool IsOperationInProgress()
    {
        return Stage == ShopFlowStage.Purchasing ||
               Stage == ShopFlowStage.Rerolling ||
               Stage == ShopFlowStage.Locking;
    }

    private void ResetCurrentActionState()
    {
        CurrentRerollCost = 0;
        IsCurrentRerollFree = false;
        IsCurrentOperationRejected = false;
        CurrentOperationRejectMessage = string.Empty;
    }

    private void ClearVisitState()
    {
        visitPriceMultipliers.Clear();
        visitFreeRerollCount = 0;
        paidRerollCountThisVisit = 0;
        rerollBlocks.Clear();
        excludedProductKeys.Clear();
        isGeneratingOffers = false;
        ResetCurrentActionState();
    }
}

public enum ShopFlowStage
{
    Closed,
    Opening,
    Ready,
    Purchasing,
    Rerolling,
    Locking,
    Closing
}
