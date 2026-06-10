#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 编辑器内容装配工具：补齐 GameContentCatalog，并绑定抽取系统所需的显式内容列表与配置资产。
/// 设计为幂等操作，内容资产调整后可重复执行。
/// </summary>
public static class GameContentCatalogBuildUtility
{
    private const string CatalogPath = GameContentAssetPaths.GameContentCatalog;
    private const string DamageTextVisualConfigPath = GameContentAssetPaths.DamageTextVisualConfig;
    private const string MainScenePath = "Assets/Scenes/Game Scene.unity";

    [MenuItem("Survivors/Content/Rebuild Runtime Content Catalog")]
    public static void RebuildRuntimeContentCatalog()
    {
        EnsureFolders();
        GameContentCatalogSO catalog = GetOrCreateCatalog();
        PopulateCatalog(catalog);
        BindBootstrap(catalog);
        CleanupLegacyWaveContent();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Rebuilt runtime content catalog at {CatalogPath}.");
    }

    private static void EnsureFolders()
    {
        EnsureFolder(GameContentAssetPaths.Root);
        EnsureFolder(GameContentAssetPaths.CatalogFolder);
        EnsureFolder(GameContentAssetPaths.Tier);
        EnsureFolder(GameContentAssetPaths.RunProgression);
        EnsureFolder(GameContentAssetPaths.Characters);
        EnsureFolder(GameContentAssetPaths.Weapons);
        EnsureFolder(GameContentAssetPaths.Accessories);
        EnsureFolder(GameContentAssetPaths.Upgrades);
        EnsureFolder(GameContentAssetPaths.Waves);
        EnsureFolder(GameContentAssetPaths.WavesDirector);
        EnsureFolder(GameContentAssetPaths.WavesDirectorFinite);
        EnsureFolder(GameContentAssetPaths.WavesDirectorEndless);
        EnsureFolder(GameContentAssetPaths.Enemies);
        EnsureFolder(GameContentAssetPaths.Collections);
        EnsureFolder(GameContentAssetPaths.CollectionProfiles);
        EnsureFolder(GameContentAssetPaths.UI);
        EnsureFolder(GameContentAssetPaths.Audio);
        EnsureFolder(GameContentAssetPaths.Combat);
        EnsureFolder(GameContentAssetPaths.Map);
        EnsureFolder(GameContentAssetPaths.UpgradePresentation);
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

    private static IReadOnlyList<T> LoadAssets<T>(params string[] folders)
        where T : UnityEngine.Object
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
        IReadOnlyList<EnemySO> enemies = LoadAssets<EnemySO>(GameContentAssetPaths.EnemiesData);
        SetObjectArray(serializedObject, "weapons", LoadAssets<WeaponDataSO>(GameContentAssetPaths.WeaponsData));
        SetObject(serializedObject, "accessoryDataList", LoadRequired<AccessoryDataListSO>(GameContentAssetPaths.AccessoryDataList));
        SetDefaultCharacter(serializedObject);
        SetObjectArray(serializedObject, "rewardCards", LoadAssets<RewardCardSO>(GameContentAssetPaths.UpgradeCards));
        SetStarterCards(serializedObject);
        SetObjectArray(serializedObject, "collections", LoadAssets<CollectionSO>(GameContentAssetPaths.CollectionsData));
        SetObjectArray(serializedObject, "enemies", enemies);
        SetObjectArray(serializedObject, "buffs", LoadAssets<BuffDataSO>(GameContentAssetPaths.CombatBuffs));
        SetObject(serializedObject, "playerLevelConfig", LoadRequired<PlayerLevelConfigSO>(GameContentAssetPaths.PlayerLevelConfig));
        SetObject(serializedObject, "runProgressionProfile", GetOrCreateRunProgressionProfile());
        SetObject(serializedObject, "defaultStageDirectorProfile", GetOrCreateStageDirectorProfile(enemies));
        SetObject(serializedObject, "contentTierWeightProfile", LoadRequired<ContentTierWeightProfileSO>(GameContentAssetPaths.ContentTierWeightProfile));
        SetObject(serializedObject, "dropCollectionProfile", LoadRequired<DropCollectionProfileSO>(GameContentAssetPaths.DefaultDropCollectionProfile));
        SetObject(serializedObject, "defaultPlayerPrefab", LoadRequiredPrefabComponent<Player>(GameContentAssetPaths.DefaultPlayerPrefab));
        SetObject(serializedObject, "defaultWeaponPrefab", LoadRequiredPrefabComponent<Weapon>(GameContentAssetPaths.DefaultWeaponPrefab));
        SetObject(serializedObject, "damageTextPrefab", LoadRequiredPrefabComponent<DamageTextFlow>(GameContentAssetPaths.DamageTextPrefab));
        SetObject(serializedObject, "propPresentationCatalog", LoadRequired<PropPresentationCatalogSO>(GameContentAssetPaths.PropPresentationCatalog));
        SetObject(serializedObject, "damageTextVisualConfig", GetOrCreateDamageTextVisualConfig());
        SetObject(serializedObject, "itemQualityIconEffectMaterial", LoadRequired<Material>(GameContentAssetPaths.ItemQualityIconEffectMaterial));
        SetObject(serializedObject, "tierColorPalette", LoadRequired<TierColorPaletteSO>(GameContentAssetPaths.TierColorPalette));
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

        string[] searchFolders = GetExistingFolders(GameContentAssetPaths.CharactersData);
        string[] guids = AssetDatabase.FindAssets("t:CharacterDataSO", searchFolders);
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

        List<RewardCardSO> starterCards = new();
        for (int i = 0; i < starterCardNames.Length; i++)
        {
            RewardCardSO card = LoadFirstAsset<RewardCardSO>(GameContentAssetPaths.UpgradeCards, starterCardNames[i]);
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

    private static T LoadRequired<T>(string path)
        where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            Debug.LogError($"Missing required content asset at {path}.");
        }

        return asset;
    }

