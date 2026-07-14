using System;
using System.Collections.Generic;
using Orange.GameServices;
using UnityEngine;

/// <summary>
/// 局内商店管理器。它只推进固定流程，所有商店状态与阶段临时状态由 <see cref="ShopBoard"/> 统一维护。
/// </summary>
[Serializable]
public sealed class ShopManager : GameService
{
    private const int DEFAULT_CONTAINERS_TO_ADD = 4;
    private const int DEFAULT_REROLL_STEP_COST = 1;
    private const string FREE_REROLL_CONSUMPTION_SOURCE_PREFIX = "ShopManager.FreeRerollConsumption";
    private const string FREE_REROLL_GRANT_SOURCE_PREFIX = "ShopManager.FreeRerollGrant";

    [SerializeField] private int containersToAdd = DEFAULT_CONTAINERS_TO_ADD;
    [SerializeField] private CurrencyWallet currencyWallet;

    private readonly ShopExtractionRoller extractionRoller = new();
    private readonly ShopBoard board = new();
    private Player player;
    private AttributeManager AttributeManager;
    private int currentCurrency;
    private bool isExecutingShopOperation;
    private bool hasDeferredViewState;
    private ShopRefreshReason deferredViewStateReason;

    private UnityEngine.Object LogContext => Context != null ? Context.Root : null;

    public event Action<ShopViewState> ViewStateChanged;
    public event Action<ShopPurchaseSuccess> PurchaseSucceeded;
    public event Action<ShopPurchaseFailure> PurchaseFailed;

    public event Action StageChanged;
    public event Action VisitOpened;
    public event Action VisitClosing;
    public event Action VisitClosed;
    public event Action OffersGenerating;
    public event Action PurchasePreparing;
    public event Action PurchaseCompleted;
    public event Action RerollPreparing;
    public event Action RerollCompleted;
    public event Action LockChanging;
    public event Action LockChanged;

    public ShopBoard Board => board;
    public ShopFlowStage CurrentStage => board.Stage;
    public bool IsVisitOpen => board.IsVisitOpen;
    public int CurrentVisitId => board.VisitId;
    public Player CurrentPlayer => player;

    protected override void DeclareDependencies(GameServiceDependencyBuilder dependencies)
    {
        dependencies.Require<IGameContentProvider>();
    }

    protected override void OnAttach()
    {
        YokiFrame.EventKit.Type.Register<PlayerSpawnedEvent>(OnPlayerSpawned);
        AddCleanup(() => YokiFrame.EventKit.Type.UnRegister<PlayerSpawnedEvent>(OnPlayerSpawned));

        YokiFrame.EventKit.Type.Register<GameStateChangedEvent>(OnGameStateChanged);
        AddCleanup(() => YokiFrame.EventKit.Type.UnRegister<GameStateChangedEvent>(OnGameStateChanged));

        YokiFrame.EventKit.Type.Register<ShopFreeRerollsGrantedEvent>(OnShopFreeRerollsGranted);
        AddCleanup(() => YokiFrame.EventKit.Type.UnRegister<ShopFreeRerollsGrantedEvent>(OnShopFreeRerollsGranted));

        TryBindWallet();
        RefreshCurrency();
    }

    protected override void OnStart()
    {
        GenerateShopOffers();
        PublishViewState(ShopRefreshReason.Initial);
    }

    protected override void OnDispose()
    {
        UnbindCurrencyWallet();
        UnbindAttributeManager();
        ViewStateChanged = null;
        PurchaseSucceeded = null;
        PurchaseFailed = null;
        StageChanged = null;
        VisitOpened = null;
        VisitClosing = null;
        VisitClosed = null;
        OffersGenerating = null;
        PurchasePreparing = null;
        PurchaseCompleted = null;
        RerollPreparing = null;
        RerollCompleted = null;
        LockChanging = null;
        LockChanged = null;
    }

    private void OnPlayerSpawned(PlayerSpawnedEvent eventData)
    {
        player = eventData.Player;
        BindAttributeManager(player != null ? player.GetComponent<AttributeManager>() : null);
        BindCurrencyWallet(player != null ? player.GetComponent<CurrencyWallet>() : null);
    }

