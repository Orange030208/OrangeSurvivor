using System;
using System.Collections.Generic;
using UnityEngine;

public struct ShopItemData
{
    public ItemDataSO ItemData;
    public int Level;
    public bool Lock;
    public float ContentPriceMultiplier;
    public float PriceMultiplier;

    public int GetPrice()
    {
        if (ItemData == null)
        {
            return 0;
        }

        if (ItemData.ItemType == ItemType.Weapon)
        {
            return ApplyPriceMultiplier(WeaponPriceHelper.GetPrice(ItemData.ItemPrice, Level));
        }

        return ApplyPriceMultiplier(ItemData.ItemPrice);
    }

    private int ApplyPriceMultiplier(int basePrice)
    {
        float contentMultiplier = ContentPriceMultiplier > 0f ? ContentPriceMultiplier : 1f;
        float shopMultiplier = PriceMultiplier > 0f ? PriceMultiplier : 1f;
        float multiplier = contentMultiplier * shopMultiplier;
        return Mathf.Max(0, Mathf.RoundToInt(basePrice * multiplier));
    }
}

public class ShopManager : MonoBehaviour
{
    private const int DEFAULT_CONTAINERS_TO_ADD = 4;
    private const int BASE_REROLL_COST = 5;

    [SerializeField] private int containersToAdd = DEFAULT_CONTAINERS_TO_ADD;
    [SerializeField] private int baseRerollCost = BASE_REROLL_COST;
    [SerializeField] private CurrencyWallet currencyWallet;
    [SerializeField] private ContentPoolSO shopPool;

    private readonly ContentPoolRollService contentPoolRollService = new();
    private readonly ContentPoolRuntimeState shopRuntimeState = new();
    private ShopItemData[] currentItems;
    private Player player;
    private PropertiesManager propertiesManager;
    private int freeShopRerolls;
    private int rerollCost;
    private int rerollCount;
    private int shopRefreshCount;
    private int currentWaveNumber = 1;
    private int currentCurrency;

    public event Action<ShopViewState> ViewStateChanged;
    public event Action<ShopPurchaseSuccess> PurchaseSucceeded;
    public event Action<ShopPurchaseFailure> PurchaseFailed;

    private void Awake()
    {
        rerollCost = baseRerollCost;
    }

