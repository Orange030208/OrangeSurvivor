using System;
using System.Linq;
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

        if (itemData.ItemType == ItemType.Accessory)
        {
            ProcessAccessoryPurchase(itemData);
        }
        else if (itemData.ItemType == ItemType.Weapon)
        {
            ProcessWeaponPurchase(itemData);
        }
    }

    private void ProcessAccessoryPurchase(ShopItemData itemData)
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

        GameEventBus.Publish(new ShopPurchaseSuccessEvent(itemData));
        RemoveItemFromShop(itemData.Index);
    }

    private void ProcessWeaponPurchase(ShopItemData itemData)
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

        GameEventBus.Publish(new ShopPurchaseSuccessEvent(itemData));
        RemoveItemFromShop(itemData.Index);
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
                newItems[writeIndex].Index = writeIndex;
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
            currentItems[i] = GenerateRandomShopItem(i);
        }
    }

    private ShopItemData GenerateRandomShopItem(int index)
    {
        int randomValue = Random.Range(0, 3);
        switch (randomValue)
        {
            case 0:
            case 1:
                return GenerateAccessoryItem(index);
            case 2:
                return GenerateWeaponItem(index);
            default:
                return GenerateAccessoryItem(index);
        }
    }

    private ShopItemData GenerateAccessoryItem(int index)
    {
        AccessoryDataSO accessoryData = null;

        accessoryData = ResourcesManager.GetRandomAccessory();

        if (accessoryData == null)
        {
            Debug.LogWarning("Failed to get random accessory.");
            return new ShopItemData { Index = index };
        }

        return ShopItemData.CreateAccessory(index, accessoryData);
    }

    private ShopItemData GenerateWeaponItem(int index)
    {
        var weaponData = ResourcesManager.GetRandomWeapon();
        if (weaponData == null)
        {
            Debug.LogWarning("No weapons available for shop.");
            return new ShopItemData { Index = index };
        }

        int level = Random.Range(1, 7);
        return ShopItemData.CreateWeapon(index, weaponData, level);
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