    private void OnGameStateChanged(GameStateChangedEvent eventData)
    {
        if (eventData.NewState == GameState.Shop && eventData.OldState != GameState.Shop)
        {
            OpenVisit();
            return;
        }

        if (eventData.OldState == GameState.Shop && eventData.NewState != GameState.Shop)
        {
            CloseVisit();
        }
    }

    public void RefreshViewState()
    {
        PublishViewState(ShopRefreshReason.StateUpdate);
    }

    private void OnCurrencyAmountChanged(int currentAmount, int changeAmount)
    {
        currentCurrency = currentAmount;
        PublishViewState(ShopRefreshReason.StateUpdate);
    }

    public void RequestBuyOffer(int offerId)
    {
        if (isExecutingShopOperation)
        {
            return;
        }

        if (!board.TryGetOffer(offerId, out ShopOfferState offer))
        {
            NotifyPurchaseFailed("Invalid shop offer.");
            return;
        }

        if (!board.TryBeginPurchase(offer))
        {
            return;
        }

        NotifyStageChanged();
        ExecuteShopOperation(RequestBuyOfferCore);
    }

    private void RequestBuyOfferCore()
    {
        ShopOfferState offer = board.CurrentOperation.Offer;
        if (offer?.Product == null)
        {
            NotifyPurchaseFailed("Item data is null.");
            return;
        }

        if (offer.IsSoldOut)
        {
            NotifyPurchaseFailed("Item already sold out.");
            return;
        }

        PurchasePreparing?.Invoke();
        if (board.CurrentOperation.IsRejected)
        {
            NotifyPurchaseFailed(board.CurrentOperation.RejectMessage);
            return;
        }

        int price = board.ApplyCurrentPurchaseModifiers(ResolveBaseOfferPrice(offer));
        if (currentCurrency < price)
        {
            NotifyPurchaseFailed("Not enough currency.");
            return;
        }

        ShopPurchaseContext purchaseContext = new(
            player,
            ResolveWeaponsHolder(),
            ResolveAccessoryManager(),
            currencyWallet);
        ShopPurchaseResult purchaseResult = offer.Product.TryPurchase(purchaseContext);
        if (!purchaseResult.Succeeded)
        {
            NotifyPurchaseFailed(purchaseResult.FailureMessage);
            return;
        }

        offer.MarkSoldOut();
        currencyWallet?.ChangeAmount(-price);
        PurchaseCompleted?.Invoke();
        AudioSfxBridge.RequestPlay(AudioSfxKey.ShopPurchaseSucceeded);
        NotifyPurchaseSucceeded(offer, price);
        PublishViewState(ShopRefreshReason.Purchase);
    }

    public void RequestReroll()
    {
        if (isExecutingShopOperation)
        {
            return;
        }

        if (board.IsRerollBlocked)
        {
            NotifyPurchaseFailed("Shop reroll is blocked.");
            return;
        }

        if (!board.TryBeginReroll(ResolvePlayerFreeRerollCount(), ResolveCurrentRerollCost()))
        {
            return;
        }

        NotifyStageChanged();
        ExecuteShopOperation(RequestRerollCore);
    }

    private void RequestRerollCore()
    {
        RerollPreparing?.Invoke();
        if (board.CurrentOperation.IsRejected)
        {
            NotifyPurchaseFailed(board.CurrentOperation.RejectMessage);
            return;
        }

        bool useFreeReroll = board.CurrentOperation.UsesFreeReroll;
        int rerollCost = board.CurrentOperation.PaidRerollCost;
        if (!useFreeReroll && currentCurrency < rerollCost)
        {
            NotifyPurchaseFailed($"Not enough currency for reroll. Cost: {rerollCost}");
            return;
        }

        switch (board.CurrentOperation.FreeRerollSource)
        {
            case ShopFreeRerollSource.Visit:
                board.TryConsumeVisitFreeReroll();
                break;
            case ShopFreeRerollSource.Attribute:
                TryConsumeAttributeFreeReroll();
                break;
            case ShopFreeRerollSource.None:
                currencyWallet?.ChangeAmount(-rerollCost);
                break;
        }

        RerollShopOffers(trackAsPaidReroll: !useFreeReroll);
        RerollCompleted?.Invoke();
        NotifyShopRerolled(usedFreeReroll: useFreeReroll);
        AudioSfxBridge.RequestPlay(AudioSfxKey.ShopRerolled);
        PublishViewState(ShopRefreshReason.Reroll);
    }

