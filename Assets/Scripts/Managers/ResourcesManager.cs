using System;
using System.Collections.Generic;
using UnityEngine;
using UniversalUI.Integration.Game.ScriptableObjects;

public static class ResourcesManager
{
    private const string PROP_ICONS_DATA_PATH = "Data/Prop Icons";
    private const string ACCESSORY_DATA_PATH = "Data/Accessory Data List";
    private const string WEAPON_DATA_PATH = "Data/Weapon Data List";
    private const string CHARACTER_DATA_PATH = "Data/Characters";
    private const string PLAYER_LEVEL_CONFIG_DATA_PATH = "Data/Player Level Config";
    private const string STAGE_DEFINITION_DATA_PATH = "Data/Waves/Stage Definition";
    private const string ENEMY_TEMPLATE_CATALOG_DATA_PATH = "Data/Enemies/Enemy Template Catalog";
    private const string PROJECTILE_COMMON_PREFAB_PATH = "Prefabs/Projectiles/Projectile Common";
    private const string PLAYER_PREFAB_PATH = "Prefabs/Player";
    private const string DEFAULT_PLAYER_PREFAB_NAME = "Character";

    private static PropIcon[] propIcons;
    private static AccessoryDataSO[] Accessories;
    private static WeaponDataSO[] Weapons;
    private static CharacterDataSO[] characters;
    private static PlayerLevelConfigSO playerLevelConfig;
    private static StageDefinitionSO stageDefinition;
    private static EnemyTemplateCatalogSO enemyTemplateCatalog;
    private static readonly Dictionary<ProjectileTemplateKind, Projectile> projectilePrefabCache = new();

    public static Sprite GetPropIcon(PropType propType)
    {
        if (propIcons == null)
        {
            PropIconDataSO data = Resources.Load<PropIconDataSO>(PROP_ICONS_DATA_PATH);
            propIcons = data.PropIcons;
        }

        for (int i = 0; i < propIcons.Length; i++)
        {
            PropIcon propIcon = propIcons[i];
            if (propIcon.propType == propType)
            {
                return propIcon.icon;
            }
        }

        return null;
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

    private static void LoadStageDefinition()
    {
        if (stageDefinition == null)
        {
            stageDefinition = Resources.Load<StageDefinitionSO>(STAGE_DEFINITION_DATA_PATH);
        }
    }

    private static void LoadEnemyTemplateCatalog()
    {
        if (enemyTemplateCatalog == null)
        {
            enemyTemplateCatalog = Resources.Load<EnemyTemplateCatalogSO>(ENEMY_TEMPLATE_CATALOG_DATA_PATH);
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

    public static Projectile GetProjectilePrefab(ProjectileTemplateKind templateKind)
    {
        if (projectilePrefabCache.TryGetValue(templateKind, out Projectile cachedPrefab) && cachedPrefab != null)
        {
            return cachedPrefab;
        }

        Projectile loadedPrefab = templateKind switch
        {
            ProjectileTemplateKind.Common => LoadProjectilePrefab(PROJECTILE_COMMON_PREFAB_PATH),
            ProjectileTemplateKind.Piercing => LoadProjectilePrefab(PROJECTILE_COMMON_PREFAB_PATH),
            ProjectileTemplateKind.Homing => LoadProjectilePrefab(PROJECTILE_COMMON_PREFAB_PATH),
            ProjectileTemplateKind.Explosive => LoadProjectilePrefab(PROJECTILE_COMMON_PREFAB_PATH),
            ProjectileTemplateKind.Boomerang => LoadProjectilePrefab(PROJECTILE_COMMON_PREFAB_PATH),
            _ => throw new ArgumentOutOfRangeException(nameof(templateKind), templateKind, "Unsupported projectile template kind.")
        };

        projectilePrefabCache[templateKind] = loadedPrefab;
        return loadedPrefab;
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

    public static StageDefinitionSO GetStageDefinition()
    {
        LoadStageDefinition();
        return stageDefinition;
    }

    public static EnemyTemplateCatalogSO GetEnemyTemplateCatalog()
    {
        LoadEnemyTemplateCatalog();
        return enemyTemplateCatalog;
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

    private static Projectile LoadProjectilePrefab(string resourcePath)
    {
        Projectile prefab = Resources.Load<Projectile>(resourcePath);
        return prefab != null
            ? prefab
            : throw new MissingReferenceException($"{nameof(ResourcesManager)} requires a {nameof(Projectile)} resource at {resourcePath}.");
    }
}
