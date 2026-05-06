using System;
using System.Collections.Generic;
using UnityEngine;

public static class ResourcesManager
{
    private const string ACCESSORY_DATA_PATH = "Data/Accessory Data List";
    private const string WEAPON_DATA_PATH = "Data/Weapon Data List";
    private const string CHARACTER_DATA_PATH = "Data/Characters";
    private const string PLAYER_LEVEL_CONFIG_DATA_PATH = "Data/Player Level Config";
    private const string UPGRADE_CARD_POOL_DATA_PATH = "Data/UpgradeCards/Pool/Default Upgrade Card Pool";
    private const string CARD_QUALITY_PRESENTATION_CATALOG_PATH = "Data/UpgradeCards/Presentation/Upgrade Card Rarity Presentation Catalog";
    // 属性展示目录由 ResourcesManager 统一管理，资产需放在 Assets/Resources/Data/Prop Presentation Catalog.asset。
    private const string PROP_PRESENTATION_CATALOG_PATH = "Data/Prop Presentation Catalog";
    private const string STAGE_DEFINITION_DATA_PATH = "Data/Waves/Stage Definition";
    private const string PLAYER_PREFAB_PATH = "Prefabs/Player";
    private const string DEFAULT_PLAYER_PREFAB_NAME = "Character";

    private static AccessoryDataSO[] Accessories;
    private static WeaponDataSO[] Weapons;
    private static CharacterDataSO[] characters;
    private static PlayerLevelConfigSO playerLevelConfig;
    private static UpgradeCardPoolSO upgradeCardPool;
    private static CardQualityPresentationCatalogSO cardQualityPresentationCatalog;
    private static PropPresentationCatalogSO propPresentationCatalog;
    private static StageDefinitionSO stageDefinition;
    private const string ITEM_QUALITY_ICON_EFFECT_MATERIAL_PATH = "Materials/UI/WeaponQualityIconEffect";

    private static Material cachedItemQualityIconEffectMaterial;

    public static Sprite GetPropIcon(PropType propType)
    {
        return TryGetPropPresentationEntry(propType, out PropPresentationEntry entry) ? entry.Icon : null;
    }

    public static string GetPropDisplayName(PropType propType)
    {
        return TryGetPropPresentationEntry(propType, out PropPresentationEntry entry) &&
               !string.IsNullOrWhiteSpace(entry.ChineseName)
            ? entry.ChineseName
            : propType.ToString();
    }

    public static string GetPropDescription(PropType propType)
    {
        return TryGetPropPresentationEntry(propType, out PropPresentationEntry entry)
            ? entry.Description
            : string.Empty;
    }

    private static void LoadAccessoryData()
    {
        if (Accessories == null)
        {
            Accessories = Resources.Load<AccessoryDataListSO>(ACCESSORY_DATA_PATH).Accessories;
        }
    }

    private static void LoadWeaponData()
    {
        if (Weapons == null)
        {
            Weapons = Resources.Load<WeaponDataListSO>(WEAPON_DATA_PATH).Weapons;
        }
    }

    private static void LoadCharacterData()
    {
        if (characters == null)
        {
            CharacterDataSO[] loadedCharacters = Resources.LoadAll<CharacterDataSO>(CHARACTER_DATA_PATH);
            Array.Sort(loadedCharacters, CompareCharacterNames);
            characters = loadedCharacters;
        }
    }

    private static int CompareCharacterNames(CharacterDataSO left, CharacterDataSO right)
    {
        string leftName = left != null ? left.name : string.Empty;
        string rightName = right != null ? right.name : string.Empty;
        return string.Compare(leftName, rightName, StringComparison.Ordinal);
    }

    private static void LoadPlayerLevelConfig()
    {
        if (playerLevelConfig == null)
        {
            playerLevelConfig = Resources.Load<PlayerLevelConfigSO>(PLAYER_LEVEL_CONFIG_DATA_PATH);
        }
    }

    private static void LoadUpgradeCardPool()
    {
        if (upgradeCardPool == null)
        {
            upgradeCardPool = Resources.Load<UpgradeCardPoolSO>(UPGRADE_CARD_POOL_DATA_PATH);
        }
    }

    private static void LoadCardQualityPresentationCatalog()
    {
        if (cardQualityPresentationCatalog == null)
        {
            cardQualityPresentationCatalog =
                Resources.Load<CardQualityPresentationCatalogSO>(CARD_QUALITY_PRESENTATION_CATALOG_PATH);
        }
    }

    private static void LoadPropPresentationCatalog()
    {
        if (propPresentationCatalog == null)
        {
            propPresentationCatalog = Resources.Load<PropPresentationCatalogSO>(PROP_PRESENTATION_CATALOG_PATH);
        }
    }

    private static void LoadStageDefinition()
    {
        if (stageDefinition == null)
        {
            stageDefinition = Resources.Load<StageDefinitionSO>(STAGE_DEFINITION_DATA_PATH);
        }
    }

    public static WeaponDataSO GetWeapon(string weaponName)
    {
        LoadWeaponData();
        if (string.IsNullOrEmpty(weaponName))
        {
            Debug.LogWarning("WeaponName cannot be null or empty.");
            return null;
        }

        for (int i = 0; i < Weapons.Length; i++)
        {
            WeaponDataSO weapon = Weapons[i];
            if (weapon != null && weapon.ItemName == weaponName)
            {
                return weapon;
            }
        }

        return null;
    }