    public void RequestToggleOfferLock(int offerId)
    {
        if (isExecutingShopOperation ||
            !board.TryGetOffer(offerId, out ShopOfferState offer) ||
            offer.IsSoldOut ||
            !board.TryBeginLock(offer, !offer.IsLocked))
        {
            return;
        }

        NotifyStageChanged();
        ExecuteShopOperation(RequestToggleOfferLockCore);
    }

    private void RequestToggleOfferLockCore()
    {
        LockChanging?.Invoke();
        if (board.CurrentOperation.IsRejected)
        {
            return;
        }

        ShopOfferState offer = board.CurrentOperation.Offer;
        offer.SetLocked(board.CurrentOperation.WillBeLocked);
        LockChanged?.Invoke();
        if (offer.IsLocked)
        {
            NotifyItemLocked(offer);
        }

        Debug.Log($"物品:{offer.Product.DisplayName} 锁定状态:{offer.IsLocked}", LogContext);
        PublishViewState(ShopRefreshReason.StateUpdate);
    }

    private void OpenVisit()
    {
        if (!board.TryBeginVisit(board.VisitId + 1))
        {
            return;
        }

        NotifyStageChanged();
        try
        {
            RefreshShopForWaveEntry();
            VisitOpened?.Invoke();
            PublishViewState(ShopRefreshReason.WaveRefresh);
        }
        finally
        {
            if (board.CompleteVisitOpening())
            {
                NotifyStageChanged();
            }
        }
    }

    private void CloseVisit()
    {
        if (!board.TryBeginClosing())
        {
            return;
        }

        NotifyStageChanged();
        try
        {
            VisitClosing?.Invoke();
            VisitClosed?.Invoke();
        }
        finally
        {
            if (board.CompleteClosing())
            {
                NotifyStageChanged();
            }
        }
    }

    private void RefreshShopForWaveEntry()
    {
        int count = ResolveOfferCount();
        List<ShopOfferState> nextOffers = CreateLockedCarryoverOffers(count, markLockedAsPreviousVisit: true);
        FillWithRandomOffers(nextOffers, count, ShopOfferGenerationReason.VisitEntry);
        board.ReplaceOffers(nextOffers);
    }

    private void GenerateShopOffers()
    {
        int count = ResolveOfferCount();
        List<ShopOfferState> nextOffers = new(count);
        FillWithRandomOffers(nextOffers, count, ShopOfferGenerationReason.Initial);
        board.ReplaceOffers(nextOffers);
    }

    private void RerollShopOffers(bool trackAsPaidReroll)
    {
        if (trackAsPaidReroll)
        {
            board.RecordPaidReroll();
        }

        int count = ResolveOfferCount();
        List<ShopOfferState> nextOffers = CreateLockedCarryoverOffers(count, markLockedAsPreviousVisit: false);
        FillWithRandomOffers(nextOffers, count, ShopOfferGenerationReason.Reroll);
        board.ReplaceOffers(nextOffers);
    }

    private List<ShopOfferState> CreateLockedCarryoverOffers(int maxCount, bool markLockedAsPreviousVisit)
    {
        List<ShopOfferState> nextOffers = new(maxCount);
        IReadOnlyList<ShopOfferState> currentOffers = board.Offers;
        for (int i = 0; i < currentOffers.Count && nextOffers.Count < maxCount; i++)
        {
            ShopOfferState offer = currentOffers[i];
            if (offer == null || !offer.IsLocked || offer.IsSoldOut || offer.Product == null)
            {
                continue;
            }

            if (markLockedAsPreviousVisit)
            {
                offer.MarkLockedStateAsPreviousVisit();
            }

            nextOffers.Add(offer);
        }

        return nextOffers;
    }

