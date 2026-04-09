using System;
using System.Collections.Generic;
using UnityEngine;

public struct ShopItemData
{
    public ItemDataSO ItemData;
    public int Level;
    public bool Lock;

    public int GetPrice()
    {
        if (ItemData == null)
        {
            return 0;
        }

        if (ItemData.ItemType == ItemType.Weapon)
        {
            return WeaponPriceHelper.GetPrice(ItemData.ItemPrice, Level);
        }

        return ItemData.ItemPrice;
    }
}

public class ShopManager : MonoBehaviour
{
    private const int DEFAULT_CONTAINERS_TO_ADD = 6;
    private const int BASE_REROLL_COST = 5;
    private const int ACCESSORY_WEIGHT = 2;
    private const int WEAPON_WEIGHT = 1;

    [SerializeField] private int containersToAdd = DEFAULT_CONTAINERS_TO_ADD;
    [SerializeField] private int baseRerollCost = BASE_REROLL_COST;

    private ShopItemData[] currentItems;
    private int rerollCost;
    private int rerollCount;

    private void Awake()
    {
        rerollCost = baseRerollCost;
    }

    private void OnEnable()
    {
        GameEventBus.Subscribe<RequestShopSnapshotEvent>(OnRequestSnapshot);
        GameEventBus.Subscribe<ShopItemClickedEvent>(OnItemClicked);
        GameEventBus.Subscribe<ShopRerollRequestedEvent>(OnRerollRequested);
        GameEventBus.Subscribe<ShopVideoAdRerollRequestedEvent>(OnVideoAdRerollRequested);
        GameEventBus.Subscribe<OperateShopItemLockEvent>(OnOperateShopItemLock);
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<RequestShopSnapshotEvent>(OnRequestSnapshot);
        GameEventBus.Unsubscribe<ShopItemClickedEvent>(OnItemClicked);
        GameEventBus.Unsubscribe<ShopRerollRequestedEvent>(OnRerollRequested);
        GameEventBus.Unsubscribe<ShopVideoAdRerollRequestedEvent>(OnVideoAdRerollRequested);
        GameEventBus.Unsubscribe<OperateShopItemLockEvent>(OnOperateShopItemLock);
    }

    private void Start()
    {
        GenerateShopItems();
        PublishShopItems();
    }

    private void OnRequestSnapshot()
    {
        PublishShopItems();
    }

    private void OnItemClicked(ShopItemClickedEvent eventData)
    {
        if (currentItems == null || eventData.ItemIndex < 0 || eventData.ItemIndex >= currentItems.Length)
        {
            GameEventBus.Publish(new ShopPurchaseFailedEvent("Invalid item index."));
            return;
        }

        ShopItemData itemData = currentItems[eventData.ItemIndex];
        if (itemData.ItemData == null)
        {
            GameEventBus.Publish(new ShopPurchaseFailedEvent("Item data is null."));
            return;
        }

        if (itemData.ItemData.ItemType == ItemType.Accessory)
        {
            ProcessAccessoryPurchase(itemData, eventData.ItemIndex);
        }
        else if (itemData.ItemData.ItemType == ItemType.Weapon)
        {
            ProcessWeaponPurchase(itemData, eventData.ItemIndex);
        }
    }

    private void ProcessAccessoryPurchase(ShopItemData itemData, int itemIndex)
    {
        var accessoryData = itemData.ItemData as AccessoryDataSO;
        if (accessoryData == null)
        {
            GameEventBus.Publish(new ShopPurchaseFailedEvent("Accessory data is null or wrong type."));
            return;
        }

        int price = itemData.GetPrice();
        if (CurrencyManager.Instance.Currency < price)
        {
            GameEventBus.Publish(new ShopPurchaseFailedEvent("Not enough currency."));
            return;
        }

        var player = FindFirstObjectByType<Survivors.Player.AccessoryManager>();
        if (player == null)
        {
            GameEventBus.Publish(new ShopPurchaseFailedEvent("Accessory manager not found."));
            return;
        }

        CurrencyManager.Instance.AddCurrency(-price);
        player.EquipAccessory(accessoryData);

        GameEventBus.Publish(new ShopPurchaseSuccessEvent(itemData.ItemData, itemData.Level));
        RemoveItemFromShop(itemIndex);
        PublishShopItems();
    }

    private void ProcessWeaponPurchase(ShopItemData itemData, int itemIndex)
    {
        var weaponData = itemData.ItemData as WeaponDataSO;
        if (weaponData == null)
        {
            GameEventBus.Publish(new ShopPurchaseFailedEvent("Weapon data is null or wrong type."));
            return;
        }

        int price = itemData.GetPrice();
        if (CurrencyManager.Instance.Currency < price)
        {
            GameEventBus.Publish(new ShopPurchaseFailedEvent("Not enough currency."));
            return;
        }

        var weaponsHolder = FindFirstObjectByType<WeaponsHolder>();
        if (weaponsHolder == null)
        {
            GameEventBus.Publish(new ShopPurchaseFailedEvent("Weapons holder not found."));
            return;
        }

        if (!weaponsHolder.AddWeapon(weaponData, itemData.Level))
        {
            GameEventBus.Publish(new ShopPurchaseFailedEvent("No empty weapon slot available."));
            return;
        }

        CurrencyManager.Instance.AddCurrency(-price);

        GameEventBus.Publish(new ShopPurchaseSuccessEvent(itemData.ItemData, itemData.Level));
        RemoveItemFromShop(itemIndex);
        PublishShopItems();
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

    private void OnRerollRequested()
    {
        if (CurrencyManager.Instance.Currency < rerollCost)
        {
            GameEventBus.Publish(new ShopPurchaseFailedEvent($"Not enough currency for reroll. Cost: {rerollCost}"));
            return;
        }

        CurrencyManager.Instance.AddCurrency(-rerollCost);
        RerollShopItems();
        PublishShopItems();
    }

    private void OnVideoAdRerollRequested()
    {
        Debug.Log("Video ad reroll requested - implement ad integration here.");
        RerollShopItems();
        PublishShopItems();
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
        var weaponData = ResourcesManager.GetRandomWeapon();
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

    private void PublishShopItems()
    {
        if (currentItems == null)
        {
            currentItems = Array.Empty<ShopItemData>();
        }

        GameEventBus.Publish(new ShopItemsChangedEvent(currentItems, rerollCost));
    }

    private void OnOperateShopItemLock(OperateShopItemLockEvent @event)
    {
        if (currentItems == null || @event.Index < 0 || @event.Index >= currentItems.Length)
        {
            return;
        }

        currentItems[@event.Index].Lock = !currentItems[@event.Index].Lock;
        print($"物品:{currentItems[@event.Index].ItemData.ItemName} 锁定状态:{currentItems[@event.Index].Lock}");
        PublishShopItems();
    }
}