    private void OnEnable()
    {
        GameEventBus.Subscribe<ShopVideoAdRerollRequestedEvent>(OnVideoAdRerollRequested);
        GameEventBus.Subscribe<CurrencyChangedEvent>(OnCurrencyChanged);
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
        UnbindPropertiesManager();

        GameEventBus.Unsubscribe<ShopVideoAdRerollRequestedEvent>(OnVideoAdRerollRequested);
        GameEventBus.Unsubscribe<CurrencyChangedEvent>(OnCurrencyChanged);
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
        currencyWallet = player != null ? player.GetComponent<CurrencyWallet>() : null;
        BindPropertiesManager(player != null ? player.GetComponent<PropertiesManager>() : null);
        RefreshCurrency();
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

    private void OnCurrencyChanged(CurrencyChangedEvent eventData)
    {
        if (eventData.Wallet != currencyWallet)
        {
            return;
        }

        currentCurrency = eventData.CurrentAmount;
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

        currencyWallet?.ChangeAmount(-price);
        playerAccessoryManager.EquipAccessory(accessoryData, false);

        AudioSfxBridge.RequestPlay(AudioSfxKey.ShopPurchaseSucceeded);
        NotifyPurchaseSucceeded(itemData.ItemData, itemData.Level);
        RemoveItemFromShop(itemIndex);
        PublishViewState(ShopRefreshReason.Purchase);
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

        currencyWallet?.ChangeAmount(-price);

        AudioSfxBridge.RequestPlay(AudioSfxKey.ShopPurchaseSucceeded);
        NotifyPurchaseSucceeded(itemData.ItemData, itemData.Level);
        RemoveItemFromShop(itemIndex);
        PublishViewState(ShopRefreshReason.Purchase);
    }

    private void RemoveItemFromShop(int index)
    {
        if (currentItems == null || index < 0 || index >= currentItems.Length)
        {
            return;
        }

        ShopItemData[] nextItems = new ShopItemData[Mathf.Max(0, currentItems.Length - 1)];
        int writeIndex = 0;
        for (int i = 0; i < currentItems.Length; i++)
        {
            if (i == index)
            {
                continue;
            }

            nextItems[writeIndex++] = currentItems[i];
        }

        currentItems = nextItems;
    }

    public void RequestReroll()
    {
        if (TryConsumeFreeShopReroll())
        {
            RerollShopItems();
            AudioSfxBridge.RequestPlay(AudioSfxKey.ShopRerolled);
            PublishViewState(ShopRefreshReason.Reroll);
            return;
        }

        if (currentCurrency < rerollCost)
        {
            NotifyPurchaseFailed($"Not enough currency for reroll. Cost: {rerollCost}");
            return;
        }

        currencyWallet?.ChangeAmount(-rerollCost);
        RerollShopItems();
        AudioSfxBridge.RequestPlay(AudioSfxKey.ShopRerolled);
        PublishViewState(ShopRefreshReason.Reroll);
    }

    private void OnVideoAdRerollRequested()
    {
        Debug.Log("Video ad reroll requested - implement ad integration here.");
        RerollShopItems();
        AudioSfxBridge.RequestPlay(AudioSfxKey.ShopRerolled);
        PublishViewState(ShopRefreshReason.Reroll);
    }

    private void RefreshShopForWaveEntry()
    {
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
            if (!currentItems[i].Lock || currentItems[i].ItemData == null)
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

    private void RerollShopItems()
    {
        rerollCount++;
        shopRefreshCount++;
        rerollCost = baseRerollCost + rerollCount;

        int count = Mathf.Max(1, containersToAdd);
        ShopItemData[] nextItems = new ShopItemData[count];
        int writeIndex = 0;

        if (currentItems != null)
        {
            for (int i = 0; i < currentItems.Length && writeIndex < count; i++)
            {
                if (currentItems[i].Lock && currentItems[i].ItemData != null)
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

        ContentFactSource factSource = ContentFactSource.ForPlayer(player, currentWaveNumber);
        factSource.ShopRefreshCount = shopRefreshCount;
        factSource.ShopRerollCount = rerollCount;
        ContentRollResult result = contentPoolRollService.Roll(
            pool,
            factSource,
            shopRuntimeState,
            1,
            entry => entry.Content is ItemDataSO);
        if (!result.HasAny)
        {
            Debug.LogWarning("[ShopManager] No shop item could be rolled from content pool.", this);
            return default;
        }

        return CreateShopItemData(result.Items[0]);
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
            ContentPriceMultiplier = rollItem.PriceMultiplier
        };
    }

    private static int ResolveShopItemLevel(ItemDataSO itemData, ContentRollItem rollItem)
    {
        if (itemData == null || itemData.ItemType != ItemType.Weapon)
        {
            return WeaponLevelHelper.MinLevel;
        }

        int minLevel = rollItem.MinLevel > 0 ? WeaponLevelHelper.ClampLevel(rollItem.MinLevel) : WeaponLevelHelper.MinLevel;
        int maxLevel = rollItem.MaxLevel > 0 ? WeaponLevelHelper.ClampLevel(rollItem.MaxLevel) : WeaponLevelHelper.MaxLevel;
        if (maxLevel < minLevel)
        {
            maxLevel = minLevel;
        }

        return UnityEngine.Random.Range(minLevel, maxLevel + 1);
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
        bool canReroll = currentCurrency >= rerollCost || freeShopRerolls > 0;
        ViewStateChanged?.Invoke(new ShopViewState(currentItems, rerollCost, canReroll, reason));
    }

    private void ApplyShopPriceMultiplier()
    {
        float priceMultiplier = ResolveShopPriceMultiplier();
        for (int i = 0; i < currentItems.Length; i++)
        {
            currentItems[i].PriceMultiplier = priceMultiplier;
        }
    }

    public void RequestToggleLock(int itemIndex)
    {
        if (currentItems == null || itemIndex < 0 || itemIndex >= currentItems.Length)
        {
            return;
        }

        currentItems[itemIndex].Lock = !currentItems[itemIndex].Lock;
        print($"物品:{currentItems[itemIndex].ItemData.ItemName} 锁定状态:{currentItems[itemIndex].Lock}");
        PublishViewState(ShopRefreshReason.StateUpdate);
    }

    private void TryBindWallet()
    {
        if (currencyWallet != null && propertiesManager != null)
        {
            return;
        }

        if (player == null)
        {
            player = FindFirstObjectByType<Player>();
        }

        if (player == null)
        {
            return;
        }

        if (currencyWallet == null)
        {
            currencyWallet = player.GetComponent<CurrencyWallet>();
        }

        if (propertiesManager == null)
        {
            BindPropertiesManager(player.GetComponent<PropertiesManager>());
        }
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

        if (player == null)
        {
            TryBindWallet();
        }

        return player == null || player == eventPlayer;
    }

    private float ResolveShopPriceMultiplier()
    {
        if (propertiesManager == null)
        {
            TryBindWallet();
        }

        float discount = propertiesManager != null
            ? PropValueUtility.PercentPointsToEffectiveRatio(
                PropType.ShopPriceDiscount,
                propertiesManager.GetPropValue(PropType.ShopPriceDiscount))
            : 0f;
        return Mathf.Max(PropValueUtility.MIN_EFFECTIVE_SHOP_PRICE_MULTIPLIER, 1f - discount);
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