    private bool FillWithRandomOffers(List<ShopOfferState> offers, int targetCount, ShopOfferGenerationReason reason)
    {
        if (offers == null)
        {
            return false;
        }

        board.BeginOfferGeneration(reason);
        OffersGenerating?.Invoke();
        try
        {
            while (offers.Count < targetCount)
            {
                ShopOfferState offer = GenerateRandomShopOffer(offers);
                if (offer == null)
                {
                    break;
                }

                offers.Add(offer);
            }

            return offers.Count >= targetCount;
        }
        finally
        {
            board.EndOfferGeneration();
        }
    }

    private ShopOfferState GenerateRandomShopOffer(IReadOnlyList<ShopOfferState> existingOffers)
    {
        for (int attempt = 0; attempt < 8; attempt++)
        {
            IShopProduct product = RollShopProduct();
            if (product != null && board.IsCurrentCandidateAllowed(product) && !ContainsDuplicate(existingOffers, product))
            {
                return board.CreateOffer(product);
            }
        }

        IShopProduct fallbackProduct = RollShopProduct();
        return fallbackProduct != null && board.IsCurrentCandidateAllowed(fallbackProduct)
            ? board.CreateOffer(fallbackProduct)
            : null;
    }

    private bool ContainsDuplicate(IReadOnlyList<ShopOfferState> existingOffers, IShopProduct product)
    {
        if (existingOffers == null || product == null)
        {
            return false;
        }

        for (int i = 0; i < existingOffers.Count; i++)
        {
            if (existingOffers[i]?.Product != null && existingOffers[i].Product.Key.Equals(product.Key))
            {
                return true;
            }
        }

        return false;
    }

    private IShopProduct RollShopProduct()
    {
        if (!GameContentRuntime.TryGetProvider(out IGameContentProvider provider))
        {
            Debug.LogError($"[{nameof(ShopManager)}] Missing {nameof(IGameContentProvider)}. Cannot roll shop item.", LogContext);
            return null;
        }

        if (provider.ContentTierWeightProfile == null)
        {
            Debug.LogError(
                $"[{nameof(ShopManager)}] Missing {nameof(ContentTierWeightProfileSO)} in {nameof(GameContentCatalogSO)}.",
                LogContext);
            return null;
        }

        ShopExtractionContext context = new(ResolveAccessoryManager(), ResolvePlayerLuck());
        if (!extractionRoller.TryRollOne(
                provider.Weapons,
                provider.Accessories,
                provider.ContentTierWeightProfile,
                context,
                out ShopExtractionCandidate candidate))
        {
            Debug.LogWarning(
                $"[{nameof(ShopManager)}] No shop item could be rolled from configured weapon/accessory candidates.",
                LogContext);
            return null;
        }

        return candidate.Product;
    }

    private AccessoryManager ResolveAccessoryManager()
    {
        return player != null && player.TryGetComponent(out AccessoryManager resolvedAccessoryManager)
            ? resolvedAccessoryManager
            : UnityEngine.Object.FindFirstObjectByType<AccessoryManager>();
    }

    private WeaponsHolder ResolveWeaponsHolder()
    {
        return player != null && player.TryGetComponent(out WeaponsHolder resolvedWeaponsHolder)
            ? resolvedWeaponsHolder
            : UnityEngine.Object.FindFirstObjectByType<WeaponsHolder>();
    }

    private void PublishViewState(ShopRefreshReason reason = ShopRefreshReason.StateUpdate)
    {
        if (isExecutingShopOperation)
        {
            hasDeferredViewState = true;
            deferredViewStateReason = reason;
            return;
        }

        int attributeFreeRerollCount = ResolvePlayerFreeRerollCount();
        int freeRerollCount = board.VisitFreeRerollCount + attributeFreeRerollCount;
        int rerollCost = ResolveCurrentRerollCost();
        bool canReroll = !board.IsRerollBlocked && (currentCurrency >= rerollCost || freeRerollCount > 0);
        ViewStateChanged?.Invoke(new ShopViewState(
            CreateOfferViewData(),
            rerollCost,
            freeRerollCount,
            canReroll,
            board.IsRerollBlocked,
            reason));
    }

