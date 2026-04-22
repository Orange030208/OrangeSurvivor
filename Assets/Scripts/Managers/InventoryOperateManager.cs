using System.Collections.Generic;
using UnityEngine;

public class InventoryOperateManager : MonoBehaviour
{
    [Header("绑定目标（可留空，运行时自动查找）")]
    [SerializeField] private WeaponsHolder weaponsHolder;
    [SerializeField] private AccessoryManager accessoryManager;
    [SerializeField] private CurrencyWallet currencyWallet;

    private readonly List<InventoryRuntimeItem> runtimeItems = new();
    private bool subscribed;

    private void OnEnable()
    {
        GameEventBus.Subscribe<PlayerSpawnedEvent>(OnPlayerSpawned);

        if (weaponsHolder == null || accessoryManager == null)
        {
            Bind(FindFirstObjectByType<Player>());
        }

        Subscribe();
        PublishInventorySnapshot();
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<PlayerSpawnedEvent>(OnPlayerSpawned);
        Unsubscribe();
    }

    public void Bind(Player player)
    {
        if (player == null)
        {
            return;
        }

        Bind(
            player.GetComponent<WeaponsHolder>(),
            player.GetComponent<AccessoryManager>(),
            player.GetComponent<CurrencyWallet>());
    }

    public void Bind(WeaponsHolder newWeaponsHolder, AccessoryManager newAccessoryManager, CurrencyWallet newCurrencyWallet)
    {
        bool sameTarget = weaponsHolder == newWeaponsHolder
                          && accessoryManager == newAccessoryManager
                          && currencyWallet == newCurrencyWallet;
        if (sameTarget)
        {
            PublishInventorySnapshot();
            return;
        }

        Unsubscribe();
        weaponsHolder = newWeaponsHolder;
        accessoryManager = newAccessoryManager;
        currencyWallet = newCurrencyWallet;
        Subscribe();
        PublishInventorySnapshot();
    }

    private void OnPlayerSpawned(PlayerSpawnedEvent eventData)
    {
        Bind(eventData.Player);
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

        int sellPrice = item.ItemData.ItemType == ItemType.Weapon ? item.GetSellPrice() : 0;
        InventoryItemOperateResource resource = new InventoryItemOperateResource(
            eventData.ItemIndex,
            item.ItemData,
            item.ColorDependencyNumber,
            sellPrice,
            item.ItemData);

        GameEventBus.Publish(new InventoryItemOperatePanelDataEvent(resource));
    }

    private void OnSellClicked(InventoryItemSellClickedEvent eventData)
    {
        if (!TryGetRuntimeItem(eventData.ItemIndex, out InventoryRuntimeItem item))
        {
            return;
        }

        if (item.ItemData.ItemType != ItemType.Weapon)
        {
            return;
        }

        bool sold = weaponsHolder != null && weaponsHolder.RemoveWeapon(item.RuntimeWeapon);
        if (!sold)
        {
            return;
        }

        currencyWallet?.ChangeAmount(item.GetSellPrice());
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
            if (candidate.ItemData.ItemType != ItemType.Weapon || candidate.RuntimeWeapon == null)
            {
                continue;
            }

            if (candidate.ItemData != selectedItem.ItemData)
            {
                continue;
            }

            if (candidate.ColorDependencyNumber != selectedItem.ColorDependencyNumber)
            {
                continue;
            }

            targetItem = candidate;
            return true;
        }

        targetItem = default;
        return false;
    }

    private bool TryGetRuntimeItem(int itemIndex, out InventoryRuntimeItem item)
    {
        if (itemIndex < 0 || itemIndex >= runtimeItems.Count)
        {
            item = default;
            return false;
        }

        item = runtimeItems[itemIndex];
        return true;
    }

    private static IDescribable BuildDisplayDocument(InventoryRuntimeItem item)
    {
        return item.ItemData;
    }

    private readonly struct InventoryRuntimeItem
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
            return ItemData switch
            {
                AccessoryDataSO accessoryData => accessoryData.RecyclePrice,
                _ => ItemData != null ? ItemData.ItemPrice : 0
            };
        }
    }
}
