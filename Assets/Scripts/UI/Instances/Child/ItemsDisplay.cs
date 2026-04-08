using Survivors.Player;
using UnityEngine;

public class ItemsDisplay : MonoBehaviour
{
    [Header("绑定目标（可留空，运行时自动查找）")]
    [SerializeField] private WeaponsHolder weaponsHolder;
    [SerializeField] private AccessoryManager accessoryManager;

    [Header("容器与预制体")]
    [SerializeField] private InventoryItemContainer itemContainerPrefab;
    [SerializeField] private Transform itemContainersParent;

    private void OnEnable()
    {
        if (weaponsHolder == null || accessoryManager == null)
        {
            Bind(FindFirstObjectByType<Player>());
        }

        Subscribe();
        RefreshAll();
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
        if (weaponsHolder != null)
        {
            weaponsHolder.OnWeaponsChanged += RefreshAll;
        }

        if (accessoryManager != null)
        {
            accessoryManager.OnAccessoryEquipped += OnAccessoryChanged;
            accessoryManager.OnAccessoryUnequipped += OnAccessoryChanged;
        }
    }

    private void Unsubscribe()
    {
        if (weaponsHolder != null)
        {
            weaponsHolder.OnWeaponsChanged -= RefreshWeapons;
        }

        if (accessoryManager != null)
        {
            accessoryManager.OnAccessoryEquipped -= OnAccessoryChanged;
            accessoryManager.OnAccessoryUnequipped -= OnAccessoryChanged;
        }
    }

    private void OnAccessoryChanged(AccessoryDataSO _)
    {
        RefreshAll();
    }

    private void RefreshWeapons()
    {
        RefreshAll();
    }

    private void RefreshAll()
    {
        if (itemContainersParent == null)
        {
            return;
        }

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
            InventoryItemContainer container = Instantiate(itemContainerPrefab, itemContainersParent);
            container.Configure(accessories[i],accessories[i].Rarity);
        }
    }
}