    private ShopOfferViewData[] CreateOfferViewData()
    {
        IReadOnlyList<ShopOfferState> offers = board.Offers;
        if (offers.Count == 0)
        {
            return Array.Empty<ShopOfferViewData>();
        }

        float runPriceMultiplier = ResolveRunPriceMultiplier();
        float playerDiscountMultiplier = ResolvePlayerDiscountMultiplier();
        ShopOfferViewData[] viewData = new ShopOfferViewData[offers.Count];
        for (int i = 0; i < offers.Count; i++)
        {
            ShopOfferState offer = offers[i];
            int originalPrice = ShopPricingService.GetPrice(offer.Product, runPriceMultiplier, playerDiscountMultiplier, 1f);
            viewData[i] = offer.CreateViewData(ResolveOfferPrice(offer), originalPrice);
        }

        return viewData;
    }

    private int ResolveOfferPrice(ShopOfferState offer)
    {
        int basePrice = ResolveBaseOfferPrice(offer);
        return board.ApplyVisitPriceModifiers(basePrice);
    }

    private int ResolveBaseOfferPrice(ShopOfferState offer)
    {
        return ShopPricingService.GetPrice(
            offer?.Product,
            ResolveRunPriceMultiplier(),
            ResolvePlayerDiscountMultiplier(),
            offer != null ? offer.StatePriceMultiplier : 1f);
    }

    private int ResolveOfferCount()
    {
        return Mathf.Max(1, containersToAdd);
    }

    private void TryBindWallet()
    {
        if (player == null)
        {
            player = UnityEngine.Object.FindFirstObjectByType<Player>();
        }

        CurrencyWallet resolvedWallet = player != null ? player.GetComponent<CurrencyWallet>() : currencyWallet;
        if (player != null && AttributeManager == null)
        {
            BindAttributeManager(player.GetComponent<AttributeManager>());
        }

        if (resolvedWallet != null)
        {
            BindCurrencyWallet(resolvedWallet);
        }
    }

    private void BindCurrencyWallet(CurrencyWallet newCurrencyWallet)
    {
        UnbindCurrencyWallet();
        currencyWallet = newCurrencyWallet;
        if (currencyWallet != null)
        {
            currencyWallet.OnAmountChanged += OnCurrencyAmountChanged;
            currentCurrency = currencyWallet.CurrentAmount;
        }
        else
        {
            currentCurrency = 0;
        }

        if (board.Offers.Count > 0)
        {
            PublishViewState(ShopRefreshReason.StateUpdate);
        }
    }

    private void UnbindCurrencyWallet()
    {
        if (currencyWallet != null)
        {
            currencyWallet.OnAmountChanged -= OnCurrencyAmountChanged;
            currencyWallet = null;
        }
    }

    private void OnShopFreeRerollsGranted(ShopFreeRerollsGrantedEvent eventData)
    {
        if (IsEventForCurrentPlayer(eventData.Player) && eventData.Count > 0)
        {
            AddAttributeFreeRerollModifier(FREE_REROLL_GRANT_SOURCE_PREFIX, eventData.Count);
            PublishViewState(ShopRefreshReason.StateUpdate);
        }
    }

    private bool TryConsumeAttributeFreeReroll()
    {
        if (ResolvePlayerFreeRerollCount() <= 0)
        {
            return false;
        }

        AddAttributeFreeRerollModifier(FREE_REROLL_CONSUMPTION_SOURCE_PREFIX, -1);
        return true;
    }

    private void AddAttributeFreeRerollModifier(string sourcePrefix, int value)
    {
        if (AttributeManager == null || value == 0)
        {
            return;
        }

        AttributeManager.AddModifier(
            $"{sourcePrefix}:{Guid.NewGuid():N}",
            new PropModifierData(PropType.ShopFreeRerollCount, value));
    }

    private bool IsEventForCurrentPlayer(Player eventPlayer)
    {
        return eventPlayer == null || player == null || player == eventPlayer;
    }

    private float ResolvePlayerDiscountMultiplier()
    {
        float discount = AttributeManager != null
            ? PropValueUtility.PercentPointsToEffectiveRatio(
                PropType.ShopPriceDiscount,
                AttributeManager.GetAttributeValue(PropType.ShopPriceDiscount))
            : 0f;
        return Mathf.Max(PropValueUtility.MIN_EFFECTIVE_SHOP_PRICE_MULTIPLIER, 1f - discount);
    }

