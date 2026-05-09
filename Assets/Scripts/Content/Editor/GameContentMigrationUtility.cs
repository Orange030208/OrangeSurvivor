#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 编辑器内容装配工具：补齐 GameContentCatalog 并生成所有运行时 ContentPool。
/// 设计为幂等操作，内容资产调整后可重复执行。
/// </summary>
public static class GameContentMigrationUtility
{
    private const string CatalogPath = "Assets/ScriptableObjects/Content/Game Content Catalog.asset";
    private const string DamageTextVisualConfigPath = "Assets/ScriptableObjects/Content/Damage Text Visual Config.asset";
    private const string MainScenePath = "Assets/Scenes/Game Scene.unity";
    private const string PoolFolder = "Assets/ScriptableObjects/Content/Pools";
    private const string UpgradeCardPoolPath = PoolFolder + "/Upgrade Card Pool.asset";
    private const string ChestRewardPoolPath = PoolFolder + "/Chest Reward Pool.asset";
    private const string ShopPoolPath = PoolFolder + "/Shop Pool.asset";
    private const string DropPoolPath = PoolFolder + "/Drop Pool.asset";
    private const string WaveSpawnPoolPath = PoolFolder + "/Wave Spawn Pool.asset";
    private const string WeaponRewardPoolPath = PoolFolder + "/Weapon Reward Pool.asset";
    private const float DefaultAccessoryWeight = 2f;
    private const float DefaultWeaponWeight = 1f;

