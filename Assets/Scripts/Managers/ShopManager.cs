using System;
using UnityEngine;

public struct ShopItemData : IHasContentTier
{
    public ItemDataSO ItemData;
    public int Level;
    public bool Lock;
    public bool SoldOut;
    public float ContentPriceMultiplier;
    public float RunPriceMultiplier;
    public float PlayerDiscountMultiplier;
    public ContentRollItem RollItem;
    public ContentTier Tier => ResolveTier();

    public float PriceMultiplier
    {
        get => PlayerDiscountMultiplier;
        set => PlayerDiscountMultiplier = value;
    }

    public int GetPrice()
    {
        return ShopPricingService.GetPrice(
            ItemData,
            Level,
            ContentPriceMultiplier,
            RunPriceMultiplier,
            PlayerDiscountMultiplier);
    }

    private ContentTier ResolveTier()
    {
        if (ItemData == null)
        {
            return RollItem.TryGetTier(out ContentTier rollTier) ? rollTier : ContentTier.Common;
        }

        if (ItemData.ItemType == ItemType.Weapon)
        {
            return ContentTierResolver.FromWeaponLevel(Level);
        }

        if (ItemData is AccessoryDataSO accessoryData)
        {
            return accessoryData.Tier;
        }

        return RollItem.TryGetTier(out ContentTier tier) ? tier : ContentTier.Common;
    }
}

public class ShopManager : MonoBehaviour
{
    private const int DEFAULT_CONTAINERS_TO_ADD = 4;
    private const int DEFAULT_REROLL_STEP_COST = 1;

    [SerializeField] private int containersToAdd = DEFAULT_CONTAINERS_TO_ADD;
    [SerializeField] private CurrencyWallet currencyWallet;
    [SerializeField] private ContentPoolSO shopPool;

    private readonly ShopContentRoller contentRoller = new();
    private ShopItemData[] currentItems;
    private Player player;
    private PropertiesManager propertiesManager;
    private int freeShopRerolls;
    private int totalRerollCount;
    private int paidRerollCountThisWave;
    private int shopRefreshCount;
    private int currentWaveNumber = 1;
    private int currentCurrency;

    public event Action<ShopViewState> ViewStateChanged;
    public event Action<ShopPurchaseSuccess> PurchaseSucceeded;
    public event Action<ShopPurchaseFailure> PurchaseFailed;

    private void OnEnable()
    {
        GameEventBus.Subscribe<PlayerSpawnedEvent>(OnPlayerSpawned);
        GameEventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        GameEventBus.Subscribe<ShopFreeRerollsGrantedEvent>(OnShopFreeRerollsGranted);
        GameEventBus.Subscribe<WaveStartedEvent>(OnWaveStarted);
        GameEventBus.Subscribe<WaveRuntimeChangedEvent>(OnWaveRuntimeChanged);

        TryBindWallet();
        RefreshCurrency();
    }

    private void OnDisable()
    {
        UnbindCurrencyWallet();
        UnbindPropertiesManager();

        GameEventBus.Unsubscribe<PlayerSpawnedEvent>(OnPlayerSpawned);
        GameEventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        GameEventBus.Unsubscribe<ShopFreeRerollsGrantedEvent>(OnShopFreeRerollsGranted);
        GameEventBus.Unsubscribe<WaveStartedEvent>(OnWaveStarted);
        GameEventBus.Unsubscribe<WaveRuntimeChangedEvent>(OnWaveRuntimeChanged);
    }

