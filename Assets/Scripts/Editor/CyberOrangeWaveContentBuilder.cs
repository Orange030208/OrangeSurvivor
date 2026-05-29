using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class CyberOrangeWaveContentBuilder
{
    private const string MENU_PATH = "Tools/Orange/Content/Build Cyber Orange Wave Enemies";
    private const string DATA_FOLDER = "Assets/GameContent/Enemies/Data/CyberOrange";
    private const string PREFAB_FOLDER = "Assets/GameContent/Enemies/Prefabs";
    private const string SPRITE_FOLDER = "Assets/GameContent/Enemies/Sprites/CyberOrange";
    private const string BASE_PREFAB_PATH = PREFAB_FOLDER + "/Cyber Orange.prefab";
    private const string ANIM_CONFIG_PATH = DATA_FOLDER + "/CyberOrangeAnimationConfig.asset";
    private const string WAVE_POOL_PATH = "Assets/GameContent/Waves/Pools/Wave Spawn Pool.asset";
    private const string PROJECTILE_1_PATH = GameContentAssetPaths.WeaponProjectileData + "/ProjectileCore_SolidSlug_Orange.asset";
    private const string PROJECTILE_2_PATH = GameContentAssetPaths.WeaponProjectileData + "/ProjectileCore_Needle_Orange.asset";

    private sealed class VariantDefinition
    {
        public string Id;
        public string SpriteName;
        public System.Type EnemyType;
        public System.Type BrainType;
        public int OpenWave;
        public float Weight;
        public WaveEnemyTag Tags;
        public BasePropData[] Props;
        public string ProjectilePath;
    }

    [MenuItem(MENU_PATH)]
    public static void Build()
    {
        EnsureFolder(DATA_FOLDER);
        EnsureFolder(PREFAB_FOLDER);

        GameObject basePrefab = LoadRequired<GameObject>(BASE_PREFAB_PATH);
        EntityAnimationConfig animConfig = LoadRequired<EntityAnimationConfig>(ANIM_CONFIG_PATH);
        ContentPoolSO wavePool = LoadRequired<ContentPoolSO>(WAVE_POOL_PATH);

        List<ContentPoolEntry> poolEntries = new();
        foreach (VariantDefinition variant in CreateVariants())
        {
            Sprite sprite = LoadRequired<Sprite>($"{SPRITE_FOLDER}/{variant.SpriteName}.png");
            BasePropGroupSO propGroup = CreateOrUpdatePropGroup(variant);
            Enemy prefabEnemy = CreateOrUpdatePrefab(basePrefab, variant, sprite);
            EnemySO enemy = CreateOrUpdateEnemy(variant, prefabEnemy, propGroup, animConfig);
            poolEntries.Add(CreateWavePoolEntry(variant, enemy));
        }

        wavePool.Initialize(poolEntries, 1, false);
        EditorUtility.SetDirty(wavePool);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[{nameof(CyberOrangeWaveContentBuilder)}] Built {poolEntries.Count} Cyber Orange wave enemies.");
    }

    private static IReadOnlyList<VariantDefinition> CreateVariants()
    {
        return new[]
        {
            new VariantDefinition
            {
                Id = "CyberOrangeSlowMelee",
                SpriteName = "SlowMelee",
                EnemyType = typeof(CyberOrangeEnemySO),
                BrainType = typeof(ChaseEnemyBrain),
                OpenWave = 1,
                Weight = 8f,
                Tags = WaveEnemyTag.Normal | WaveEnemyTag.Heavy,
                Props = Props(maxHealth: 34f, attack: 10f, attackSpeed: 60f, moveSpeed: 250f, attackRange: 120f)
            },
            new VariantDefinition
            {
                Id = "CyberOrangeFastMelee",
                SpriteName = "FastMelee",
                EnemyType = typeof(CyberOrangeEnemySO),
                BrainType = typeof(ChaseEnemyBrain),
                OpenWave = 2,
                Weight = 6f,
                Tags = WaveEnemyTag.Normal | WaveEnemyTag.Fast,
                Props = Props(maxHealth: 22f, attack: 8f, attackSpeed: 80f, moveSpeed: 420f, attackRange: 110f)
            },
            new VariantDefinition
            {
                Id = "CyberOrangeCharger",
                SpriteName = "Charger",
                EnemyType = typeof(ChargerEnemySO),
                BrainType = typeof(ChargerEnemyBrain),
                OpenWave = 4,
                Weight = 4f,
                Tags = WaveEnemyTag.Normal | WaveEnemyTag.Fast | WaveEnemyTag.Heavy,
                Props = Props(maxHealth: 42f, attack: 12f, attackSpeed: 65f, moveSpeed: 330f, attackRange: 125f)
            },
            new VariantDefinition
            {
                Id = "CyberOrangeRanged",
                SpriteName = "Ranged",
                EnemyType = typeof(FlyForestEnemySO),
                BrainType = typeof(RangedChaseEnemyBrain),
                OpenWave = 6,
                Weight = 3f,
                Tags = WaveEnemyTag.Ranged,
                Props = Props(maxHealth: 26f, attack: 9f, attackSpeed: 55f, moveSpeed: 260f, attackRange: 450f),
                ProjectilePath = PROJECTILE_1_PATH
            },
            new VariantDefinition
            {
                Id = "CyberOrangeKitingRanged",
                SpriteName = "KitingRanged",
                EnemyType = typeof(WormEnemySO),
                BrainType = typeof(KitingRangedEnemyBrain),
                OpenWave = 8,
                Weight = 2f,
                Tags = WaveEnemyTag.Ranged | WaveEnemyTag.Fast | WaveEnemyTag.Special,
                Props = Props(maxHealth: 20f, attack: 7f, attackSpeed: 70f, moveSpeed: 340f, attackRange: 520f),
                ProjectilePath = PROJECTILE_2_PATH
            }
        };
    }

    private static BasePropData[] Props(float maxHealth, float attack, float attackSpeed, float moveSpeed, float attackRange)
    {
        return new[]
        {
            new BasePropData(PropType.MaxHealth, maxHealth),
            new BasePropData(PropType.Attack, attack),
            new BasePropData(PropType.AttackSpeed, attackSpeed),
            new BasePropData(PropType.MoveSpeed, moveSpeed),
            new BasePropData(PropType.AttackRange, attackRange),
            new BasePropData(PropType.Armor, 0f),
            new BasePropData(PropType.KnockbackResistance, 0f)
        };
    }

    private static BasePropGroupSO CreateOrUpdatePropGroup(VariantDefinition variant)
    {
        string path = $"{DATA_FOLDER}/{variant.Id}PropGroup.asset";
        BasePropGroupSO propGroup = AssetDatabase.LoadAssetAtPath<BasePropGroupSO>(path);
        if (propGroup == null)
        {
            propGroup = ScriptableObject.CreateInstance<BasePropGroupSO>();
            AssetDatabase.CreateAsset(propGroup, path);
        }

        SerializedObject serializedObject = new(propGroup);
        SerializedProperty values = serializedObject.FindProperty("values");
        values.arraySize = variant.Props.Length;
        for (int i = 0; i < variant.Props.Length; i++)
        {
            SerializedProperty item = values.GetArrayElementAtIndex(i);
            item.FindPropertyRelative("propType").intValue = (int)variant.Props[i].propType;
            item.FindPropertyRelative("value").floatValue = variant.Props[i].value;
        }

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(propGroup);
        return propGroup;
    }

    private static EnemySO CreateOrUpdateEnemy(
        VariantDefinition variant,
        Enemy prefabEnemy,
        BasePropGroupSO propGroup,
        EntityAnimationConfig animConfig)
    {
        string path = $"{DATA_FOLDER}/{variant.Id}Enemy.asset";
        EnemySO enemy = AssetDatabase.LoadAssetAtPath<EnemySO>(path);
        if (enemy == null || enemy.GetType() != variant.EnemyType)
        {
            if (enemy != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            enemy = (EnemySO)ScriptableObject.CreateInstance(variant.EnemyType);
            AssetDatabase.CreateAsset(enemy, path);
        }

        SerializedObject serializedObject = new(enemy);
        serializedObject.FindProperty("prefab").objectReferenceValue = prefabEnemy;
        serializedObject.FindProperty("role").intValue = (int)EnemyRole.Normal;
        serializedObject.FindProperty("basePropsAsset").objectReferenceValue = propGroup;
        serializedObject.FindProperty("animConfig").objectReferenceValue = animConfig;
        serializedObject.FindProperty("damagedSfxKey").intValue = (int)AudioSfxKey.EnemyHurtGeneric;

        if (enemy is FlyForestEnemySO)
        {
            serializedObject.FindProperty("normalAttackProjectileDefinition").objectReferenceValue =
                LoadRequired<ProjectileDefinitionSO>(variant.ProjectilePath);
        }
        else if (enemy is WormEnemySO)
        {
            serializedObject.FindProperty("attackProjectileDefinition").objectReferenceValue =
                LoadRequired<ProjectileDefinitionSO>(variant.ProjectilePath);
        }

        if (enemy is ChargerEnemySO)
        {
            SerializedProperty chargeInterval = serializedObject.FindProperty("chargeInterval");
            SerializedProperty preChargeDuration = serializedObject.FindProperty("preChargeDuration");
            SerializedProperty chargeDuration = serializedObject.FindProperty("chargeDuration");
            SerializedProperty chargeDamageRadius = serializedObject.FindProperty("chargeDamageRadius");
            SerializedProperty chargeDamageMultiplier = serializedObject.FindProperty("chargeDamageMultiplier");
            SerializedProperty chargeModifiers = serializedObject.FindProperty("chargeModifiers");
            chargeInterval.floatValue = 7f;
            preChargeDuration.floatValue = 0.75f;
            chargeDuration.floatValue = 0.75f;
            chargeDamageRadius.floatValue = 1.1f;
            chargeDamageMultiplier.floatValue = 1.2f;
            chargeModifiers.arraySize = 1;
            SerializedProperty chargeMoveSpeedModifier = chargeModifiers.GetArrayElementAtIndex(0);
            chargeMoveSpeedModifier.FindPropertyRelative("propType").intValue = (int)PropType.MoveSpeed;
            chargeMoveSpeedModifier.FindPropertyRelative("modifierType").intValue = (int)PropModifierType.FinalMultiplier;
            chargeMoveSpeedModifier.FindPropertyRelative("value").floatValue = 50f;
        }

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(enemy);
        return enemy;
    }

    private static Enemy CreateOrUpdatePrefab(GameObject basePrefab, VariantDefinition variant, Sprite sprite)
    {
        string path = $"{PREFAB_FOLDER}/{variant.Id}.prefab";
        GameObject contents = PrefabUtility.LoadPrefabContents(BASE_PREFAB_PATH);
        try
        {
            contents.name = variant.Id;
            SpriteRenderer spriteRenderer = contents.GetComponentInChildren<SpriteRenderer>(true);
            if (spriteRenderer == null)
            {
                throw new MissingComponentException($"{basePrefab.name} has no child SpriteRenderer.");
            }

            spriteRenderer.sprite = sprite;
            ConfigureBrain(contents, variant.BrainType);

            PrefabUtility.SaveAsPrefabAsset(contents, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }

        GameObject prefab = LoadRequired<GameObject>(path);
        Enemy enemy = prefab.GetComponent<Enemy>();
        if (enemy == null)
        {
            throw new MissingComponentException($"{path} has no {nameof(Enemy)} component.");
        }

        return enemy;
    }

    private static void ConfigureBrain(GameObject root, System.Type brainType)
    {
        Transform point = root.transform.Find("MeleePoint");
        if (point == null)
        {
            GameObject pointObject = new("MeleePoint");
            pointObject.layer = root.layer;
            pointObject.transform.SetParent(root.transform, false);
            pointObject.transform.localPosition = new Vector3(0f, 0.255f, 0f);
            point = pointObject.transform;
        }

        EnemyBrain[] brains = root.GetComponents<EnemyBrain>();
        foreach (EnemyBrain brain in brains)
        {
            if (brain.GetType() != brainType)
            {
                Object.DestroyImmediate(brain, true);
            }
        }

        EnemyBrain targetBrain = root.GetComponent(brainType) as EnemyBrain;
        if (targetBrain == null)
        {
            targetBrain = root.AddComponent(brainType) as EnemyBrain;
        }

        SerializedObject serializedObject = new(targetBrain);
        SetObjectIfFound(serializedObject, "meleePointTransform", point);
        SetObjectIfFound(serializedObject, "shootPointTransform", point);
        SetObjectIfFound(serializedObject, "attackPointTransform", point);
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static ContentPoolEntry CreateWavePoolEntry(VariantDefinition variant, EnemySO enemy)
    {
        ContentPoolEntry entry = new(enemy, variant.Weight, variant.Id);
        entry.ConfigureRuntimeMetadata(new ContentEntryMetadata[]
        {
            new WaveSpawnMetadata(variant.Tags)
        });
        entry.ConfigureRuntimeRules(
            new ContentCondition[]
            {
                new CurrentWaveCondition(ContentComparisonOperator.GreaterOrEqual, variant.OpenWave)
            },
            null);
        return entry;
    }

    private static void SetObjectIfFound(SerializedObject serializedObject, string propertyName, Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }

    private static T LoadRequired<T>(string path)
        where T : Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            throw new FileNotFoundException($"Missing required asset at {path}.");
        }

        return asset;
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
        string folderName = Path.GetFileName(folderPath);
        if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(folderName))
        {
            throw new DirectoryNotFoundException($"Invalid Unity asset folder path: {folderPath}");
        }

        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folderName);
    }
}