    [MenuItem("Survivors/Content/Migrate Runtime Content Catalog")]
    public static void MigrateRuntimeContentCatalog()
    {
        EnsureFolders();
        ContentFactDefinitionAssetUtility.CreateBuiltInFactDefinitions();
        CreateOrUpdateContentPools();
        GameContentCatalogSO catalog = GetOrCreateCatalog();
        PopulateCatalog(catalog);
        BindBootstrap(catalog);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Migrated runtime content catalog at {CatalogPath}.");
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/ScriptableObjects");
        EnsureFolder("Assets/ScriptableObjects/Content");
        EnsureFolder(PoolFolder);
        EnsureFolder("Assets/Prefabs");
        EnsureFolder("Assets/Prefabs/Player");
        EnsureFolder("Assets/Prefabs/Weapons");
        EnsureFolder("Assets/Prefabs/Effects");
        EnsureFolder("Assets/Materials");
        EnsureFolder("Assets/Materials/UI");
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
        string folderName = Path.GetFileName(folderPath);
        if (!string.IsNullOrEmpty(parent))
        {
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }

    private static void CreateOrUpdateContentPools()
    {
        CreateOrUpdatePool(
            UpgradeCardPoolPath,
            ContentPoolPurpose.UpgradeCard,
            BuildUpgradeCardEntries(),
            3,
            false);
        CreateOrUpdatePool(
            ChestRewardPoolPath,
            ContentPoolPurpose.ChestReward,
            BuildChestRewardEntries(),
            3,
            false);
        CreateOrUpdatePool(
            ShopPoolPath,
            ContentPoolPurpose.Shop,
            BuildShopEntries(),
            4,
            false);
        CreateOrUpdatePool(
            DropPoolPath,
            ContentPoolPurpose.Drop,
            BuildDropEntries(),
            1,
            false);
        EnsureExistingPool(WaveSpawnPoolPath, ContentPoolPurpose.WaveSpawn, "Wave Spawn Pool must be authored directly; wave definitions no longer contain enemy candidates.");
        CreateOrUpdatePool(
            WeaponRewardPoolPath,
            ContentPoolPurpose.WeaponReward,
            BuildWeaponRewardEntries(),
            1,
            false);
    }

    private static ContentPoolSO CreateOrUpdatePool(
        string path,
        ContentPoolPurpose purpose,
        IReadOnlyList<ContentPoolEntry> entries,
        int rollCount,
        bool allowDuplicateResults)
    {
        UnityEngine.Object existingAsset = AssetDatabase.LoadMainAssetAtPath(path);
        ContentPoolSO pool = existingAsset as ContentPoolSO;
        if (existingAsset != null && pool == null)
        {
            AssetDatabase.DeleteAsset(path);
        }

        pool = AssetDatabase.LoadAssetAtPath<ContentPoolSO>(path);
        if (pool == null)
        {
            EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
            pool = ScriptableObject.CreateInstance<ContentPoolSO>();
            AssetDatabase.CreateAsset(pool, path);
        }

        pool.Initialize(purpose, entries, rollCount, allowDuplicateResults);
        EditorUtility.SetDirty(pool);
        return pool;
    }

    private static void EnsureExistingPool(string path, ContentPoolPurpose expectedPurpose, string missingMessage)
    {
        ContentPoolSO pool = AssetDatabase.LoadAssetAtPath<ContentPoolSO>(path);
        if (pool == null)
        {
            Debug.LogError($"{missingMessage} Missing asset at {path}.");
            return;
        }

        if (pool.Purpose != expectedPurpose)
        {
            Debug.LogError($"{path} expects purpose {expectedPurpose} but is {pool.Purpose}.", pool);
        }
    }

    private static List<ContentPoolEntry> BuildUpgradeCardEntries()
    {
        List<ContentPoolEntry> entries = new();
        foreach (UpgradeCardSO card in LoadAssets<UpgradeCardSO>("Assets/ScriptableObjects/Content/Upgrade Cards"))
        {
            if (card == null)
            {
                continue;
            }

            ContentPoolEntry entry = UpgradeCardContentPoolTuningUtility.CreateEntry(card);
            if (entry != null)
            {
                entries.Add(entry);
            }
        }

        return entries;
    }

    private static List<ContentPoolEntry> BuildChestRewardEntries()
    {
        List<ContentPoolEntry> entries = new();
        foreach (AccessoryDataSO accessory in LoadAssets<AccessoryDataSO>("Assets/ScriptableObjects/Content/Accessories"))
        {
            if (accessory == null)
            {
                continue;
            }

            ContentPoolEntry entry = new(accessory, DefaultAccessoryWeight, accessory.AccessoryId);
            entry.ConfigureRuntimeMetadata(0, 0, accessory.Rarity, 1f);
            entries.Add(entry);
        }

        return entries;
    }

    private static List<ContentPoolEntry> BuildShopEntries()
    {
        List<ContentPoolEntry> entries = BuildChestRewardEntries();
        foreach (WeaponDataSO weapon in LoadAssets<WeaponDataSO>("Assets/ScriptableObjects/Content/Weapons"))
        {
            if (weapon == null)
            {
                continue;
            }

            ContentPoolEntry entry = new(weapon, DefaultWeaponWeight, weapon.ItemName);
            entry.ConfigureRuntimeMetadata(WeaponLevelHelper.MinLevel, WeaponLevelHelper.MaxLevel, 0, 1f);
            entries.Add(entry);
        }

        return entries;
    }

    private static List<ContentPoolEntry> BuildDropEntries()
    {
        List<ContentPoolEntry> entries = new();
        foreach (CollectionSO collection in LoadAssets<CollectionSO>("Assets/ScriptableObjects/Content/Collections"))
        {
            if (collection == null)
            {
                continue;
            }

            string lowerName = collection.name.ToLowerInvariant();
            bool isChest = lowerName.Contains("chest");
            float weight = isChest ? 0.1f : 0.9f;
            ContentPoolEntry entry = new(collection, weight, collection.name);
            if (isChest)
            {
                entry.ConfigureRuntimeRules(
                    null,
                    new[] { new FactScaleWeightContentRule(LoadFact("Luck.asset"), 0.02f, 0f, 5f) });
            }

            entries.Add(entry);
        }

        return entries;
    }

    private static List<ContentPoolEntry> BuildWeaponRewardEntries()
    {
        List<ContentPoolEntry> entries = new();
        foreach (WeaponDataSO weapon in LoadAssets<WeaponDataSO>("Assets/ScriptableObjects/Content/Weapons"))
        {
            if (weapon == null)
            {
                continue;
            }

            ContentPoolEntry entry = new(weapon, DefaultWeaponWeight, weapon.ItemName);
            entry.ConfigureRuntimeMetadata(WeaponLevelHelper.MinLevel, WeaponLevelHelper.MaxLevel, 0, 1f);
            entries.Add(entry);
        }

        return entries;
    }

    private static FactDefinitionSO LoadFact(string fileName)
    {
        return AssetDatabase.LoadAssetAtPath<FactDefinitionSO>($"Assets/ScriptableObjects/Content/Facts/{fileName}");
    }

    private static IReadOnlyList<T> LoadAssets<T>(params string[] folders) where T : UnityEngine.Object
    {
        string[] existingFolders = GetExistingFolders(folders);
        if (existingFolders.Length == 0)
        {
            return System.Array.Empty<T>();
        }

        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", existingFolders);
        List<T> assets = new();
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                assets.Add(asset);
            }
        }

