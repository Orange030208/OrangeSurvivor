using System;
using System.Collections.Generic;
using UnityEngine;

public struct ShopItemData
{
    public ItemDataSO ItemData;
    public int Level;
    public bool Lock;
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
        float multiplier = PriceMultiplier > 0f ? PriceMultiplier : 1f;
        return Mathf.Max(0, Mathf.RoundToInt(basePrice * multiplier));
    }
}

public class ShopManager : MonoBehaviour
{
    private const int DEFAULT_CONTAINERS_TO_ADD = 4;
    private const int BASE_REROLL_COST = 5;
    private const int ACCESSORY_WEIGHT = 2;
    private const int WEAPON_WEIGHT = 1;
    private const float MIN_SHOP_PRICE_MULTIPLIER = 0.2f;
    private const float MAX_SHOP_PRICE_DISCOUNT = 0.8f;

    [SerializeField] private int containersToAdd = DEFAULT_CONTAINERS_TO_ADD;
    [SerializeField] private int baseRerollCost = BASE_REROLL_COST;
    [SerializeField] private CurrencyWallet currencyWallet;

    private ShopItemData[] currentItems;
    private Player player;
    private PropertiesManager propertiesManager;
    private int freeShopRerolls;
    private int rerollCost;
    private int rerollCount;
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
        playerAccessoryManager.EquipAccessory(accessoryData);

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

        if (!weaponsHolder.AddWeapon(weaponData, itemData.Level))
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
            ShopItemData item = GenerateWeightedRandomShopItem();
            if (!ContainsDuplicate(existingItems, existingCount, item))
            {
                return item;
            }
        }

        return GenerateWeightedRandomShopItem();
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

    private ShopItemData GenerateWeightedRandomShopItem()
    {
        int randomValue = UnityEngine.Random.Range(0, ACCESSORY_WEIGHT + WEAPON_WEIGHT);
        if (randomValue < ACCESSORY_WEIGHT)
        {
            return GenerateAccessoryItem();
        }

        return GenerateWeaponItem();
    }

    private ShopItemData GenerateAccessoryItem()
    {
        AccessoryDataSO accessoryData = ResourcesManager.GetRandomAccessory();
        if (accessoryData == null)
        {
            Debug.LogWarning("Failed to get random accessory.");
            return default;
        }

        return new ShopItemData
        {
            ItemData = accessoryData,
            Level = WeaponLevelHelper.MinLevel,
            Lock = false
        };
    }

    private ShopItemData GenerateWeaponItem()
    {
        WeaponDataSO weaponData = ResourcesManager.GetRandomWeapon();
        if (weaponData == null)
        {
            Debug.LogWarning("No weapons available for shop.");
            return default;
        }

        int level = WeaponLevelHelper.GetRandomLevelInclusiveMax();
        return new ShopItemData
        {
            ItemData = weaponData,
            Level = level,
            Lock = false
        };
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
            ? Mathf.Clamp(PropValueUtility.PercentPointsToRatio(propertiesManager.GetPropValue(PropType.ShopPriceDiscount)), 0f, MAX_SHOP_PRICE_DISCOUNT)
            : 0f;
        return Mathf.Max(MIN_SHOP_PRICE_MULTIPLIER, 1f - discount);
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
