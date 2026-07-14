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

    public ShopBoardOperation CurrentOperation { get; } = new();

    public ShopFlowStage Stage { get; private set; } = ShopFlowStage.Closed;
    public int VisitId { get; private set; }
    public ShopOfferGenerationReason? CurrentGenerationReason { get; private set; }
    public IReadOnlyList<ShopOfferState> Offers => offers;
    public bool IsVisitOpen => Stage != ShopFlowStage.Closed;
    public bool IsReady => Stage == ShopFlowStage.Ready;
    public bool IsRerollBlocked => rerollBlocks.Count > 0;
    public int VisitFreeRerollCount => visitFreeRerollCount;
    public int PaidRerollCountThisVisit => paidRerollCountThisVisit;

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
        CurrentGenerationReason = null;
        ClearCurrentOperation();
        Stage = ShopFlowStage.Opening;
        return true;
    }

    public bool CompleteVisitOpening()
    {
        return TrySetStage(ShopFlowStage.Opening, ShopFlowStage.Ready);
    }

    public bool TryBeginPurchase(ShopOfferState offer)
    {
        if (offer == null || !TrySetStage(ShopFlowStage.Ready, ShopFlowStage.Purchasing))
        {
            return false;
        }

        CurrentOperation.BeginPurchase(offer);
        return true;
    }

    public bool TryBeginReroll(int attributeFreeRerollCount, int paidCost)
    {
        if (!TrySetStage(ShopFlowStage.Ready, ShopFlowStage.Rerolling))
        {
            return false;
        }

        CurrentOperation.BeginReroll(
            visitFreeRerollCount,
            Math.Max(0, attributeFreeRerollCount),
            Math.Max(0, paidCost));
        return true;
    }

    public bool TryBeginLock(ShopOfferState offer, bool willBeLocked)
    {
        if (offer == null || !TrySetStage(ShopFlowStage.Ready, ShopFlowStage.Locking))
        {
            return false;
        }

        CurrentOperation.BeginLock(offer, willBeLocked);
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

        ClearCurrentOperation();
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

    public void BeginOfferGeneration(ShopOfferGenerationReason reason)
    {
        excludedProductKeys.Clear();
        CurrentGenerationReason = reason;
    }

    public void EndOfferGeneration()
    {
        CurrentGenerationReason = null;
    }

    public void ExcludeCurrentCandidate(ShopProductKey productKey)
    {
        if (CurrentGenerationReason.HasValue)
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

    public int ApplyCurrentPurchaseModifiers(int price)
    {
        float multiplier = CurrentOperation.Type == ShopBoardOperationType.Purchase
            ? CurrentOperation.PriceMultiplier
            : 1f;
        return PropValueUtility.ResolveNonNegativePrice(ApplyVisitPriceModifiers(price) * multiplier);
    }

    public void MultiplyCurrentPurchasePrice(float multiplier)
    {
        if (CurrentOperation.Type == ShopBoardOperationType.Purchase)
        {
            CurrentOperation.MultiplyPrice(Math.Max(0f, multiplier));
        }
    }

    public void RejectCurrentOperation(string message = null)
    {
        if (CurrentOperation.Type != ShopBoardOperationType.None)
        {
            CurrentOperation.Reject(message);
        }
    }

    public void GrantVisitFreeRerolls(int count)
    {
        visitFreeRerollCount = Math.Max(0, visitFreeRerollCount + Math.Max(0, count));
    }

    public void GrantCurrentRerollFree()
    {
        if (CurrentOperation.Type == ShopBoardOperationType.Reroll)
        {
            CurrentOperation.GrantFreeReroll();
        }
    }

    public void SetCurrentRerollCost(int cost)
    {
        if (CurrentOperation.Type == ShopBoardOperationType.Reroll)
        {
            CurrentOperation.SetPaidRerollCost(Math.Max(0, cost));
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

    private void ClearCurrentOperation()
    {
        CurrentOperation.Reset();
    }

    private void ClearVisitState()
    {
        visitPriceMultipliers.Clear();
        visitFreeRerollCount = 0;
        paidRerollCountThisVisit = 0;
        rerollBlocks.Clear();
        excludedProductKeys.Clear();
        CurrentGenerationReason = null;
        ClearCurrentOperation();
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

public enum ShopBoardOperationType
{
    None,
    Purchase,
    Reroll,
    Lock
}

public enum ShopOfferGenerationReason
{
    Initial,
    VisitEntry,
    Reroll
}

public enum ShopFreeRerollSource
{
    None,
    Visit,
    Attribute,
    Granted
}

/// <summary>
/// Board 中唯一的阶段临时状态。Feature 只读它，并通过 <see cref="ShopBoard"/> 的方法修改允许的数据。
/// </summary>
public sealed class ShopBoardOperation
{
    public ShopBoardOperationType Type { get; private set; }
    public ShopOfferState Offer { get; private set; }
    public bool WillBeLocked { get; private set; }
    public float PriceMultiplier { get; private set; } = 1f;
    public int PaidRerollCost { get; private set; }
    public int AttributeFreeRerollCount { get; private set; }
    public ShopFreeRerollSource FreeRerollSource { get; private set; }
    public bool IsRejected { get; private set; }
    public string RejectMessage { get; private set; } = string.Empty;

    public bool UsesFreeReroll => FreeRerollSource != ShopFreeRerollSource.None;

    internal void BeginPurchase(ShopOfferState offer)
    {
        Reset();
        Type = ShopBoardOperationType.Purchase;
        Offer = offer;
    }

    internal void BeginReroll(int visitFreeRerollCount, int attributeFreeRerollCount, int paidCost)
    {
        Reset();
        Type = ShopBoardOperationType.Reroll;
        PaidRerollCost = paidCost;
        AttributeFreeRerollCount = attributeFreeRerollCount;
        FreeRerollSource = visitFreeRerollCount > 0
            ? ShopFreeRerollSource.Visit
            : attributeFreeRerollCount > 0
                ? ShopFreeRerollSource.Attribute
                : ShopFreeRerollSource.None;
    }

    internal void BeginLock(ShopOfferState offer, bool willBeLocked)
    {
        Reset();
        Type = ShopBoardOperationType.Lock;
        Offer = offer;
        WillBeLocked = willBeLocked;
    }

    internal void MultiplyPrice(float multiplier)
    {
        PriceMultiplier *= multiplier;
    }

    internal void GrantFreeReroll()
    {
        FreeRerollSource = ShopFreeRerollSource.Granted;
    }

    internal void SetPaidRerollCost(int cost)
    {
        PaidRerollCost = cost;
    }

    internal void Reject(string message)
    {
        IsRejected = true;
        RejectMessage = string.IsNullOrWhiteSpace(message) ? "Shop operation rejected." : message;
    }

    internal void Reset()
    {
        Type = ShopBoardOperationType.None;
        Offer = null;
        WillBeLocked = false;
        PriceMultiplier = 1f;
        PaidRerollCost = 0;
        AttributeFreeRerollCount = 0;
        FreeRerollSource = ShopFreeRerollSource.None;
        IsRejected = false;
        RejectMessage = string.Empty;
    }
}