    public static WeaponDataSO GetRandomWeapon()
    {
        LoadWeaponData();
        if (Weapons == null || Weapons.Length == 0)
        {
            Debug.LogWarning("No weapons available.");
            return null;
        }

        return Weapons[UnityEngine.Random.Range(0, Weapons.Length)];
    }

    public static WeaponDataSO[] GetAllWeapons()
    {
        LoadWeaponData();
        return Weapons ?? Array.Empty<WeaponDataSO>();
    }

    public static CharacterDataSO[] GetAllCharacters()
    {
        LoadCharacterData();
        return characters ?? Array.Empty<CharacterDataSO>();
    }

    public static Player GetPlayerPrefab(string prefabName)
    {
        if (string.IsNullOrWhiteSpace(prefabName))
        {
            return null;
        }

        return Resources.Load<Player>($"{PLAYER_PREFAB_PATH}/{prefabName}");
    }

    public static Player GetDefaultPlayerPrefab()
    {
        return GetPlayerPrefab(DEFAULT_PLAYER_PREFAB_NAME);
    }

    public static PlayerLevelConfigSO GetPlayerLevelConfig()
    {
        LoadPlayerLevelConfig();
        return playerLevelConfig;
    }

    public static UpgradeCardPoolSO GetUpgradeCardPool()
    {
        LoadUpgradeCardPool();
        if (upgradeCardPool == null)
        {
            Debug.LogError(
                $"{nameof(ResourcesManager)} could not find {nameof(UpgradeCardPoolSO)} at {UPGRADE_CARD_POOL_DATA_PATH}. " +
                "Run Survivors/Upgrades/Rebuild Upgrade Card System to generate the configured card pool.");
        }

        return upgradeCardPool;
    }

    public static CardQualityPresentationCatalogSO GetCardQualityPresentationCatalog()
    {
        LoadCardQualityPresentationCatalog();
        if (cardQualityPresentationCatalog == null)
        {
            Debug.LogError(
                $"{nameof(ResourcesManager)} could not find {nameof(CardQualityPresentationCatalogSO)} at {CARD_QUALITY_PRESENTATION_CATALOG_PATH}. " +
                "Run Survivors/Upgrades/Rebuild Upgrade Card System to generate the configured rarity presentation profiles.");
        }

        return cardQualityPresentationCatalog;
    }

    public static PropPresentationCatalogSO GetPropPresentationCatalog()
    {
        LoadPropPresentationCatalog();
        if (propPresentationCatalog == null)
        {
            Debug.LogError(
                $"{nameof(ResourcesManager)} could not find {nameof(PropPresentationCatalogSO)} at {PROP_PRESENTATION_CATALOG_PATH}. " +
                $"Create it from {ScriptableObjectMenuPaths.PROP_PRESENTATION_CATALOG} and place it under Assets/Resources/{PROP_PRESENTATION_CATALOG_PATH}.asset.");
        }

        return propPresentationCatalog;
    }

    public static bool TryGetPropPresentationEntry(string propName, out PropPresentationEntry entry)
    {
        LoadPropPresentationCatalog();
        PropPresentationCatalogSO catalog = propPresentationCatalog;
        if (catalog == null)
        {
            entry = default;
            return false;
        }

        return catalog.TryGetEntry(propName, out entry);
    }

    public static bool TryGetPropPresentationEntry(PropType propType, out PropPresentationEntry entry)
    {
        return TryGetPropPresentationEntry(propType.ToString(), out entry);
    }

    public static StageDefinitionSO GetStageDefinition()
    {
        LoadStageDefinition();
        return stageDefinition;
    }

    public static AccessoryDataSO GetAccessory(string accessoryId)
    {
        LoadAccessoryData();
        if (string.IsNullOrEmpty(accessoryId))
        {
            Debug.LogWarning("AccessoryId cannot be null or empty.");
            return null;
        }

        for (int i = 0; i < Accessories.Length; i++)
        {
            AccessoryDataSO accessory = Accessories[i];
            if (accessory != null && accessory.AccessoryId == accessoryId)
            {
                return accessory;
            }
        }

        return null;
    }

    public static AccessoryDataSO GetRandomAccessory()
    {
        LoadAccessoryData();
        if (Accessories == null || Accessories.Length == 0)
        {
            Debug.LogWarning("No accessories available.");
            return null;
        }

        return Accessories[UnityEngine.Random.Range(0, Accessories.Length)];
    }

    public static AccessoryDataSO GetRandomAccessoryByRarity(int rarity)
    {
        LoadAccessoryData();
        AccessoryDataSO[] filteredAccessories = Array.FindAll(
            Accessories,
            accessory => accessory != null && accessory.Rarity == rarity);

        if (filteredAccessories.Length == 0)
        {
            Debug.LogWarning($"No accessories found with rarity {rarity}.");
            return null;
        }

        return filteredAccessories[UnityEngine.Random.Range(0, filteredAccessories.Length)];
    }

    public static Material GetItemQualityIconEffectMaterial()
    {
        if (cachedItemQualityIconEffectMaterial == null)
        {
            cachedItemQualityIconEffectMaterial = Resources.Load<Material>(ITEM_QUALITY_ICON_EFFECT_MATERIAL_PATH);
        }

        return cachedItemQualityIconEffectMaterial;
    }

}