    private static T LoadFirstAsset<T>(string folderPath, string assetName)
        where T : UnityEngine.Object
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

    private static T LoadRequiredPrefabComponent<T>(string path)
        where T : Component
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

    private static StageDirectorProfileSO GetOrCreateStageDirectorProfile(IReadOnlyList<EnemySO> enemies)
    {
        StageDirectorProfileSO profile =
            AssetDatabase.LoadAssetAtPath<StageDirectorProfileSO>(GameContentAssetPaths.StageDirectorProfile);
        if (profile == null)
        {
            EnsureFolder(GameContentAssetPaths.WavesDirector);
            profile = ScriptableObject.CreateInstance<StageDirectorProfileSO>();
            profile.name = Path.GetFileNameWithoutExtension(GameContentAssetPaths.StageDirectorProfile);
            AssetDatabase.CreateAsset(profile, GameContentAssetPaths.StageDirectorProfile);
        }

        BuildStageDirectorContent(profile, enemies);
        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static void BuildStageDirectorContent(StageDirectorProfileSO stageProfile, IReadOnlyList<EnemySO> enemies)
    {
        EnsureFolder(GameContentAssetPaths.WavesDirectorFinite);
        EnsureFolder(GameContentAssetPaths.WavesDirectorEndless);

        EnemyDirectorTemplates templates = BuildEnemyDirectorTemplates(enemies);
        WaveDirectorDefinitionSO[] finiteWaves =
        {
            GetOrCreateFiniteWave("Wave Director 01", wave =>
                wave.Configure(
                    "DirectorWave_01",
                    "Wave 1",
                    30f,
                    WaveCompletionMode.TimerOnly,
                    12f,
                    6f,
                    AnimationCurve.Linear(0f, 0f, 1f, 1f),
                    new[] { new SpawnRoleTarget(SpawnRole.Melee, 1f) },
                    BuildRoster(templates.MeleeEntries, 1f, 1, 2, 0f, 0, 0f, 100f),
                    System.Array.Empty<ScriptedSpawnBeat>())),
            GetOrCreateFiniteWave("Wave Director 02", wave =>
                wave.Configure(
                    "DirectorWave_02",
                    "Wave 2",
                    35f,
                    WaveCompletionMode.TimerOnly,
                    16f,
                    8f,
                    CreatePacingCurve(0f, 0f, 0.35f, 0.2f, 1f, 1f),
                    new[]
                    {
                        new SpawnRoleTarget(SpawnRole.Melee, 0.75f),
                        new SpawnRoleTarget(SpawnRole.Elite, 0.25f)
                    },
                    BuildRoster(templates.MeleeAndEliteEntries, 1.1f, 1, 2, 1f, 0, 0f, 100f),
                    System.Array.Empty<ScriptedSpawnBeat>())),
            GetOrCreateFiniteWave("Wave Director 03", wave =>
                wave.Configure(
                    "DirectorWave_03",
                    "Wave 3",
                    40f,
                    WaveCompletionMode.TimerOnly,
                    20f,
                    9f,
                    CreatePacingCurve(0f, 0f, 0.3f, 0.15f, 1f, 1f),
                    new[]
                    {
                        new SpawnRoleTarget(SpawnRole.Melee, 0.65f),
                        new SpawnRoleTarget(SpawnRole.Ranged, 0.35f)
                    },
                    BuildRoster(templates.MeleeAndRangedEntries, 1.15f, 1, 2, 1f, 0, 0f, 100f),
                    System.Array.Empty<ScriptedSpawnBeat>())),
            GetOrCreateFiniteWave("Wave Director 04", wave =>
                wave.Configure(
                    "DirectorWave_04",
                    "Wave 4",
                    45f,
                    WaveCompletionMode.TimerOnly,
                    24f,
                    11f,
                    CreatePacingCurve(0f, 0f, 0.2f, 0.1f, 0.7f, 0.75f, 1f, 1f),
                    new[]
                    {
                        new SpawnRoleTarget(SpawnRole.Melee, 0.5f),
                        new SpawnRoleTarget(SpawnRole.Ranged, 0.25f),
                        new SpawnRoleTarget(SpawnRole.Elite, 0.25f)
                    },
                    BuildRoster(templates.MixedEntries, 1.2f, 1, 2, 1.2f, 0, 0f, 100f),
                    BuildEliteBurstBeats(templates.EliteAnchor))),
            GetOrCreateFiniteWave("Wave Director 05", wave =>
                wave.Configure(
                    "DirectorWave_05",
                    "Wave 5",
                    55f,
                    templates.BossAnchor != null ? WaveCompletionMode.BossDefeated : WaveCompletionMode.TimerOnly,
                    18f,
                    10f,
                    CreatePacingCurve(0f, 0f, 0.45f, 0.35f, 1f, 0.8f),
                    new[]
                    {
                        new SpawnRoleTarget(SpawnRole.Melee, 0.45f),
                        new SpawnRoleTarget(SpawnRole.Ranged, 0.2f),
                        new SpawnRoleTarget(SpawnRole.Elite, 0.35f)
                    },
                    BuildRoster(templates.MixedEntries, 1.25f, 1, 2, 1.1f, 0, 0f, 100f),
                    BuildBossBeat(templates.BossAnchor)))
        };

        EndlessPhaseCardSO hordePhase = GetOrCreateEndlessPhase("Endless Phase Horde", phase =>
            phase.Configure(
                "EndlessPhase_Horde",
                "Horde Rush",
                38f,
                WaveCompletionMode.TimerOnly,
                20f,
                10f,
                CreatePacingCurve(0f, 0f, 0.25f, 0.2f, 1f, 1f),
                new[]
                {
                    new SpawnRoleTarget(SpawnRole.Melee, 0.8f),
                    new SpawnRoleTarget(SpawnRole.Elite, 0.2f)
                },
                BuildRoster(templates.MeleeAndEliteEntries, 1.15f, 1, 3, 0.9f, 0, 0f, 100f),
                System.Array.Empty<ScriptedSpawnBeat>()));
        EndlessPhaseCardSO rangedPhase = GetOrCreateEndlessPhase("Endless Phase Ranged", phase =>
            phase.Configure(
                "EndlessPhase_Ranged",
                "Ranged Pressure",
                42f,
                WaveCompletionMode.TimerOnly,
                24f,
                11f,
                CreatePacingCurve(0f, 0f, 0.3f, 0.2f, 1f, 1f),
                new[]
                {
                    new SpawnRoleTarget(SpawnRole.Melee, 0.5f),
                    new SpawnRoleTarget(SpawnRole.Ranged, 0.35f),
                    new SpawnRoleTarget(SpawnRole.Elite, 0.15f)
                },
                BuildRoster(templates.MixedEntries, 1.2f, 1, 2, 1f, 0, 0f, 100f),
                System.Array.Empty<ScriptedSpawnBeat>()));
        EndlessPhaseCardSO elitePhase = GetOrCreateEndlessPhase("Endless Phase Elite", phase =>
            phase.Configure(
                "EndlessPhase_Elite",
                "Elite Check",
                46f,
                WaveCompletionMode.TimerOnly,
                28f,
                12f,
                CreatePacingCurve(0f, 0f, 0.25f, 0.15f, 0.8f, 0.85f, 1f, 1f),
                new[]
                {
                    new SpawnRoleTarget(SpawnRole.Melee, 0.4f),
                    new SpawnRoleTarget(SpawnRole.Ranged, 0.2f),
                    new SpawnRoleTarget(SpawnRole.Elite, 0.4f)
                },
                BuildRoster(templates.MixedEntries, 1.35f, 1, 2, 1.2f, 0, 0f, 100f),
                BuildEliteBurstBeats(templates.EliteAnchor)));

        EndlessDirectorProfileSO endlessProfile = GetOrCreateEndlessDirectorProfile();
        endlessProfile.Configure(
            new[] { hordePhase, rangedPhase, elitePhase },
            EndlessPhaseSelectionMode.Sequence,
            12345,
            new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(3f, 1.45f), new Keyframe(6f, 2.1f)),
            new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(6f, 1.2f)),
            new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(6f, 1.6f)),
            System.Array.Empty<EndlessRosterUnlockRule>(),
            BuildMilestoneBossBeat(templates.BossAnchor));
        EditorUtility.SetDirty(endlessProfile);

        stageProfile.Configure(
            StageDirectorMode.FiniteThenEndless,
            finiteWaves,
            endlessProfile,
            0.5f,
            3,
            SpawnLocationDefinition.CreateDefault());
    }

    private static WaveDirectorDefinitionSO GetOrCreateFiniteWave(string assetName, System.Action<WaveDirectorDefinitionSO> configure)
    {
        string path = $"{GameContentAssetPaths.WavesDirectorFinite}/{assetName}.asset";
        WaveDirectorDefinitionSO wave = AssetDatabase.LoadAssetAtPath<WaveDirectorDefinitionSO>(path);
        if (wave == null)
        {
            wave = ScriptableObject.CreateInstance<WaveDirectorDefinitionSO>();
            wave.name = assetName;
            AssetDatabase.CreateAsset(wave, path);
        }

        configure(wave);
        EditorUtility.SetDirty(wave);
        return wave;
    }

    private static EndlessPhaseCardSO GetOrCreateEndlessPhase(string assetName, System.Action<EndlessPhaseCardSO> configure)
    {
        string path = $"{GameContentAssetPaths.WavesDirectorEndless}/{assetName}.asset";
        EndlessPhaseCardSO phase = AssetDatabase.LoadAssetAtPath<EndlessPhaseCardSO>(path);
        if (phase == null)
        {
            phase = ScriptableObject.CreateInstance<EndlessPhaseCardSO>();
            phase.name = assetName;
            AssetDatabase.CreateAsset(phase, path);
        }

        configure(phase);
        EditorUtility.SetDirty(phase);
        return phase;
    }

    private static EndlessDirectorProfileSO GetOrCreateEndlessDirectorProfile()
    {
        string path = $"{GameContentAssetPaths.WavesDirectorEndless}/Endless Director Profile.asset";
        EndlessDirectorProfileSO profile = AssetDatabase.LoadAssetAtPath<EndlessDirectorProfileSO>(path);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<EndlessDirectorProfileSO>();
            profile.name = "Endless Director Profile";
            AssetDatabase.CreateAsset(profile, path);
        }

        return profile;
    }

    private static EnemyRosterEntry[] BuildRoster(
        IReadOnlyList<EnemyDirectorTemplate> templates,
        float costMultiplier,
        int minGroupSize,
        int maxGroupSize,
        float cooldownSeconds,
        int maxAlive,
        float startPercent,
        float endPercent)
    {
        if (templates == null || templates.Count == 0)
        {
            return System.Array.Empty<EnemyRosterEntry>();
        }

        EnemyRosterEntry[] roster = new EnemyRosterEntry[templates.Count];
        for (int i = 0; i < templates.Count; i++)
        {
            EnemyDirectorTemplate template = templates[i];
            roster[i] = new EnemyRosterEntry(
                $"{template.Enemy.name}_{template.Role}",
                template.Enemy,
                template.Role,
                template.Tags,
                template.Cost * costMultiplier,
                minGroupSize,
                maxGroupSize,
                cooldownSeconds,
                maxAlive,
                new Vector2(startPercent, endPercent));
        }

        return roster;
    }

    private static ScriptedSpawnBeat[] BuildEliteBurstBeats(EnemyDirectorTemplate? eliteTemplate)
    {
        if (eliteTemplate == null)
        {
            return System.Array.Empty<ScriptedSpawnBeat>();
        }

        EnemyDirectorTemplate template = eliteTemplate.Value;
        return new[]
        {
            new ScriptedSpawnBeat(
                "EliteBurst_30",
                30f,
                false,
                false,
                true,
                new[]
                {
                    new EnemySpawnCommandTemplate(
                        $"{template.Enemy.name}_burst",
                        template.Enemy,
                        template.Role,
                        template.Tags,
                        2,
                        template.Cost * 1.5f)
                })
        };
    }

    private static ScriptedSpawnBeat[] BuildBossBeat(EnemyDirectorTemplate? bossTemplate)
    {
        if (bossTemplate == null)
        {
            return System.Array.Empty<ScriptedSpawnBeat>();
        }

        EnemyDirectorTemplate template = bossTemplate.Value;
        return new[]
        {
            new ScriptedSpawnBeat(
                "BossEntry_40",
                40f,
                false,
                false,
                true,
                new[]
                {
                    new EnemySpawnCommandTemplate(
                        $"{template.Enemy.name}_boss",
                        template.Enemy,
                        template.Role,
                        template.Tags,
                        1,
                        template.Cost * 2f)
                })
        };
    }

    private static EndlessMilestoneBeat[] BuildMilestoneBossBeat(EnemyDirectorTemplate? bossTemplate)
    {
        if (bossTemplate == null)
        {
            return System.Array.Empty<EndlessMilestoneBeat>();
        }

        EnemyDirectorTemplate template = bossTemplate.Value;
        return new[]
        {
            new EndlessMilestoneBeat(
                5,
                5,
                new[]
                {
                    new ScriptedSpawnBeat(
                        "EndlessBossMilestone",
                        0.8f,
                        true,
                        true,
                        true,
                        new[]
                        {
                            new EnemySpawnCommandTemplate(
                                $"{template.Enemy.name}_milestone",
                                template.Enemy,
                                template.Role,
                                template.Tags,
                                1,
                                template.Cost * 2.5f)
                        })
                })
        };
    }

    private static AnimationCurve CreatePacingCurve(params float[] keys)
    {
        List<Keyframe> keyframes = new();
        for (int i = 0; i + 1 < keys.Length; i += 2)
        {
            keyframes.Add(new Keyframe(keys[i], keys[i + 1]));
        }

        return new AnimationCurve(keyframes.ToArray());
    }

    private static EnemyDirectorTemplates BuildEnemyDirectorTemplates(IReadOnlyList<EnemySO> enemies)
    {
        List<EnemyDirectorTemplate> meleeEntries = new();
        List<EnemyDirectorTemplate> rangedEntries = new();
        List<EnemyDirectorTemplate> eliteEntries = new();
        EnemyDirectorTemplate? bossAnchor = null;

        for (int i = 0; i < enemies.Count; i++)
        {
            EnemySO enemy = enemies[i];
            if (enemy == null)
            {
                continue;
            }

            string enemyName = enemy.name.ToLowerInvariant();
            if (enemy.role == EnemyRole.Boss || enemyName.Contains("boss"))
            {
                bossAnchor = new EnemyDirectorTemplate(enemy, SpawnRole.Boss, WaveEnemyTag.Boss | WaveEnemyTag.BossLike, 8f);
                continue;
            }

            if (enemyName.Contains("charger"))
            {
                eliteEntries.Add(new EnemyDirectorTemplate(enemy, SpawnRole.Elite, WaveEnemyTag.Fast | WaveEnemyTag.Heavy | WaveEnemyTag.Special, 2.5f));
                continue;
            }

            if (enemyName.Contains("ranged") || enemyName.Contains("kite"))
            {
                rangedEntries.Add(new EnemyDirectorTemplate(enemy, SpawnRole.Ranged, WaveEnemyTag.Ranged | WaveEnemyTag.Fast, 1.6f));
                continue;
            }

            if (enemyName.Contains("fast"))
            {
                meleeEntries.Add(new EnemyDirectorTemplate(enemy, SpawnRole.Melee, WaveEnemyTag.Fast | WaveEnemyTag.Normal, 1.2f));
                continue;
            }

            meleeEntries.Add(new EnemyDirectorTemplate(enemy, SpawnRole.Melee, WaveEnemyTag.Normal, 1f));
        }

        if (meleeEntries.Count == 0 && enemies.Count > 0)
        {
            EnemySO fallback = enemies[0];
            if (fallback != null)
            {
                meleeEntries.Add(new EnemyDirectorTemplate(fallback, SpawnRole.Melee, WaveEnemyTag.Normal, 1f));
            }
        }

        List<EnemyDirectorTemplate> meleeAndElite = new(meleeEntries);
        meleeAndElite.AddRange(eliteEntries);
        List<EnemyDirectorTemplate> meleeAndRanged = new(meleeEntries);
        meleeAndRanged.AddRange(rangedEntries);
        List<EnemyDirectorTemplate> mixed = new(meleeEntries);
        mixed.AddRange(rangedEntries);
        mixed.AddRange(eliteEntries);

        EnemyDirectorTemplate? eliteAnchor = eliteEntries.Count > 0
            ? eliteEntries[0]
            : meleeEntries.Count > 0 ? meleeEntries[0] : (EnemyDirectorTemplate?)null;

        return new EnemyDirectorTemplates(
            meleeEntries,
            meleeAndElite,
            meleeAndRanged,
            mixed,
            eliteAnchor,
            bossAnchor);
    }

    private readonly struct EnemyDirectorTemplate
    {
        public EnemyDirectorTemplate(EnemySO enemy, SpawnRole role, WaveEnemyTag tags, float cost)
        {
            Enemy = enemy;
            Role = role;
            Tags = tags;
            Cost = cost;
        }

        public EnemySO Enemy { get; }
        public SpawnRole Role { get; }
        public WaveEnemyTag Tags { get; }
        public float Cost { get; }
    }

    private readonly struct EnemyDirectorTemplates
    {
        public EnemyDirectorTemplates(
            List<EnemyDirectorTemplate> meleeEntries,
            List<EnemyDirectorTemplate> meleeAndEliteEntries,
            List<EnemyDirectorTemplate> meleeAndRangedEntries,
            List<EnemyDirectorTemplate> mixedEntries,
            EnemyDirectorTemplate? eliteAnchor,
            EnemyDirectorTemplate? bossAnchor)
        {
            MeleeEntries = meleeEntries;
            MeleeAndEliteEntries = meleeAndEliteEntries;
            MeleeAndRangedEntries = meleeAndRangedEntries;
            MixedEntries = mixedEntries;
            EliteAnchor = eliteAnchor;
            BossAnchor = bossAnchor;
        }

        public List<EnemyDirectorTemplate> MeleeEntries { get; }
        public List<EnemyDirectorTemplate> MeleeAndEliteEntries { get; }
        public List<EnemyDirectorTemplate> MeleeAndRangedEntries { get; }
        public List<EnemyDirectorTemplate> MixedEntries { get; }
        public EnemyDirectorTemplate? EliteAnchor { get; }
        public EnemyDirectorTemplate? BossAnchor { get; }
    }

    private static void SetObject(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogError($"Missing serialized property '{propertyName}' on {serializedObject.targetObject}.");
            return;
        }

        property.objectReferenceValue = value;
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

    private static void CleanupLegacyWaveContent()
    {
        DeleteAssetIfExists(GameContentAssetPaths.WavesData);
        DeleteAssetIfExists($"{GameContentAssetPaths.Waves}/Profiles");
        DeleteAssetIfExists($"{GameContentAssetPaths.Waves}/Pools");
    }

    private static void DeleteAssetIfExists(string assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
        {
            return;
        }

        if (!AssetDatabase.IsValidFolder(assetPath) &&
            AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) == null)
        {
            return;
        }

        if (!AssetDatabase.DeleteAsset(assetPath))
        {
            Debug.LogWarning($"Failed to delete legacy asset at {assetPath}.");
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
