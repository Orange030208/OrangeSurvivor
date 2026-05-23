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
public static class GameContentCatalogBuildUtility
{
    private const float LuckWeightDivisor = 250f;
    private const string CatalogPath = GameContentAssetPaths.GameContentCatalog;
    private const string DamageTextVisualConfigPath = GameContentAssetPaths.DamageTextVisualConfig;
    private const string MainScenePath = "Assets/Scenes/Game Scene.unity";
    private const string UpgradeCardPoolPath = GameContentAssetPaths.UpgradeCardPool;
    private const string ChestRewardPoolPath = GameContentAssetPaths.ChestRewardPool;
    private const string ShopPoolPath = GameContentAssetPaths.ShopPool;
    private const string DropPoolPath = GameContentAssetPaths.DropPool;
    private const string WaveSpawnPoolPath = GameContentAssetPaths.WaveSpawnPool;
    private const string WeaponRewardPoolPath = GameContentAssetPaths.WeaponRewardPool;
    private const float DefaultAccessoryWeight = 2f;

    [MenuItem("Survivors/Content/Rebuild Runtime Content Catalog")]
    public static void RebuildRuntimeContentCatalog()
    {
        EnsureFolders();
        CreateOrUpdateContentPools();
        GameContentCatalogSO catalog = GetOrCreateCatalog();
        PopulateCatalog(catalog);
        BindBootstrap(catalog);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Rebuilt runtime content catalog at {CatalogPath}.");
    }

