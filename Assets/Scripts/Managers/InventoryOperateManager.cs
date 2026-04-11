using System.Collections.Generic;
using UnityEngine;

public class InventoryOperateManager : MonoBehaviour
{
    [Header("绑定目标（可留空，运行时自动查找）")]
    [SerializeField] private WeaponsHolder weaponsHolder;
    [SerializeField] private AccessoryManager accessoryManager;

    private readonly List<InventoryRuntimeItem> runtimeItems = new();
    private bool subscribed;

    private void OnEnable()
    {
        if (weaponsHolder == null || accessoryManager == null)
        {
            Bind(FindFirstObjectByType<Player>());
        }

        Subscribe();
        PublishInventorySnapshot();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void Bind(Player player)
    {
        if (player == null)
        {
            return;
        }

        Bind(player.GetComponent<WeaponsHolder>(), player.GetComponent<AccessoryManager>());
    }

    public void Bind(WeaponsHolder newWeaponsHolder, AccessoryManager newAccessoryManager)
    {
        bool sameTarget = weaponsHolder == newWeaponsHolder && accessoryManager == newAccessoryManager;
        if (sameTarget)
        {
            PublishInventorySnapshot();
            return;
        }

        Unsubscribe();
        weaponsHolder = newWeaponsHolder;
        accessoryManager = newAccessoryManager;
        Subscribe();
        PublishInventorySnapshot();
    }

    private void Subscribe()
    {
        if (subscribed)
        {
            return;
        }

        if (weaponsHolder != null)
        {
            weaponsHolder.OnWeaponsChanged += PublishInventorySnapshot;
        }

        if (accessoryManager != null)
        {
            accessoryManager.OnAccessoryEquipped += OnAccessoryChanged;
            accessoryManager.OnAccessoryUnequipped += OnAccessoryChanged;
        }

        GameEventBus.Subscribe<RequestInventorySnapshotEvent>(OnRequestInventorySnapshot);
        GameEventBus.Subscribe<RequestInventoryItemOperatePanelEvent>(OnRequestOperatePanelData);
        GameEventBus.Subscribe<InventoryItemSellClickedEvent>(OnSellClicked);
        GameEventBus.Subscribe<InventoryItemMergeClickedEvent>(OnMergeClicked);

        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed)
        {
            return;
        }

        if (weaponsHolder != null)
        {
            weaponsHolder.OnWeaponsChanged -= PublishInventorySnapshot;
        }

        if (accessoryManager != null)
        {
            accessoryManager.OnAccessoryEquipped -= OnAccessoryChanged;
            accessoryManager.OnAccessoryUnequipped -= OnAccessoryChanged;
        }

        GameEventBus.Unsubscribe<RequestInventorySnapshotEvent>(OnRequestInventorySnapshot);
        GameEventBus.Unsubscribe<RequestInventoryItemOperatePanelEvent>(OnRequestOperatePanelData);
        GameEventBus.Unsubscribe<InventoryItemSellClickedEvent>(OnSellClicked);
        GameEventBus.Unsubscribe<InventoryItemMergeClickedEvent>(OnMergeClicked);

        subscribed = false;
    }

    private void OnAccessoryChanged(AccessoryDataSO _)
    {
        PublishInventorySnapshot();
    }

    private void OnRequestInventorySnapshot(RequestInventorySnapshotEvent _)
    {
        PublishInventorySnapshot();
    }

    private void PublishInventorySnapshot()
    {
        runtimeItems.Clear();

        if (weaponsHolder != null)
        {
            var equippedWeapons = weaponsHolder.EquippedWeapons;
            for (int i = 0; i < equippedWeapons.Count; i++)
            {
                var weaponInfo = equippedWeapons[i];
                runtimeItems.Add(InventoryRuntimeItem.CreateWeapon(
                    weaponInfo.WeaponData,
                    weaponInfo.RuntimeWeapon.Level,
                    weaponInfo.RuntimeWeapon));
            }
        }

        if (accessoryManager != null)
        {
            var accessories = accessoryManager.EquippedAccessories;
            for (int i = 0; i < accessories.Count; i++)
            {
                AccessoryDataSO accessory = accessories[i];
                runtimeItems.Add(InventoryRuntimeItem.CreateAccessory(accessory));
            }
        }

        InventoryUIItemSnapshot[] snapshots = new InventoryUIItemSnapshot[runtimeItems.Count];
        for (int i = 0; i < runtimeItems.Count; i++)
        {
            snapshots[i] = new InventoryUIItemSnapshot(runtimeItems[i].ItemData, runtimeItems[i].ColorDependencyNumber);
        }

        GameEventBus.Publish(new InventorySnapshotChangedEvent(snapshots));
    }

    private void OnRequestOperatePanelData(RequestInventoryItemOperatePanelEvent eventData)
    {
        if (!TryGetRuntimeItem(eventData.ItemIndex, out InventoryRuntimeItem item))
        {
            return;
        }

        InventoryItemOperateResource resource = new InventoryItemOperateResource(
            eventData.ItemIndex,
            item.ItemData,
            item.ColorDependencyNumber,
            item.GetSellPrice(),
            BuildDescriptions(item));

        GameEventBus.Publish(new InventoryItemOperatePanelDataEvent(resource));
    }