    private void Start()
    {
        GenerateShopItems();
        PublishViewState(ShopRefreshReason.Initial);
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

        AccessoryManager playerAccessoryManager = FindFirstObjectByType<AccessoryManager>();
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

        RecordShopPick(itemData);

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

        WeaponsHolder weaponsHolder = FindFirstObjectByType<WeaponsHolder>();
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

        RecordShopPick(itemData);

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

    private bool IsSameShopItem(ShopItemData a, ShopItemData b)
    {
        return a.ItemData == b.ItemData && a.Level == b.Level;
    }

    private ShopItemData RollShopItem()
    {
        ContentPoolSO pool = ResolveShopPool();
        if (pool == null)
        {
            Debug.LogError($"[ShopManager] Missing shop content pool in scene or {nameof(GameContentCatalogSO)}.", this);
            return default;
        }

        ContentRollItem rollItem = contentRoller.RollItem(
            pool,
            player,
            shopRefreshCount,
            totalRerollCount,
            RunContentHistoryRuntime.Current);
        if (rollItem.Content == null)
        {
            Debug.LogWarning("[ShopManager] No shop item could be rolled from content pool.", this);
            return default;
        }

        return CreateShopItemData(rollItem);
    }

    private ShopItemData CreateShopItemData(ContentRollItem rollItem)
    {
        ItemDataSO itemData = rollItem.Content as ItemDataSO;
        if (itemData == null)
        {
            return default;
        }

        return new ShopItemData
        {
            ItemData = itemData,
            Level = ResolveShopItemLevel(itemData, rollItem),
            Lock = false,
            SoldOut = false,
            ContentPriceMultiplier = ResolveShopPriceMultiplier(rollItem),
            RunPriceMultiplier = ResolveRunPriceMultiplier(),
            PlayerDiscountMultiplier = ResolvePlayerDiscountMultiplier(),
            RollItem = rollItem
        };
    }

    private void RecordShopPick(ShopItemData itemData)
    {
        if (itemData.ItemData == null)
        {
            return;
        }

        contentRoller.RecordPick(ResolveShopPool(), player, RunContentHistoryRuntime.Current, itemData.RollItem);
    }

    private static int ResolveShopItemLevel(ItemDataSO itemData, ContentRollItem rollItem)
    {
        if (itemData == null || itemData.ItemType != ItemType.Weapon)
        {
            return WeaponLevelHelper.MinLevel;
        }

        int minLevel = WeaponLevelHelper.MinLevel;
        int maxLevel = WeaponLevelHelper.MaxLevel;
        if (rollItem.TryGetMetadata(out WeaponLevelRollMetadata levelMetadata))
        {
            minLevel = levelMetadata.MinLevel;
            maxLevel = levelMetadata.MaxLevel;
        }

        if (maxLevel < minLevel)
        {
            maxLevel = minLevel;
        }

        return UnityEngine.Random.Range(minLevel, maxLevel + 1);
    }

    private static float ResolveShopPriceMultiplier(ContentRollItem rollItem)
    {
        return rollItem.TryGetMetadata(out ShopPricingMetadata pricingMetadata)
            ? pricingMetadata.PriceMultiplier
            : 1f;
    }

    private ContentPoolSO ResolveShopPool()
    {
        if (shopPool != null)
        {
            return shopPool;
        }

        if (!GameContentRuntime.TryGetProvider(out IGameContentProvider provider))
        {
            return null;
        }

        return provider.ShopPool;
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
        print($"物品:{currentItems[itemIndex].ItemData.ItemName} 锁定状态:{currentItems[itemIndex].Lock}");
        PublishViewState(ShopRefreshReason.StateUpdate);
    }

    private void TryBindWallet()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<Player>();
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

    private float ResolveRunPriceMultiplier()
    {
        RunProgressionSnapshot snapshot = RunProgressionRuntime.CurrentSnapshot;
        return snapshot.ShopPriceMultiplier > 0f ? snapshot.ShopPriceMultiplier : 1f;
    }

    private int ResolveCurrentRerollCost()
    {
        // 刷新基础价由局内推进快照统一提供，不再保留 ShopManager 本地兜底字段。
        float baseCost = RunProgressionRuntime.CurrentSnapshot.ShopRerollBasePrice;
        float stepCost = ResolveCurrentWaveRerollStepCost();
        float currentCost = baseCost + (paidRerollCountThisWave * stepCost);
        return Mathf.Max(0, Mathf.RoundToInt(currentCost));
    }

    private float ResolveCurrentWaveRerollStepCost()
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

    private void OnWaveStarted(WaveStartedEvent eventData)
    {
        currentWaveNumber = Mathf.Max(1, eventData.CurrentWave);
    }

    private void OnWaveRuntimeChanged(WaveRuntimeChangedEvent eventData)
    {
        if (eventData.CurrentWave > 0)
        {
            currentWaveNumber = eventData.CurrentWave;
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