    private static void EnsureFolders()
    {
        EnsureFolder(GameContentAssetPaths.Root);
        EnsureFolder(GameContentAssetPaths.CatalogFolder);
        EnsureFolder(GameContentAssetPaths.CatalogPools);
        EnsureFolder(GameContentAssetPaths.RunProgression);
        EnsureFolder(GameContentAssetPaths.Characters);
        EnsureFolder(GameContentAssetPaths.Weapons);
        EnsureFolder(GameContentAssetPaths.Accessories);
        EnsureFolder(GameContentAssetPaths.Upgrades);
        EnsureFolder(GameContentAssetPaths.Waves);
        EnsureFolder(GameContentAssetPaths.Enemies);
        EnsureFolder(GameContentAssetPaths.Collections);
        EnsureFolder(GameContentAssetPaths.UI);
        EnsureFolder(GameContentAssetPaths.Audio);
        EnsureFolder(GameContentAssetPaths.Combat);
        EnsureFolder(GameContentAssetPaths.Map);
        EnsureFolder(GameContentAssetPaths.UpgradePools);
        EnsureFolder(GameContentAssetPaths.UpgradePresentation);
        EnsureFolder(GameContentAssetPaths.WavePools);
        EnsureFolder(GameContentAssetPaths.CombatData);
        EnsureFolder(GameContentAssetPaths.CombatMaterials);
        EnsureFolder(GameContentAssetPaths.CombatVfx);
        EnsureFolder(GameContentAssetPaths.CombatVfxPrefabs);
        EnsureFolder(GameContentAssetPaths.UIData);
        EnsureFolder(GameContentAssetPaths.UIMaterials);
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
            BuildUpgradeCardEntries(),
            4,
            false);
        CreateOrUpdatePool(
            ChestRewardPoolPath,
            BuildChestRewardEntries(),
            3,
            false);
        CreateOrUpdatePool(
            ShopPoolPath,
            BuildShopEntries(),
            4,
            false);
        CreateOrUpdatePool(
            DropPoolPath,
            BuildDropEntries(),
            1,
            false);
        EnsureExistingPool(WaveSpawnPoolPath, "Wave Spawn Pool must be authored directly; wave definitions no longer contain enemy candidates.");
        CreateOrUpdatePool(
            WeaponRewardPoolPath,
            BuildWeaponRewardEntries(),
            1,
            false);
    }

    private static ContentPoolSO CreateOrUpdatePool(
        string path,
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

        pool.Initialize(entries, rollCount, allowDuplicateResults);
        EditorUtility.SetDirty(pool);
        return pool;
    }

    private static void EnsureExistingPool(string path, string missingMessage)
    {
        ContentPoolSO pool = AssetDatabase.LoadAssetAtPath<ContentPoolSO>(path);
        if (pool == null)
        {
            Debug.LogError($"{missingMessage} Missing asset at {path}.");
            return;
        }
    }

    private static List<ContentPoolEntry> BuildChestRewardEntries()
    {
        List<ContentPoolEntry> entries = new();
        foreach (AccessoryDataSO accessory in LoadAssets<AccessoryDataSO>(GameContentAssetPaths.AccessoriesData))
        {
            if (accessory == null)
            {
                continue;
            }

            ContentPoolEntry entry = new(accessory, DefaultAccessoryWeight, accessory.AccessoryId);
            entry.ConfigureRuntimeMetadata(new ContentEntryMetadata[]
            {
                new QualityMetadata(accessory.Rarity)
            });
            entry.ConfigureRuntimeRules(
                new ContentCondition[] { new AccessoryOwnedLimitCondition() },
                new[] { CreateLuckWeightRule(GetAccessoryLuckCoefficient(accessory.RarityGrade), 0.5f) });
            entries.Add(entry);
        }

        return entries;
    }

    private static List<ContentPoolEntry> BuildShopEntries()
    {
        List<ContentPoolEntry> entries = BuildChestRewardEntries();
        IReadOnlyDictionary<string, WeaponJsonWeapon> weaponRowsById = LoadWeaponRowsById();
        foreach (WeaponDataSO weapon in LoadAssets<WeaponDataSO>(GameContentAssetPaths.WeaponsData))
        {
            if (weapon == null)
            {
                continue;
            }

            WeaponJsonWeapon row = ResolveWeaponRow(weaponRowsById, weapon);
            for (int level = WeaponLevelHelper.MinLevel; level <= WeaponLevelHelper.MaxLevel; level++)
            {
                ContentPoolEntry entry = WeaponContentPoolTuningUtility.CreateShopEntry(
                    weapon,
                    level,
                    ResolveWeaponOpenWave(row),
                    ResolveWeaponCloseWave(row));
                if (entry == null)
                {
                    continue;
                }

                entry.ConfigureRuntimeRules(
                    WeaponContentPoolTuningUtility.BuildAvailabilityConditions(
                        ResolveWeaponOpenWave(row),
                        ResolveWeaponCloseWave(row)),
                    new[] { CreateLuckWeightRule(GetWeaponLevelLuckCoefficient(level), 0.5f) });
                entries.Add(entry);
            }
        }

        return entries;
    }

    private static List<ContentPoolEntry> BuildWeaponRewardEntries()
    {
        List<ContentPoolEntry> entries = new();
        IReadOnlyDictionary<string, WeaponJsonWeapon> weaponRowsById = LoadWeaponRowsById();
        foreach (WeaponDataSO weapon in LoadAssets<WeaponDataSO>(GameContentAssetPaths.WeaponsData))
        {
            if (weapon == null)
            {
                continue;
            }

            WeaponJsonWeapon row = ResolveWeaponRow(weaponRowsById, weapon);
            ContentPoolEntry entry = WeaponContentPoolTuningUtility.CreateRewardEntry(
                weapon,
                ResolveWeaponBaseWeight(row),
                ResolveWeaponOpenWave(row),
                ResolveWeaponCloseWave(row));
            if (entry == null)
            {
                continue;
            }

            entries.Add(entry);
        }

        return entries;
    }

    private static IReadOnlyDictionary<string, WeaponJsonWeapon> LoadWeaponRowsById()
    {
        Dictionary<string, WeaponJsonWeapon> weaponRowsById = new(System.StringComparer.Ordinal);
        try
        {
            IReadOnlyList<WeaponJsonWeapon> rows = WeaponJsonReader.ReadDefault();
            for (int i = 0; i < rows.Count; i++)
            {
                WeaponJsonWeapon row = rows[i];
                if (row == null || string.IsNullOrWhiteSpace(row.weaponId))
                {
                    continue;
                }

                weaponRowsById[row.weaponId] = row;
            }
        }
        catch (DataImportException exception)
        {
            Debug.LogWarning(
                $"Weapon JSON tuning could not be loaded for content pool generation. " +
                $"Using default weapon pool tuning. {exception.Message}");
        }

        return weaponRowsById;
    }

    private static WeaponJsonWeapon ResolveWeaponRow(
        IReadOnlyDictionary<string, WeaponJsonWeapon> rowsById,
        WeaponDataSO weapon)
    {
        if (rowsById == null || weapon == null)
        {
            return null;
        }

        return rowsById.TryGetValue(weapon.WeaponId, out WeaponJsonWeapon row)
            ? row
            : null;
    }

    private static float ResolveWeaponBaseWeight(WeaponJsonWeapon row)
    {
        return row != null
            ? Mathf.Max(0f, row.baseWeight)
            : WeaponContentPoolTuningUtility.DefaultRewardWeaponWeight;
    }

    private static int ResolveWeaponOpenWave(WeaponJsonWeapon row)
    {
        return row != null
            ? Mathf.Max(WeaponContentPoolTuningUtility.DefaultOpenWave, row.openWave)
            : WeaponContentPoolTuningUtility.DefaultOpenWave;
    }

    private static int ResolveWeaponCloseWave(WeaponJsonWeapon row)
    {
        return row != null
            ? Mathf.Max(WeaponContentPoolTuningUtility.DefaultCloseWave, row.closeWave)
            : WeaponContentPoolTuningUtility.DefaultCloseWave;
    }

    private static List<ContentPoolEntry> BuildUpgradeCardEntries()
    {
        List<ContentPoolEntry> entries = new();
        foreach (UpgradeCardSO card in LoadAssets<UpgradeCardSO>(GameContentAssetPaths.UpgradeCards))
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

    private static List<ContentPoolEntry> BuildDropEntries()
    {
        List<ContentPoolEntry> entries = new();
        foreach (CollectionSO collection in LoadAssets<CollectionSO>(GameContentAssetPaths.CollectionsData))
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
                    new[] { CreateLuckWeightRule(0.8f, 0.5f) });
            }

            entries.Add(entry);
        }

        return entries;
    }

    private static ContentWeightRule CreateLuckWeightRule(float coefficient, float minMultiplier)
    {
        return new PlayerPropertyScaleWeightRule(
            PropType.Luck,
            coefficient / LuckWeightDivisor,
            minMultiplier,
            0f);
    }

    private static float GetAccessoryLuckCoefficient(AccessoryRarity rarity)
    {
        return rarity switch
        {
            AccessoryRarity.Common => -0.4f,
            AccessoryRarity.Rare => 0.4f,
            AccessoryRarity.Epic => 0.9f,
            AccessoryRarity.Legendary => 1.4f,
            _ => 0f
        };
    }

    private static float GetWeaponLevelLuckCoefficient(int level)
    {
        return WeaponLevelHelper.ClampLevel(level) switch
        {
            1 => -0.3f,
            2 => 0.3f,
            3 => 0.8f,
            4 => 1.2f,
            _ => 0f
        };
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
        // 使用 SerializedObject 写入私有序列化字段，避免为了编辑器装配流程暴露额外 public setter。
        StageDefinitionSO stageDefinition = LoadRequired<StageDefinitionSO>(GameContentAssetPaths.StageDefinition);
        SetObjectArray(serializedObject, "weapons", LoadAssets<WeaponDataSO>(GameContentAssetPaths.WeaponsData));
        SetObject(serializedObject, "accessoryDataList", LoadRequired<AccessoryDataListSO>(GameContentAssetPaths.AccessoryDataList));
        SetDefaultCharacter(serializedObject);
        SetStarterCards(serializedObject);
        SetObject(serializedObject, "playerLevelConfig", LoadRequired<PlayerLevelConfigSO>(GameContentAssetPaths.PlayerLevelConfig));
        SetObject(serializedObject, "runProgressionProfile", GetOrCreateRunProgressionProfile());
        SetObject(serializedObject, "upgradeCardPool", LoadRequired<ContentPoolSO>(UpgradeCardPoolPath));
        SetObject(serializedObject, "defaultStageDefinition", stageDefinition);
        SetObject(serializedObject, "chestRewardPool", LoadRequired<ContentPoolSO>(ChestRewardPoolPath));
        SetObject(serializedObject, "shopPool", LoadRequired<ContentPoolSO>(ShopPoolPath));
        SetObject(serializedObject, "dropPool", LoadRequired<ContentPoolSO>(DropPoolPath));
        SetObject(serializedObject, "waveSpawnPool", LoadRequired<ContentPoolSO>(WaveSpawnPoolPath));
        SetObject(serializedObject, "weaponRewardPool", LoadRequired<ContentPoolSO>(WeaponRewardPoolPath));
        SetObject(serializedObject, "defaultPlayerPrefab", LoadRequiredPrefabComponent<Player>(GameContentAssetPaths.DefaultPlayerPrefab));
        SetObject(serializedObject, "defaultWeaponPrefab", LoadRequiredPrefabComponent<Weapon>(GameContentAssetPaths.DefaultWeaponPrefab));
        SetObject(serializedObject, "damageTextPrefab", LoadRequiredPrefabComponent<DamageTextFlow>(GameContentAssetPaths.DamageTextPrefab));
        SetObject(serializedObject, "propPresentationCatalog", LoadRequired<PropPresentationCatalogSO>(GameContentAssetPaths.PropPresentationCatalog));
        SetObject(serializedObject, "cardQualityPresentationCatalog", LoadRequired<CardQualityPresentationCatalogSO>(GameContentAssetPaths.CardQualityPresentationCatalog));
        SetObject(serializedObject, "itemQualityVisualConfig", LoadRequired<ItemQualityVisualConfigSO>(GameContentAssetPaths.ItemQualityVisualConfig));
        SetObject(serializedObject, "damageTextVisualConfig", GetOrCreateDamageTextVisualConfig());
        SetObject(serializedObject, "itemQualityIconEffectMaterial", LoadRequired<Material>(GameContentAssetPaths.ItemQualityIconEffectMaterial));
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(catalog);
    }

    private static void SetDefaultCharacter(SerializedObject serializedObject)
    {
        SerializedProperty defaultCharacterProperty = serializedObject.FindProperty("defaultCharacter");
        if (defaultCharacterProperty != null && defaultCharacterProperty.objectReferenceValue != null)
        {
            return;
        }

        string[] searchFolders = GetExistingFolders(
            GameContentAssetPaths.CharactersData);
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
        if (defaultCharacterProperty != null && characters.Count > 0)
        {
            defaultCharacterProperty.objectReferenceValue = characters[0];
        }
    }

    private static void SetStarterCards(SerializedObject serializedObject)
    {
        SerializedProperty property = serializedObject.FindProperty("starterCards");
        if (property == null || property.arraySize > 0)
        {
            return;
        }

        string[] starterCardNames =
        {
            "Upgrade_Damage_Common",
            "Upgrade_MaxHealth_Common",
            "Upgrade_MoveSpeed_Common",
            "Upgrade_AttackSpeed_Common"
        };

        List<UpgradeCardSO> starterCards = new();
        for (int i = 0; i < starterCardNames.Length; i++)
        {
            UpgradeCardSO card = LoadFirstAsset<UpgradeCardSO>(
                GameContentAssetPaths.UpgradeCards,
                starterCardNames[i]);
            if (card != null)
            {
                starterCards.Add(card);
            }
        }

        property.arraySize = starterCards.Count;
        for (int i = 0; i < starterCards.Count; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = starterCards[i];
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

    private static T LoadFirstAsset<T>(string folderPath, string assetName) where T : UnityEngine.Object
    {
        if (string.IsNullOrWhiteSpace(folderPath) || string.IsNullOrWhiteSpace(assetName))
        {
            return null;
        }

        string[] searchFolders = GetExistingFolders(folderPath);
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name} {assetName}", searchFolders);
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (Path.GetFileNameWithoutExtension(path) == assetName)
            {
                return AssetDatabase.LoadAssetAtPath<T>(path);
            }
        }

        Debug.LogWarning($"Could not find asset '{assetName}' of type {typeof(T).Name} in {folderPath}.");
        return null;
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

    private static RunProgressionProfileSO GetOrCreateRunProgressionProfile()
    {
        RunProgressionProfileSO profile =
            AssetDatabase.LoadAssetAtPath<RunProgressionProfileSO>(GameContentAssetPaths.RunProgressionProfile);
        if (profile != null)
        {
            return profile;
        }

        EnsureFolder(GameContentAssetPaths.RunProgression);
        profile = ScriptableObject.CreateInstance<RunProgressionProfileSO>();
        profile.name = Path.GetFileNameWithoutExtension(GameContentAssetPaths.RunProgressionProfile);
        AssetDatabase.CreateAsset(profile, GameContentAssetPaths.RunProgressionProfile);
        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static void SetObject(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
    {
        serializedObject.FindProperty(propertyName).objectReferenceValue = value;
    }

    private static void SetObjectArray<T>(SerializedObject serializedObject, string propertyName, IReadOnlyList<T> values)
        where T : UnityEngine.Object
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogError($"Missing serialized property '{propertyName}' on {serializedObject.targetObject}.");
            return;
        }

        int count = values != null ? values.Count : 0;
        property.arraySize = count;
        for (int i = 0; i < count; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }

    private static void BindBootstrap(GameContentCatalogSO catalog)
    {
        Scene scene = EditorSceneManager.GetSceneByPath(MainScenePath);
        bool openedForBuild = !scene.IsValid() || !scene.isLoaded;
        if (openedForBuild)
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
        if (openedForBuild)
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
