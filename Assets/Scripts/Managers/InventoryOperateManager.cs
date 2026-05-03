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

    public event System.Action<InventoryUIItemSnapshot[]> SnapshotChanged;
    public event System.Action<InventoryItemOperateResource> OperatePanelOpened;
    public event System.Action<string> OperatePanelShouldClose;

    private void OnEnable()
    {
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

        Bind(
            player.GetComponent<WeaponsHolder>(),
            player.GetComponent<AccessoryManager>(),
            player.GetComponent<CurrencyWallet>());
    }

    public void Bind(WeaponsHolder targetWeaponsHolder, AccessoryManager targetAccessoryManager, CurrencyWallet targetCurrencyWallet)
    {
        bool sameTarget = weaponsHolder == targetWeaponsHolder
                          && accessoryManager == targetAccessoryManager
                          && currencyWallet == targetCurrencyWallet;
        if (sameTarget)
        {
            PublishInventorySnapshot();
            return;
        }

        Unsubscribe();
        weaponsHolder = targetWeaponsHolder;
        accessoryManager = targetAccessoryManager;
        currencyWallet = targetCurrencyWallet;
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

        subscribed = false;
    }

    private void OnAccessoryChanged(AccessoryDataSO _)
    {
        PublishInventorySnapshot();
    }

    public void RequestSnapshot()
    {
        PublishInventorySnapshot();
    }

    public void RequestOpenItemPanel(string entryId)
    {
        if (!TryGetRuntimeItem(entryId, out InventoryRuntimeItem item))
        {
            return;
        }

        int sellPrice = item.ItemData.ItemType == ItemType.Weapon ? item.GetSellPrice() : 0;
        InventoryItemOperateResource resource = new InventoryItemOperateResource(
            item.EntryId,
            item.ItemData,
            item.ColorDependencyNumber,
            sellPrice,
            item.GetDescribable());

        OperatePanelOpened?.Invoke(resource);
    }

    public void RequestSellItem(string entryId)
    {
        if (!TryGetRuntimeItem(entryId, out InventoryRuntimeItem item))
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
        NotifyOperatePanelShouldClose(item.EntryId);
    }

    public void RequestMergeItem(string entryId)
    {
        if (!TryGetRuntimeItem(entryId, out InventoryRuntimeItem selectedItem))
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

        if (!TryFindMergeTarget(selectedItem.EntryId, selectedItem, out InventoryRuntimeItem targetItem))
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

        NotifyOperatePanelShouldClose(selectedItem.EntryId);
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
                    BuildWeaponEntryId(weaponInfo.RuntimeWeapon),
                    weaponInfo.WeaponData,
                    weaponInfo.RuntimeWeapon.Level,
                    weaponInfo.RuntimeWeapon));
            }
        }

        if (accessoryManager != null)
        {
            var accessories = accessoryManager.EquippedAccessoryList;
            for (int i = 0; i < accessories.Count; i++)
            {
                RuntimeAccessoryData accessory = accessories[i];
                runtimeItems.Add(InventoryRuntimeItem.CreateAccessory(
                    BuildAccessoryEntryId(accessory.RuntimeId),
                    accessory.AccessoryData));
            }
        }

        InventoryUIItemSnapshot[] snapshots = new InventoryUIItemSnapshot[runtimeItems.Count];
        for (int i = 0; i < runtimeItems.Count; i++)
        {
            snapshots[i] = new InventoryUIItemSnapshot(
                runtimeItems[i].EntryId,
                runtimeItems[i].ItemData,
                runtimeItems[i].ColorDependencyNumber);
        }

        SnapshotChanged?.Invoke(snapshots);
    }

    private bool TryFindMergeTarget(string selectedEntryId, InventoryRuntimeItem selectedItem, out InventoryRuntimeItem targetItem)
    {
        for (int i = 0; i < runtimeItems.Count; i++)
        {
            InventoryRuntimeItem candidate = runtimeItems[i];
            if (candidate.EntryId == selectedEntryId)
            {
                continue;
            }

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

    private bool TryGetRuntimeItem(string entryId, out InventoryRuntimeItem item)
    {
        if (string.IsNullOrEmpty(entryId))
        {
            item = default;
            return false;
        }

        for (int i = 0; i < runtimeItems.Count; i++)
        {
            if (runtimeItems[i].EntryId != entryId)
            {
                continue;
            }

            item = runtimeItems[i];
            return true;
        }

        item = default;
        return false;
    }

    private static string BuildWeaponEntryId(Weapon runtimeWeapon)
    {
        return runtimeWeapon == null ? null : $"WPN_{runtimeWeapon.GetInstanceID()}";
    }

    private static string BuildAccessoryEntryId(string runtimeSourceId)
    {
        return string.IsNullOrEmpty(runtimeSourceId) ? null : runtimeSourceId;
    }

    private void NotifyOperatePanelShouldClose(string entryId)
    {
        OperatePanelShouldClose?.Invoke(entryId);
    }

    private readonly struct InventoryRuntimeItem
    {
        public string EntryId { get; }
        public ItemDataSO ItemData { get; }
        public int ColorDependencyNumber { get; }
        public Weapon RuntimeWeapon { get; }

        private InventoryRuntimeItem(string entryId, ItemDataSO itemData, int colorDependencyNumber, Weapon runtimeWeapon)
        {
            EntryId = entryId;
            ItemData = itemData;
            ColorDependencyNumber = colorDependencyNumber;
            RuntimeWeapon = runtimeWeapon;
        }

        public static InventoryRuntimeItem CreateWeapon(string entryId, WeaponDataSO weaponData, int level, Weapon runtimeWeapon)
        {
            return new InventoryRuntimeItem(entryId, weaponData, level, runtimeWeapon);
        }

        public static InventoryRuntimeItem CreateAccessory(string entryId, AccessoryDataSO accessoryData)
        {
            return new InventoryRuntimeItem(entryId, accessoryData, accessoryData != null ? accessoryData.Rarity : 0, null);
        }

        public int GetSellPrice()
        {
            return ItemData switch
            {
                AccessoryDataSO accessoryData => accessoryData.RecyclePrice,
                _ => ItemData != null ? ItemData.ItemPrice : 0
            };
        }

        public IDescribable GetDescribable()
        {
            return ItemData switch
            {
                WeaponDataSO weaponData => new WeaponLevelDescribable(weaponData, ColorDependencyNumber),
                IDescribable describable => describable,
                _ => IDescribable.Default
            };
        }
    }
}
