using System;
using Orange.GameServices;
using UnityEngine;

/// <summary>
/// 局内商店服务：负责商品刷新、购买、锁定与商店视图状态发布。
/// </summary>
[Serializable]
public sealed class ShopService : GameService, IShopController
{
    private const int DEFAULT_CONTAINERS_TO_ADD = 4;
    private const int DEFAULT_REROLL_STEP_COST = 1;

    [SerializeField] private int containersToAdd = DEFAULT_CONTAINERS_TO_ADD;
    [SerializeField] private CurrencyWallet currencyWallet;

    private readonly ShopExtractionRoller extractionRoller = new();
    private ShopItemData[] currentItems;
    private Player player;
    private PropertiesManager propertiesManager;
    private int freeShopRerolls;
    private int totalRerollCount;
    private int paidRerollCountThisWave;
    private int shopRefreshCount;
    private int currentCurrency;

    private UnityEngine.Object LogContext => Context != null ? Context.Root : null;

    public event Action<ShopViewState> ViewStateChanged;
    public event Action<ShopPurchaseSuccess> PurchaseSucceeded;
    public event Action<ShopPurchaseFailure> PurchaseFailed;

    protected override void DeclareDependencies(GameServiceDependencyBuilder dependencies)
    {
        dependencies.Require<IGameContentProvider>();
    }

    protected override void RegisterContracts(GameServiceRegistry registry)
    {
        registry.Register<IShopController>(this);
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
        GenerateShopItems();
        PublishViewState(ShopRefreshReason.Initial);
    }

    protected override void OnDispose()
    {
        UnbindCurrencyWallet();
        UnbindPropertiesManager();
        ViewStateChanged = null;
        PurchaseSucceeded = null;
        PurchaseFailed = null;
    }

    private void OnPlayerSpawned(PlayerSpawnedEvent eventData)
    {
        player = eventData.Player;
        BindPropertiesManager(player != null ? player.GetComponent<PropertiesManager>() : null);
        BindCurrencyWallet(player != null ? player.GetComponent<CurrencyWallet>() : null);
    }

