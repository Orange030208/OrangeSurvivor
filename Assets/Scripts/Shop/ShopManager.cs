using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class ShopManager : MonoBehaviour
{
    private const int DEFAULT_CONTAINERS_TO_ADD = 6;
    private const int BASE_REROLL_COST = 5;

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
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<RequestShopSnapshotEvent>(OnRequestSnapshot);
        GameEventBus.Unsubscribe<ShopItemClickedEvent>(OnItemClicked);
        GameEventBus.Unsubscribe<ShopRerollRequestedEvent>(OnRerollRequested);
        GameEventBus.Unsubscribe<ShopVideoAdRerollRequestedEvent>(OnVideoAdRerollRequested);
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
        if (eventData.ItemIndex < 0 || eventData.ItemIndex >= currentItems.Length)
        {
            GameEventBus.Publish(new ShopPurchaseFailedEvent("Invalid item index."));
            return;
        }

        ShopItemData itemData = currentItems[eventData.ItemIndex];

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

        int price = accessoryData.ItemPrice;
        if (CurrencyManager.Instance.Currency < price)
        {
            GameEventBus.Publish(new ShopPurchaseFailedEvent("Not enough currency."));
            return;
        }

        CurrencyManager.Instance.AddCurrency(-price);

        var player = FindFirstObjectByType<Survivors.Player.AccessoryManager>();
        if (player != null)
        {
            player.EquipAccessory(accessoryData);
        }

        GameEventBus.Publish(new ShopPurchaseSuccessEvent(itemData.ItemData, itemData.Level));
        RemoveItemFromShop(itemIndex);
    }

    private void ProcessWeaponPurchase(ShopItemData itemData, int itemIndex)
    {
        var weaponData = itemData.ItemData as WeaponDataSO;
        if (weaponData == null)
        {
            GameEventBus.Publish(new ShopPurchaseFailedEvent("Weapon data is null or wrong type."));
            return;
        }

        int price = weaponData.ItemPrice;
        if (CurrencyManager.Instance.Currency < price)
        {
            GameEventBus.Publish(new ShopPurchaseFailedEvent("Not enough currency."));
            return;
        }

        CurrencyManager.Instance.AddCurrency(-price);

        var weaponsHolder = FindFirstObjectByType<WeaponsHolder>();
        if (weaponsHolder != null)
        {
            weaponsHolder.AddWeapon(weaponData, itemData.Level);
        }

        GameEventBus.Publish(new ShopPurchaseSuccessEvent(itemData.ItemData, itemData.Level));
        RemoveItemFromShop(itemIndex);
    }

    private void RemoveItemFromShop(int index)
    {
        if (currentItems == null || index < 0 || index >= currentItems.Length)
        {
            return;
        }

        var newItems = new ShopItemData[currentItems.Length - 1];
        int writeIndex = 0;
        for (int i = 0; i < currentItems.Length; i++)
        {
            if (i != index)
            {
                newItems[writeIndex] = currentItems[i];
                writeIndex++;
            }
        }

        currentItems = newItems;
        PublishShopItems();
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
        GenerateShopItems();
    }

    private void GenerateShopItems()
    {
        int count = Mathf.Max(1, containersToAdd);
        currentItems = new ShopItemData[count];

        for (int i = 0; i < count; i++)
        {
            currentItems[i] = GenerateRandomShopItem();
        }
    }

    private ShopItemData GenerateRandomShopItem()
    {
        int randomValue = Random.Range(0, 3);
        switch (randomValue)
        {
            case 0:
            case 1:
                return GenerateAccessoryItem();
            case 2:
                return GenerateWeaponItem();
            default:
                return GenerateAccessoryItem();
        }
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
            Level = 1
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

        int level = Random.Range(1, 7);
        return new ShopItemData
        {
            ItemData = weaponData,
            Level = level
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
}