    private void OnSellClicked(InventoryItemSellClickedEvent eventData)
    {
        if (!TryGetRuntimeItem(eventData.ItemIndex, out InventoryRuntimeItem item))
        {
            return;
        }

        bool sold = false;
        if (item.ItemData.ItemType == ItemType.Weapon)
        {
            sold = weaponsHolder != null && weaponsHolder.RemoveWeapon(item.RuntimeWeapon);
        }
        else if (item.ItemData.ItemType == ItemType.Accessory)
        {
            sold = accessoryManager != null && accessoryManager.UnequipAccessory((AccessoryDataSO)item.ItemData);
        }

        if (!sold)
        {
            return;
        }

        GameEventBus.Publish(new CurrencyChangeRequestedEvent(CurrencyType.Currency, item.GetSellPrice()));
        GameEventBus.Publish(new InventoryItemOperatePanelShouldCloseEvent(eventData.ItemIndex));
    }

    private void OnMergeClicked(InventoryItemMergeClickedEvent eventData)
    {
        if (!TryGetRuntimeItem(eventData.ItemIndex, out InventoryRuntimeItem selectedItem))
        {
            return;
        }

        if (selectedItem.ItemData.ItemType != ItemType.Weapon || selectedItem.RuntimeWeapon == null)
        {
            return;
        }

        if (!WeaponLevelHelper.CanMerge(selectedItem.ColorDependencyNumber))
        {
            Debug.Log("[Inventory] 合并失败：当前武器已达到最大等级");
            return;
        }

        if (!TryFindMergeTarget(eventData.ItemIndex, selectedItem, out InventoryRuntimeItem targetItem))
        {
            Debug.Log("[Inventory] 合并失败：没有可合并的同名同等级武器");
            return;
        }

        if (weaponsHolder == null)
        {
            return;
        }

        bool merged = weaponsHolder.MergeWeapon(selectedItem.RuntimeWeapon, targetItem.RuntimeWeapon);
        if (!merged)
        {
            Debug.LogWarning("[Inventory] 合并失败：武器合并失败");
            return;
        }

        GameEventBus.Publish(new InventoryItemOperatePanelShouldCloseEvent(eventData.ItemIndex));
    }

    private bool TryFindMergeTarget(int selectedIndex, InventoryRuntimeItem selectedItem, out InventoryRuntimeItem targetItem)
    {
        for (int i = 0; i < runtimeItems.Count; i++)
        {
            if (i == selectedIndex)
            {
                continue;
            }

            InventoryRuntimeItem candidate = runtimeItems[i];
            if (candidate.ItemData == null || candidate.RuntimeWeapon == null)
            {
                continue;
            }

            bool sameWeapon = candidate.ItemData == selectedItem.ItemData;
            bool sameLevel = candidate.ColorDependencyNumber == selectedItem.ColorDependencyNumber;
            if (sameWeapon && sameLevel)
            {
                targetItem = candidate;
                return true;
            }
        }

        targetItem = default;
        return false;
    }

    private bool TryGetRuntimeItem(int index, out InventoryRuntimeItem item)
    {
        if (index < 0 || index >= runtimeItems.Count)
        {
            item = default;
            return false;
        }

        item = runtimeItems[index];
        return true;
    }

    private static IReadOnlyList<string> BuildDescriptions(InventoryRuntimeItem item)
    {
        if (item.ItemData is WeaponDataSO weaponData)
        {
            return weaponData.GetDescriptions(item.ColorDependencyNumber);
        }

        if (item.ItemData is AccessoryDataSO accessoryData)
        {
            return accessoryData.GetDescriptions();
        }

        return System.Array.Empty<string>();
    }
}

public readonly struct InventoryRuntimeItem
{
    public ItemDataSO ItemData { get; }
    public int ColorDependencyNumber { get; }
    public Weapon RuntimeWeapon { get; }

    private InventoryRuntimeItem(ItemDataSO itemData, int colorDependencyNumber, Weapon runtimeWeapon)
    {
        ItemData = itemData;
        ColorDependencyNumber = colorDependencyNumber;
        RuntimeWeapon = runtimeWeapon;
    }

    public static InventoryRuntimeItem CreateWeapon(WeaponDataSO weaponData, int level, Weapon runtimeWeapon)
    {
        return new InventoryRuntimeItem(weaponData, level, runtimeWeapon);
    }

    public static InventoryRuntimeItem CreateAccessory(AccessoryDataSO accessoryData)
    {
        return new InventoryRuntimeItem(accessoryData, accessoryData != null ? accessoryData.Rarity : 0, null);
    }

    public int GetSellPrice()
    {
        if (ItemData == null)
        {
            return 0;
        }

        if (ItemData is WeaponDataSO)
        {
            return WeaponPriceHelper.GetPrice(ItemData.ItemPrice, ColorDependencyNumber);
        }

        if (ItemData is AccessoryDataSO accessoryData)
        {
            return accessoryData.RecyclePrice;
        }

        return ItemData.ItemPrice;
    }
}
