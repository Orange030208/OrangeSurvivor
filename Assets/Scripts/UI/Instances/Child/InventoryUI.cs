using System.Collections.Generic;
using Survivors.Player;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [Header("绑定目标（可留空，运行时自动查找）")]
    [SerializeField] private WeaponsHolder weaponsHolder;
    [SerializeField] private AccessoryManager accessoryManager;

    [Header("容器与预制体")]
    [SerializeField] private InventoryItemContainer itemContainerPrefab;
    [SerializeField] private InventoryItemOperateContainer inventoryItemOperateContainer;
    [SerializeField] private SidebarSlider inventoryItemOperateContainerSidebar;
    [SerializeField] private Transform itemContainersParent;
    
    [SerializeField] private ClickOnlyHandler[] closeInventoryItemOperatePanelHandlers;    

    private readonly List<InventoryItemContainer> spawnedContainers = new();
    private bool subscribed;
    private SelectedInventoryItem selectedItem;

    private void OnEnable()
    {
        if (weaponsHolder == null || accessoryManager == null)
        {
            Bind(FindFirstObjectByType<Player>());
        }

        Subscribe();
        CloseOperatePanelImmediate();
        RefreshAll();
    }

    private void OnDisable()
    {
        Unsubscribe();
        CleanupSpawnedContainers();
        inventoryItemOperateContainer?.Cleanup();
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
            RefreshAll();
            return;
        }

        Unsubscribe();
        weaponsHolder = newWeaponsHolder;
        accessoryManager = newAccessoryManager;
        Subscribe();
        RefreshAll();
    }

    private void Subscribe()
    {
        if (subscribed)
        {
            return;
        }

        if (weaponsHolder != null)
        {
            weaponsHolder.OnWeaponsChanged += RefreshAll;
        }

        if (accessoryManager != null)
        {
            accessoryManager.OnAccessoryEquipped += OnAccessoryChanged;
            accessoryManager.OnAccessoryUnequipped += OnAccessoryChanged;
        }

        foreach (var item in closeInventoryItemOperatePanelHandlers)
        {
            item.OnClick += _ =>
            {
                CloseOperatePanel();
            };
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
            weaponsHolder.OnWeaponsChanged -= RefreshAll;
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
        RefreshAll();
    }

    private void RefreshAll()
    {
        if (itemContainersParent == null)
        {
            return;
        }

        CleanupSpawnedContainers();
        itemContainersParent.Clear();

        if (itemContainerPrefab == null)
        {
            return;
        }

        RefreshWeaponsInternal();
        RefreshAccessoriesInternal();
    }

    private void RefreshWeaponsInternal()
    {
        if (weaponsHolder == null)
        {
            return;
        }

        var equippedWeapons = weaponsHolder.EquippedWeapons;
        for (int i = 0; i < equippedWeapons.Count; i++)
        {
            var weaponInfo = equippedWeapons[i];
            InventoryItemContainer container = Instantiate(itemContainerPrefab, itemContainersParent);
            container.Configure(weaponInfo.WeaponData, weaponInfo.RuntimeWeapon.Level);

            SelectedInventoryItem clickItem = SelectedInventoryItem.CreateWeapon(
                weaponInfo.WeaponData,
                weaponInfo.RuntimeWeapon.Level,
                weaponInfo.RuntimeWeapon);

            container.AddClickListener(() => OpenOperatePanel(clickItem));
            spawnedContainers.Add(container);
        }
    }

    private void RefreshAccessoriesInternal()
    {
        if (accessoryManager == null)
        {
            return;
        }

        var accessories = accessoryManager.EquippedAccessories;
        for (int i = 0; i < accessories.Count; i++)
        {
            AccessoryDataSO accessory = accessories[i];
            InventoryItemContainer container = Instantiate(itemContainerPrefab, itemContainersParent);
            container.Configure(accessory, accessory.Rarity);

            SelectedInventoryItem clickItem = SelectedInventoryItem.CreateAccessory(accessory);

            container.AddClickListener(() => OpenOperatePanel(clickItem));
            spawnedContainers.Add(container);
        }
    }

    private void OpenOperatePanel(SelectedInventoryItem item)
    {
        if (inventoryItemOperateContainer == null)
        {
            return;
        }

        selectedItem = item;
        inventoryItemOperateContainer.Configure(
            item.ItemData,
            item.ColorDependencyNumber,
            item.GetProps(),
            OnSellClicked,
            OnMergeClicked,
            CloseOperatePanel);

        inventoryItemOperateContainerSidebar?.Show();
    }

    private void OnSellClicked()
    {
        if (selectedItem.ItemData == null)
        {
            return;
        }

        bool sold = false;

        if (selectedItem.ItemData.ItemType == ItemType.Weapon)
        {
            sold = weaponsHolder != null && weaponsHolder.RemoveWeapon(selectedItem.RuntimeWeapon);
        }
        else if (selectedItem.ItemData.ItemType == ItemType.Accessory)
        {
            sold = accessoryManager != null && accessoryManager.UnequipAccessory((AccessoryDataSO)selectedItem.ItemData);
        }

        if (!sold)
        {
            return;
        }

        CurrencyManager.Instance.AddCurrency(selectedItem.ItemData.ItemPrice);
        CloseOperatePanel();
        RefreshAll();
    }

    private void OnMergeClicked()
    {
        if (selectedItem.ItemData == null || selectedItem.ItemData.ItemType != ItemType.Weapon)
        {
            return;
        }

        Debug.Log($"[Inventory] 点击了合并（调试）: {selectedItem.ItemData.ItemName}, Lv.{selectedItem.ColorDependencyNumber}");
    }

    private void CloseOperatePanel()
    {
        selectedItem = default;
        inventoryItemOperateContainer?.Cleanup();
        inventoryItemOperateContainerSidebar?.Hide();
    }

    private void CloseOperatePanelImmediate()
    {
        selectedItem = default;
        inventoryItemOperateContainer?.Cleanup();
        inventoryItemOperateContainerSidebar?.HideImmediate();
    }

    private void CleanupSpawnedContainers()
    {
        for (int i = 0; i < spawnedContainers.Count; i++)
        {
            if (spawnedContainers[i] != null)
            {
                spawnedContainers[i].Dispose();
            }
        }

        spawnedContainers.Clear();
    }

    private readonly struct SelectedInventoryItem
    {
        public ItemDataSO ItemData { get; }
        public int ColorDependencyNumber { get; }
        public Weapon RuntimeWeapon { get; }

        private SelectedInventoryItem(ItemDataSO itemData, int colorDependencyNumber, Weapon runtimeWeapon)
        {
            ItemData = itemData;
            ColorDependencyNumber = colorDependencyNumber;
            RuntimeWeapon = runtimeWeapon;
        }

        public static SelectedInventoryItem CreateWeapon(WeaponDataSO weaponData, int level, Weapon runtimeWeapon)
        {
            return new SelectedInventoryItem(weaponData, level, runtimeWeapon);
        }

        public static SelectedInventoryItem CreateAccessory(AccessoryDataSO accessoryData)
        {
            return new SelectedInventoryItem(accessoryData, accessoryData.Rarity, null);
        }

        public Dictionary<PropType, float> GetProps()
        {
            if (ItemData == null)
            {
                return null;
            }

            if (ItemData.ItemType == ItemType.Weapon)
            {
                return ((WeaponDataSO)ItemData).GetPropsByLevel(ColorDependencyNumber);
            }

            return ((AccessoryDataSO)ItemData).GetProps();
        }
    }
}