        assets.Sort((left, right) => string.Compare(left.name, right.name, System.StringComparison.Ordinal));
        return assets;
    }

    private static GameContentCatalogSO GetOrCreateCatalog()
    {
        GameContentCatalogSO catalog = AssetDatabase.LoadAssetAtPath<GameContentCatalogSO>(CatalogPath);
        if (catalog != null)
        {
            return catalog;
        }

        catalog = ScriptableObject.CreateInstance<GameContentCatalogSO>();
        AssetDatabase.CreateAsset(catalog, CatalogPath);
        return catalog;
    }

    private static void PopulateCatalog(GameContentCatalogSO catalog)
    {
        SerializedObject serializedObject = new(catalog);
        // 使用 SerializedObject 写入私有序列化字段，避免为了迁移暴露额外 public setter。
        StageDefinitionSO stageDefinition = LoadRequired<StageDefinitionSO>("Assets/ScriptableObjects/Content/Stage Definition.asset");
        SetObject(serializedObject, "weaponDataList", LoadRequired<WeaponDataListSO>("Assets/ScriptableObjects/Content/Weapon Data List.asset"));
        SetObject(serializedObject, "accessoryDataList", LoadRequired<AccessoryDataListSO>("Assets/ScriptableObjects/Content/Accessory Data List.asset"));
        SetObject(serializedObject, "playerLevelConfig", LoadRequired<PlayerLevelConfigSO>("Assets/ScriptableObjects/Content/Player Level Config.asset"));
        SetObject(serializedObject, "upgradeCardPool", LoadRequired<ContentPoolSO>(UpgradeCardPoolPath));
        SetObject(serializedObject, "defaultStageDefinition", stageDefinition);
        SetObject(serializedObject, "chestRewardPool", LoadRequired<ContentPoolSO>(ChestRewardPoolPath));
        SetObject(serializedObject, "shopPool", LoadRequired<ContentPoolSO>(ShopPoolPath));
        SetObject(serializedObject, "dropPool", LoadRequired<ContentPoolSO>(DropPoolPath));
        SetObject(serializedObject, "waveSpawnPool", LoadRequired<ContentPoolSO>(WaveSpawnPoolPath));
        SetObject(serializedObject, "weaponRewardPool", LoadRequired<ContentPoolSO>(WeaponRewardPoolPath));
        SetObject(serializedObject, "defaultPlayerPrefab", LoadRequiredPrefabComponent<Player>("Assets/Prefabs/Player/Character.prefab"));
        SetObject(serializedObject, "defaultWeaponPrefab", LoadRequiredPrefabComponent<Weapon>("Assets/Prefabs/Weapons/Weapon.prefab"));
        SetObject(serializedObject, "damageTextPrefab", LoadRequiredPrefabComponent<DamageTextFlow>("Assets/Prefabs/Effects/Damage Text.prefab"));
        SetObject(serializedObject, "propPresentationCatalog", LoadRequired<PropPresentationCatalogSO>("Assets/ScriptableObjects/Content/Prop Presentation Catalog.asset"));
        SetObject(serializedObject, "cardQualityPresentationCatalog", LoadRequired<CardQualityPresentationCatalogSO>("Assets/ScriptableObjects/Content/Upgrade Card Rarity Presentation Catalog.asset"));
        SetObject(serializedObject, "itemQualityVisualConfig", LoadRequired<ItemQualityVisualConfigSO>("Assets/ScriptableObjects/Content/Item Quality Visual Config.asset"));
        SetObject(serializedObject, "damageTextVisualConfig", GetOrCreateDamageTextVisualConfig());
        SetObject(serializedObject, "itemQualityIconEffectMaterial", LoadRequired<Material>("Assets/Materials/UI/WeaponQualityIconEffect.mat"));
        SetCharacters(serializedObject);
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(catalog);
    }

    private static void SetCharacters(SerializedObject serializedObject)
    {
        string[] searchFolders = GetExistingFolders(
            "Assets/ScriptableObjects/Content/Characters");
        string[] guids = AssetDatabase.FindAssets(
            "t:CharacterDataSO",
            searchFolders);
        List<CharacterDataSO> characters = new();
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            CharacterDataSO character = AssetDatabase.LoadAssetAtPath<CharacterDataSO>(path);
            if (character != null)
            {
                characters.Add(character);
            }
        }

        characters.Sort((left, right) => string.Compare(left.name, right.name, System.StringComparison.Ordinal));
        SerializedProperty property = serializedObject.FindProperty("characters");
        property.arraySize = characters.Count;
        for (int i = 0; i < characters.Count; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = characters[i];
        }
    }

    private static string[] GetExistingFolders(params string[] folderPaths)
    {
        List<string> existingFolders = new();
        if (folderPaths == null)
        {
            return existingFolders.ToArray();
        }

        for (int i = 0; i < folderPaths.Length; i++)
        {
            string folderPath = folderPaths[i];
            if (!string.IsNullOrWhiteSpace(folderPath) && AssetDatabase.IsValidFolder(folderPath))
            {
                existingFolders.Add(folderPath);
            }
        }

        return existingFolders.ToArray();
    }

    private static T LoadRequired<T>(string path) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            Debug.LogError($"Missing required content asset at {path}.");
        }

        return asset;
    }

    private static T LoadRequiredPrefabComponent<T>(string path) where T : Component
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogError($"Missing required prefab asset at {path}.");
            return null;
        }

        T component = prefab.GetComponent<T>();
        if (component == null)
        {
            Debug.LogError($"Prefab at {path} is missing required component {typeof(T).Name}.");
        }

        return component;
    }

    private static DamageTextVisualConfigSO GetOrCreateDamageTextVisualConfig()
    {
        DamageTextVisualConfigSO config =
            AssetDatabase.LoadAssetAtPath<DamageTextVisualConfigSO>(DamageTextVisualConfigPath);
        if (config != null)
        {
            return config;
        }

        config = ScriptableObject.CreateInstance<DamageTextVisualConfigSO>();
        config.name = Path.GetFileNameWithoutExtension(DamageTextVisualConfigPath);
        AssetDatabase.CreateAsset(config, DamageTextVisualConfigPath);
        EditorUtility.SetDirty(config);
        return config;
    }

    private static void SetObject(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
    {
        serializedObject.FindProperty(propertyName).objectReferenceValue = value;
    }

    private static void BindBootstrap(GameContentCatalogSO catalog)
    {
        Scene scene = EditorSceneManager.GetSceneByPath(MainScenePath);
        bool openedForMigration = !scene.IsValid() || !scene.isLoaded;
        if (openedForMigration)
        {
            scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Additive);
        }

        GameContentBootstrap bootstrap = FindBootstrapInScene(scene);
        if (bootstrap == null)
        {
            // 主场景显式挂 Bootstrap，不再依靠隐藏的运行时路径加载。
            GameObject bootstrapObject = new(nameof(GameContentBootstrap));
            SceneManager.MoveGameObjectToScene(bootstrapObject, scene);
            bootstrap = bootstrapObject.AddComponent<GameContentBootstrap>();
        }

        SerializedObject serializedObject = new(bootstrap);
        serializedObject.FindProperty("catalog").objectReferenceValue = catalog;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(bootstrap);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        if (openedForMigration)
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static GameContentBootstrap FindBootstrapInScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return null;
        }

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            GameContentBootstrap bootstrap = roots[i].GetComponentInChildren<GameContentBootstrap>(true);
            if (bootstrap != null)
            {
                return bootstrap;
            }
        }

        return null;
    }
}
#endif