    private float ResolvePlayerLuck()
    {
        return AttributeManager != null ? AttributeManager.GetAttributeValue(PropType.Luck) : 0f;
    }

    private int ResolvePlayerFreeRerollCount()
    {
        return AttributeManager != null
            ? PropValueUtility.FloatPointsToNonNegativeFlooredInt(
                AttributeManager.GetAttributeValue(PropType.ShopFreeRerollCount))
            : 0;
    }

    private static float ResolveRunPriceMultiplier()
    {
        RunProgressionSnapshot snapshot = RunProgressionRuntime.CurrentSnapshot;
        return snapshot.ShopPriceMultiplier > 0f ? snapshot.ShopPriceMultiplier : 1f;
    }

    private int ResolveCurrentRerollCost()
    {
        float currentCost = RunProgressionRuntime.CurrentSnapshot.ShopRerollBasePrice +
                            board.PaidRerollCountThisVisit * ResolveCurrentWaveRerollStepCost();
        return Mathf.Max(0, Mathf.RoundToInt(currentCost));
    }

    private static float ResolveCurrentWaveRerollStepCost()
    {
        RunProgressionSnapshot snapshot = RunProgressionRuntime.CurrentSnapshot;
        return snapshot.ShopRerollStepPrice > 0f ? snapshot.ShopRerollStepPrice : DEFAULT_REROLL_STEP_COST;
    }

    private void BindAttributeManager(AttributeManager newAttributeManager)
    {
        if (AttributeManager == newAttributeManager)
        {
            return;
        }

        UnbindAttributeManager();
        AttributeManager = newAttributeManager;
        if (AttributeManager != null)
        {
            AttributeManager.SubscribeAttributeChanged(PropType.ShopPriceDiscount, OnShopAttributeChanged);
            AttributeManager.SubscribeAttributeChanged(PropType.ShopFreeRerollCount, OnShopAttributeChanged);
        }
    }

    private void UnbindAttributeManager()
    {
        if (AttributeManager != null)
        {
            AttributeManager.UnsubscribeAttributeChanged(PropType.ShopPriceDiscount, OnShopAttributeChanged);
            AttributeManager.UnsubscribeAttributeChanged(PropType.ShopFreeRerollCount, OnShopAttributeChanged);
            AttributeManager = null;
        }
    }

    private void OnShopAttributeChanged(int value)
    {
        PublishViewState(ShopRefreshReason.StateUpdate);
    }

    private void RefreshCurrency()
    {
        currentCurrency = currencyWallet != null ? currencyWallet.CurrentAmount : 0;
    }

    private void NotifyPurchaseSucceeded(ShopOfferState offer, int price)
    {
        ShopOfferSnapshot snapshot = offer.CreateSnapshot();
        PurchaseSucceeded?.Invoke(new ShopPurchaseSuccess(snapshot));
        YokiFrame.EventKit.Type.Send(new ShopItemPurchasedEvent(player, snapshot, price));
    }

    private void NotifyPurchaseFailed(string message)
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.ShopPurchaseFailed);
        PurchaseFailed?.Invoke(new ShopPurchaseFailure(message));
    }

    private void NotifyShopRerolled(bool usedFreeReroll)
    {
        YokiFrame.EventKit.Type.Send(new ShopRerolledEvent(player, usedFreeReroll));
    }

    private void NotifyItemLocked(ShopOfferState offer)
    {
        YokiFrame.EventKit.Type.Send(new ShopItemLockedEvent(player, offer.CreateSnapshot()));
    }

    private void ExecuteShopOperation(Action operation)
    {
        isExecutingShopOperation = true;
        try
        {
            operation.Invoke();
        }
        finally
        {
            if (board.CompleteOperation())
            {
                NotifyStageChanged();
            }

            isExecutingShopOperation = false;
            FlushDeferredViewState();
        }
    }

    private void FlushDeferredViewState()
    {
        if (hasDeferredViewState)
        {
            ShopRefreshReason reason = deferredViewStateReason;
            hasDeferredViewState = false;
            PublishViewState(reason);
        }
    }

    private void NotifyStageChanged()
    {
        StageChanged?.Invoke();
    }
}