    private void OnGameStateChanged(GameStateChangedEvent eventData)
    {
        if (eventData.NewState != GameState.Shop || eventData.OldState == GameState.Shop)
        {
            return;
        }

        RefreshShopForWaveEntry();
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

    public void RequestBuyItem(int itemIndex)
    {
        if (currentItems == null || itemIndex < 0 || itemIndex >= currentItems.Length)
        {
            NotifyPurchaseFailed("Invalid item index.");
            return;
        }

        ApplyShopPriceMultiplier();
        ShopItemData itemData = currentItems[itemIndex];
        if (itemData.ItemData == null)
        {
            NotifyPurchaseFailed("Item data is null.");
            return;
        }

        if (itemData.SoldOut)
        {
            NotifyPurchaseFailed("Item already sold out.");
            return;
        }

        if (itemData.ItemData.ItemType == ItemType.Accessory)
        {
            ProcessAccessoryPurchase(itemData, itemIndex);
        }
        else if (itemData.ItemData.ItemType == ItemType.Weapon)
        {
            ProcessWeaponPurchase(itemData, itemIndex);
        }
    }

    private void ProcessAccessoryPurchase(ShopItemData itemData, int itemIndex)
    {
        AccessoryDataSO accessoryData = itemData.ItemData as AccessoryDataSO;
        if (accessoryData == null)
        {
            NotifyPurchaseFailed("Accessory data is null or wrong type.");
            return;
        }

        int price = itemData.GetPrice();
        if (currentCurrency < price)
        {
            NotifyPurchaseFailed("Not enough currency.");
            return;
        }

        AccessoryManager playerAccessoryManager = ResolveAccessoryManager();
        if (playerAccessoryManager == null)
        {
            NotifyPurchaseFailed("Accessory manager not found.");
            return;
        }

        if (!playerAccessoryManager.EquipAccessory(accessoryData, false))
        {
            NotifyPurchaseFailed("Accessory owned limit reached.");
            return;
        }

        MarkItemAsSoldOut(itemIndex);

        if (currencyWallet != null)
        {
            currencyWallet.ChangeAmount(-price);
        }
        else
        {
            PublishViewState(ShopRefreshReason.Purchase);
        }

        AudioSfxBridge.RequestPlay(AudioSfxKey.ShopPurchaseSucceeded);
        NotifyPurchaseSucceeded(itemData.ItemData, itemData.Level);
    }

    private void ProcessWeaponPurchase(ShopItemData itemData, int itemIndex)
    {
        WeaponDataSO weaponData = itemData.ItemData as WeaponDataSO;
        if (weaponData == null)
        {
            NotifyPurchaseFailed("Weapon data is null or wrong type.");
            return;
        }

        int price = itemData.GetPrice();
        if (currentCurrency < price)
        {
            NotifyPurchaseFailed("Not enough currency.");
            return;
        }

        WeaponsHolder weaponsHolder = ResolveWeaponsHolder();
        if (weaponsHolder == null)
        {
            NotifyPurchaseFailed("Weapons holder not found.");
            return;
        }

        if (!weaponsHolder.AddWeapon(weaponData, itemData.Level, false))
        {
            NotifyPurchaseFailed("No empty weapon slot available.");
            return;
        }

        MarkItemAsSoldOut(itemIndex);

        if (currencyWallet != null)
        {
            currencyWallet.ChangeAmount(-price);
        }
        else
        {
            PublishViewState(ShopRefreshReason.Purchase);
        }

        AudioSfxBridge.RequestPlay(AudioSfxKey.ShopPurchaseSucceeded);
        NotifyPurchaseSucceeded(itemData.ItemData, itemData.Level);
    }

    private void MarkItemAsSoldOut(int index)
    {
        if (currentItems == null || index < 0 || index >= currentItems.Length)
        {
            return;
        }

        currentItems[index].SoldOut = true;
        currentItems[index].Lock = false;
    }

    public void RequestReroll()
    {
        if (TryConsumeFreeShopReroll())
        {
            RerollShopItems(trackAsPaidReroll: false);
            AudioSfxBridge.RequestPlay(AudioSfxKey.ShopRerolled);
            PublishViewState(ShopRefreshReason.Reroll);
            return;
        }

        int currentRerollCost = ResolveCurrentRerollCost();
        if (currentCurrency < currentRerollCost)
        {
            NotifyPurchaseFailed($"Not enough currency for reroll. Cost: {currentRerollCost}");
            return;
        }

        currencyWallet?.ChangeAmount(-currentRerollCost);
        RerollShopItems(trackAsPaidReroll: true);
        AudioSfxBridge.RequestPlay(AudioSfxKey.ShopRerolled);
        PublishViewState(ShopRefreshReason.Reroll);
    }

    private void RefreshShopForWaveEntry()
    {
        paidRerollCountThisWave = 0;
        if (currentItems == null || currentItems.Length == 0)
        {
            GenerateShopItems();
            PublishViewState(ShopRefreshReason.WaveRefresh);
            return;
        }

        RefreshKeepingLockedItems();
        PublishViewState(ShopRefreshReason.WaveRefresh);
    }

    private void RefreshKeepingLockedItems()
    {
        shopRefreshCount++;
        int count = Mathf.Max(1, containersToAdd);
        ShopItemData[] nextItems = new ShopItemData[count];
        int writeIndex = 0;

        for (int i = 0; i < currentItems.Length && writeIndex < count; i++)
        {
            if (!currentItems[i].Lock || currentItems[i].SoldOut || currentItems[i].ItemData == null)
            {
                continue;
            }

            nextItems[writeIndex++] = currentItems[i];
        }

        while (writeIndex < count)
        {
            nextItems[writeIndex] = GenerateRandomShopItem(nextItems, writeIndex);
            writeIndex++;
        }

        currentItems = nextItems;
    }

    private void RerollShopItems(bool trackAsPaidReroll)
    {
        totalRerollCount++;
        if (trackAsPaidReroll)
        {
            paidRerollCountThisWave++;
        }

        shopRefreshCount++;

        int count = Mathf.Max(1, containersToAdd);
        ShopItemData[] nextItems = new ShopItemData[count];
        int writeIndex = 0;

        if (currentItems != null)
        {
            for (int i = 0; i < currentItems.Length && writeIndex < count; i++)
            {
                if (currentItems[i].Lock && !currentItems[i].SoldOut && currentItems[i].ItemData != null)
                {
                    nextItems[writeIndex++] = currentItems[i];
                }
            }
        }

        while (writeIndex < count)
        {
            nextItems[writeIndex] = GenerateRandomShopItem(nextItems, writeIndex);
            writeIndex++;
        }

        currentItems = nextItems;
    }

    private void GenerateShopItems()
    {
        shopRefreshCount++;
        int count = Mathf.Max(1, containersToAdd);
        currentItems = new ShopItemData[count];

        for (int i = 0; i < count; i++)
        {
            currentItems[i] = GenerateRandomShopItem(currentItems, i);
        }
    }

    private ShopItemData GenerateRandomShopItem(ShopItemData[] existingItems, int existingCount)
    {
        for (int attempt = 0; attempt < 8; attempt++)
        {
            ShopItemData item = RollShopItem();
            if (!ContainsDuplicate(existingItems, existingCount, item))
            {
                return item;
            }
        }

        return RollShopItem();
    }

    private bool ContainsDuplicate(ShopItemData[] existingItems, int existingCount, ShopItemData item)
    {
        if (existingItems == null || item.ItemData == null)
        {
            return false;
        }

        for (int i = 0; i < existingCount; i++)
        {
            if (existingItems[i].ItemData == null)
            {
                continue;
            }

            if (IsSameShopItem(existingItems[i], item))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsSameShopItem(ShopItemData a, ShopItemData b)
    {
        return a.ItemData == b.ItemData && a.Level == b.Level;
    }

    private ShopItemData RollShopItem()
    {
        if (!GameContentRuntime.TryGetProvider(out IGameContentProvider provider))
        {
            Debug.LogError($"[{nameof(ShopService)}] Missing {nameof(IGameContentProvider)}. Cannot roll shop item.", LogContext);
            return default;
        }

        ShopExtractionContext context = new(ResolveAccessoryManager(), ResolvePlayerLuck());
        if (provider.ContentTierWeightProfile == null)
        {
            Debug.LogError(
                $"[{nameof(ShopService)}] Missing {nameof(ContentTierWeightProfileSO)} in {nameof(GameContentCatalogSO)}.",
                LogContext);
            return default;
        }

        if (!extractionRoller.TryRollOne(
                provider.Weapons,
                provider.Accessories,
                provider.ContentTierWeightProfile,
                context,
                out ShopExtractionCandidate candidate))
        {
            Debug.LogWarning(
                $"[{nameof(ShopService)}] No shop item could be rolled from configured weapon/accessory candidates.",
                LogContext);
            return default;
        }

        return CreateShopItemData(candidate);
    }

    private ShopItemData CreateShopItemData(ShopExtractionCandidate candidate)
    {
        if (candidate?.ItemData == null)
        {
            return default;
        }

        return new ShopItemData
        {
            ItemData = candidate.ItemData,
            Level = candidate.Level,
            Lock = false,
            SoldOut = false,
            RunPriceMultiplier = ResolveRunPriceMultiplier(),
            PlayerDiscountMultiplier = ResolvePlayerDiscountMultiplier()
        };
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
        if (currentItems == null)
        {
            currentItems = Array.Empty<ShopItemData>();
        }

        ApplyShopPriceMultiplier();
        int currentRerollCost = ResolveCurrentRerollCost();
        bool canReroll = currentCurrency >= currentRerollCost || freeShopRerolls > 0;
        ViewStateChanged?.Invoke(new ShopViewState(currentItems, currentRerollCost, canReroll, reason));
    }

    private void ApplyShopPriceMultiplier()
    {
        float runPriceMultiplier = ResolveRunPriceMultiplier();
        float playerDiscountMultiplier = ResolvePlayerDiscountMultiplier();
        for (int i = 0; i < currentItems.Length; i++)
        {
            currentItems[i].RunPriceMultiplier = runPriceMultiplier;
            currentItems[i].PlayerDiscountMultiplier = playerDiscountMultiplier;
        }
    }

    public void RequestToggleLock(int itemIndex)
    {
        if (currentItems == null || itemIndex < 0 || itemIndex >= currentItems.Length)
        {
            return;
        }

        if (currentItems[itemIndex].SoldOut)
        {
            return;
        }

        currentItems[itemIndex].Lock = !currentItems[itemIndex].Lock;
        Debug.Log(
            $"物品:{currentItems[itemIndex].ItemData.ItemName} 锁定状态:{currentItems[itemIndex].Lock}",
            LogContext);
        PublishViewState(ShopRefreshReason.StateUpdate);
    }

    private void TryBindWallet()
    {
        if (player == null)
        {
            player = UnityEngine.Object.FindFirstObjectByType<Player>();
        }

        CurrencyWallet resolvedWallet = null;
        if (player != null)
        {
            resolvedWallet = player.GetComponent<CurrencyWallet>();
        }

        if (resolvedWallet == null)
        {
            resolvedWallet = currencyWallet;
        }

        if (player != null && propertiesManager == null)
        {
            BindPropertiesManager(player.GetComponent<PropertiesManager>());
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

        if (currentItems != null)
        {
            PublishViewState(ShopRefreshReason.StateUpdate);
        }
    }

    private void UnbindCurrencyWallet()
    {
        if (currencyWallet == null)
        {
            return;
        }

        currencyWallet.OnAmountChanged -= OnCurrencyAmountChanged;
        currencyWallet = null;
    }

    private void OnShopFreeRerollsGranted(ShopFreeRerollsGrantedEvent eventData)
    {
        if (!IsEventForCurrentPlayer(eventData.Player))
        {
            return;
        }

        int count = Mathf.Max(0, eventData.Count);
        if (count <= 0)
        {
            return;
        }

        freeShopRerolls += count;
        PublishViewState(ShopRefreshReason.StateUpdate);
    }

    private bool TryConsumeFreeShopReroll()
    {
        if (freeShopRerolls <= 0)
        {
            return false;
        }

        freeShopRerolls--;
        return true;
    }

    private bool IsEventForCurrentPlayer(Player eventPlayer)
    {
        if (eventPlayer == null)
        {
            return true;
        }

        return player == null || player == eventPlayer;
    }

    private float ResolvePlayerDiscountMultiplier()
    {
        float discount = propertiesManager != null
            ? PropValueUtility.PercentPointsToEffectiveRatio(
                PropType.ShopPriceDiscount,
                propertiesManager.GetPropValue(PropType.ShopPriceDiscount))
            : 0f;
        return Mathf.Max(PropValueUtility.MIN_EFFECTIVE_SHOP_PRICE_MULTIPLIER, 1f - discount);
    }

    private float ResolvePlayerLuck()
    {
        return propertiesManager != null
            ? propertiesManager.GetPropValue(PropType.Luck)
            : 0f;
    }

    private static float ResolveRunPriceMultiplier()
    {
        RunProgressionSnapshot snapshot = RunProgressionRuntime.CurrentSnapshot;
        return snapshot.ShopPriceMultiplier > 0f ? snapshot.ShopPriceMultiplier : 1f;
    }

    private int ResolveCurrentRerollCost()
    {
        float baseCost = RunProgressionRuntime.CurrentSnapshot.ShopRerollBasePrice;
        float stepCost = ResolveCurrentWaveRerollStepCost();
        float currentCost = baseCost + (paidRerollCountThisWave * stepCost);
        return Mathf.Max(0, Mathf.RoundToInt(currentCost));
    }

    private static float ResolveCurrentWaveRerollStepCost()
    {
        RunProgressionSnapshot snapshot = RunProgressionRuntime.CurrentSnapshot;
        if (snapshot.ShopRerollStepPrice > 0f)
        {
            return snapshot.ShopRerollStepPrice;
        }

        return DEFAULT_REROLL_STEP_COST;
    }

    private void BindPropertiesManager(PropertiesManager newPropertiesManager)
    {
        if (propertiesManager == newPropertiesManager)
        {
            return;
        }

        UnbindPropertiesManager();
        propertiesManager = newPropertiesManager;
        if (propertiesManager != null)
        {
            propertiesManager.OnPropertyChanged += OnPlayerPropertyChanged;
        }
    }

    private void UnbindPropertiesManager()
    {
        if (propertiesManager != null)
        {
            propertiesManager.OnPropertyChanged -= OnPlayerPropertyChanged;
            propertiesManager = null;
        }
    }

    private void OnPlayerPropertyChanged(PropType propType, float value)
    {
        if (propType == PropType.ShopPriceDiscount)
        {
            PublishViewState(ShopRefreshReason.StateUpdate);
        }
    }

    private void RefreshCurrency()
    {
        currentCurrency = currencyWallet != null ? currencyWallet.CurrentAmount : 0;
    }

    private void NotifyPurchaseSucceeded(ItemDataSO itemData, int level)
    {
        PurchaseSucceeded?.Invoke(new ShopPurchaseSuccess(itemData, level));
    }

    private void NotifyPurchaseFailed(string message)
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.ShopPurchaseFailed);
        PurchaseFailed?.Invoke(new ShopPurchaseFailure(message));
    }
}
