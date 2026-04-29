#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class GolemEnemyAssetBuilder
{
    private const string BUILD_MENU_PATH = "Tools/Enemies/Build Golem Enemies";
    private const string VALIDATE_MENU_PATH = "Tools/Enemies/Validate Golem Enemies";
    private const string ENEMY_DATA_ROOT = "Assets/Resources/Data/Enemies";
    private const string BASE_PREFAB_PATH = "Assets/Resources/Prefabs/Enemies/EnemyBase.prefab";
    private const string DIRECT_CHASE_PATH = ENEMY_DATA_ROOT + "/DirectChaseStrategy.asset";

    private static readonly GolemBuildDefinition[] definitions =
    {
        new(
            "Golem Blue",
            ENEMY_DATA_ROOT + "/Golem Blue",
            "Assets/Resources/Prefabs/Enemies/Golem Blue.prefab",
            "Assets/Resources/Animation/Enemies/Golem Blue/Golem Blue.controller",
            "Assets/Resources/Sprites/Enemies/Golem Blue/Idle.png",
            "BlueGolemAnimationConfig.asset",
            "BlueGolemPropGroup.asset",
            "BlueGolemEnemy.asset",
            35f,
            0.45f,
            230f,
            1.6f,
            65f,
            1.2f,
            8f,
            1.2f,
            0.75f,
            1f,
            1.15f,
            1f),
        new(
            "Golem Orange",
            ENEMY_DATA_ROOT + "/Golem Orange",
            "Assets/Resources/Prefabs/Enemies/Golem Orange.prefab",
            "Assets/Resources/Animation/Enemies/Golem Orange/Golem Orange.controller",
            "Assets/Resources/Sprites/Enemies/Golem Orange/Idle.png",
            "OrangeGolemAnimationConfig.asset",
            "OrangeGolemPropGroup.asset",
            "OrangeGolemEnemy.asset",
            48f,
            0.5f,
            320f,
            1.7f,
            70f,
            1.25f,
            7f,
            1.1f,
            0.8f,
            0.9f,
            1.25f,
            1.1f)
    };

    [MenuItem(BUILD_MENU_PATH)]
    public static void Build()
    {
        EnsureFolder(ENEMY_DATA_ROOT);

        for (int i = 0; i < definitions.Length; i++)
        {
            Build(definitions[i]);
        }

        AssetDatabase.Refresh();
        Validate();
        Debug.Log("[GolemEnemyAssetBuilder] Golem enemy assets built.");
    }

    [MenuItem(VALIDATE_MENU_PATH)]
    public static void Validate()
    {
        List<string> errors = new();
        for (int i = 0; i < definitions.Length; i++)
        {
            Validate(definitions[i], errors);
        }

        if (errors.Count > 0)
        {
            throw new System.InvalidOperationException("[GolemEnemyAssetBuilder] Validation failed:\n" + string.Join("\n", errors));
        }

        Debug.Log("[GolemEnemyAssetBuilder] Golem enemy validation passed.");
    }

    private static void Build(GolemBuildDefinition definition)
    {
        EnsureFolder(definition.DataFolder);

        EntityAnimationConfig animationConfig = GetOrCreateAsset<EntityAnimationConfig>(definition.AnimationConfigPath);
        ConfigureAnimationConfig(animationConfig, definition);

        BasePropGroupSO propGroup = GetOrCreateAsset<BasePropGroupSO>(definition.PropGroupPath);
        ConfigurePropGroup(propGroup, definition);

        GolemEnemySO enemyData = GetOrCreateAsset<GolemEnemySO>(definition.EnemyDataPath);
        GameObject prefabRoot = BuildPrefab(definition);
        ConfigureEnemyData(enemyData, propGroup, animationConfig, prefabRoot, definition);
    }

    private static GameObject BuildPrefab(GolemBuildDefinition definition)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(definition.PrefabPath);
        if (prefab == null)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(BASE_PREFAB_PATH) == null)
            {
                throw new MissingReferenceException($"Cannot find base enemy prefab at {BASE_PREFAB_PATH}.");
            }

            AssetDatabase.CopyAsset(BASE_PREFAB_PATH, definition.PrefabPath);
        }

        GameObject instance = PrefabUtility.LoadPrefabContents(definition.PrefabPath);
        try
        {
            instance.name = definition.DisplayName;
            ConfigurePrefabInstance(instance, definition);
            PrefabUtility.SaveAsPrefabAsset(instance, definition.PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(instance);
        }

        return AssetDatabase.LoadAssetAtPath<GameObject>(definition.PrefabPath);
    }

    private static void ConfigurePrefabInstance(GameObject root, GolemBuildDefinition definition)
    {
        Animator animator = root.GetComponent<Animator>();
        if (animator != null)
        {
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(definition.ControllerPath);
        }

        CircleCollider2D collider = root.GetComponent<CircleCollider2D>();
        if (collider != null)
        {
            collider.offset = new Vector2(0f, 0.38f);
            collider.radius = 0.52f;
        }

        RangeAttackComponent rangeAttack = root.GetComponent<RangeAttackComponent>();
        if (rangeAttack != null)
        {
            Object.DestroyImmediate(rangeAttack, true);
        }

        if (root.GetComponent<MeleeAttackComponent>() == null)
        {
            root.AddComponent<MeleeAttackComponent>();
        }

        EnemyBrain[] brains = root.GetComponents<EnemyBrain>();
        for (int i = 0; i < brains.Length; i++)
        {
            if (brains[i] is GolemBrain)
            {
                continue;
            }

            Object.DestroyImmediate(brains[i], true);
        }

        if (root.GetComponent<GolemBrain>() == null)
        {
            root.AddComponent<GolemBrain>();
        }

        SpriteRenderer spriteRenderer = root.GetComponentInChildren<SpriteRenderer>();
        Sprite idleSprite = LoadFirstSprite(definition.IdleTexturePath);
        if (spriteRenderer != null && idleSprite != null)
        {
            spriteRenderer.sprite = idleSprite;
            spriteRenderer.color = Color.white;
            spriteRenderer.size = new Vector2(4f, 3f);
        }
    }

    private static void ConfigureAnimationConfig(EntityAnimationConfig config, GolemBuildDefinition definition)
    {
        config.AnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(definition.ControllerPath);
        config.Idle = "Idle";
        config.Move = "Move";
        config.Attack = "Attack";
        config.Death = "Death";
        MarkDirtyAndSave(config);
    }

    private static void ConfigurePropGroup(BasePropGroupSO propGroup, GolemBuildDefinition definition)
    {
        SerializedObject serializedObject = new(propGroup);
        SerializedProperty values = serializedObject.FindProperty("values");
        values.arraySize = 6;
        SetProp(values.GetArrayElementAtIndex(0), PropType.Attack, definition.Attack);
        SetProp(values.GetArrayElementAtIndex(1), PropType.AttackSpeed, definition.AttackSpeed);
        SetProp(values.GetArrayElementAtIndex(2), PropType.MaxHealth, definition.MaxHealth);
        SetProp(values.GetArrayElementAtIndex(3), PropType.DetectionRange, definition.DetectionRange);
        SetProp(values.GetArrayElementAtIndex(4), PropType.MoveSpeed, definition.MoveSpeed);
        SetProp(values.GetArrayElementAtIndex(5), PropType.AttackRange, definition.AttackRange);
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        MarkDirtyAndSave(propGroup);
    }

    private static void ConfigureEnemyData(
        GolemEnemySO enemyData,
        BasePropGroupSO propGroup,
        EntityAnimationConfig animationConfig,
        GameObject prefabRoot,
        GolemBuildDefinition definition)
    {
        SerializedObject serializedObject = new(enemyData);
        serializedObject.FindProperty("prefab").objectReferenceValue = prefabRoot != null ? prefabRoot.GetComponent<Enemy>() : null;
        serializedObject.FindProperty("role").enumValueIndex = (int)EnemyRole.Normal;
        serializedObject.FindProperty("basePropsAsset").objectReferenceValue = propGroup;
        serializedObject.FindProperty("animConfig").objectReferenceValue = animationConfig;
        serializedObject.FindProperty("attackCommitNormalizedTime").floatValue = 0.55f;
        serializedObject.FindProperty("attackFinishNormalizedTime").floatValue = 0.95f;
        serializedObject.FindProperty("berserkInterval").floatValue = definition.BerserkInterval;
        serializedObject.FindProperty("preChargeStunDuration").floatValue = definition.PreChargeStunDuration;
        serializedObject.FindProperty("chargeDuration").floatValue = definition.ChargeDuration;
        serializedObject.FindProperty("postChargeStunDuration").floatValue = definition.PostChargeStunDuration;
        serializedObject.FindProperty("chargeDamageRadius").floatValue = definition.ChargeDamageRadius;
        serializedObject.FindProperty("chargeDamageMultiplier").floatValue = definition.ChargeDamageMultiplier;
        serializedObject.FindProperty("chaseMoveStrategy").objectReferenceValue = AssetDatabase.LoadAssetAtPath<MovementStrategyBase>(DIRECT_CHASE_PATH);

        SerializedProperty modifiers = serializedObject.FindProperty("chargeModifiers");
        modifiers.arraySize = 1;
        SerializedProperty speedModifier = modifiers.GetArrayElementAtIndex(0);
        speedModifier.FindPropertyRelative("propType").enumValueIndex = (int)PropType.MoveSpeed;
        speedModifier.FindPropertyRelative("modifierType").enumValueIndex = (int)PropModifierType.FinalMultiplier;
        speedModifier.FindPropertyRelative("value").floatValue = 2f;

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        MarkDirtyAndSave(enemyData);
    }

    private static void Validate(GolemBuildDefinition definition, List<string> errors)
    {
        GolemEnemySO enemyData = AssetDatabase.LoadAssetAtPath<GolemEnemySO>(definition.EnemyDataPath);
        if (enemyData == null)
        {
            errors.Add($"{definition.DisplayName}: missing enemy data at {definition.EnemyDataPath}.");
            return;
        }

        if (enemyData.prefab == null)
        {
            errors.Add($"{definition.DisplayName}: enemy data has no prefab.");
        }

        if (enemyData.BasePropsAsset == null)
        {
            errors.Add($"{definition.DisplayName}: enemy data has no base prop group.");
        }

        if (enemyData.AnimConfig == null || enemyData.AnimConfig.AnimatorController == null)
        {
            errors.Add($"{definition.DisplayName}: enemy data has no animation config/controller.");
        }

        if (enemyData.chaseMoveStrategy == null)
        {
            errors.Add($"{definition.DisplayName}: enemy data has no chase move strategy.");
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(definition.PrefabPath);
        if (prefab == null)
        {
            errors.Add($"{definition.DisplayName}: missing prefab at {definition.PrefabPath}.");
            return;
        }

        if (prefab.GetComponent<Enemy>() == null)
        {
            errors.Add($"{definition.DisplayName}: prefab has no Enemy component.");
        }

        if (prefab.GetComponent<GolemBrain>() == null)
        {
            errors.Add($"{definition.DisplayName}: prefab has no GolemBrain component.");
        }

        if (prefab.GetComponent<MeleeAttackComponent>() == null)
        {
            errors.Add($"{definition.DisplayName}: prefab has no MeleeAttackComponent.");
        }

        if (prefab.GetComponent<RangeAttackComponent>() != null)
        {
            errors.Add($"{definition.DisplayName}: prefab still has RangeAttackComponent.");
        }
    }

    private static void SetProp(SerializedProperty property, PropType propType, float value)
    {
        property.FindPropertyRelative("propType").enumValueIndex = (int)propType;
        property.FindPropertyRelative("value").floatValue = value;
    }

    private static T GetOrCreateAsset<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null)
        {
            return asset;
        }

        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static void MarkDirtyAndSave(Object asset)
    {
        if (asset == null)
        {
            return;
        }

        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssetIfDirty(asset);
    }

    private static Sprite LoadFirstSprite(string texturePath)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(texturePath);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite sprite)
            {
                return sprite;
            }
        }

        return null;
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    private readonly struct GolemBuildDefinition
    {
        public readonly string DisplayName;
        public readonly string DataFolder;
        public readonly string PrefabPath;
        public readonly string ControllerPath;
        public readonly string IdleTexturePath;
        public readonly string AnimationConfigPath;
        public readonly string PropGroupPath;
        public readonly string EnemyDataPath;
        public readonly float Attack;
        public readonly float AttackSpeed;
        public readonly float MaxHealth;
        public readonly float DetectionRange;
        public readonly float MoveSpeed;
        public readonly float AttackRange;
        public readonly float BerserkInterval;
        public readonly float PreChargeStunDuration;
        public readonly float ChargeDuration;
        public readonly float PostChargeStunDuration;
        public readonly float ChargeDamageRadius;
        public readonly float ChargeDamageMultiplier;

        public GolemBuildDefinition(
            string displayName,
            string dataFolder,
            string prefabPath,
            string controllerPath,
            string idleTexturePath,
            string animationConfigName,
            string propGroupName,
            string enemyDataName,
            float attack,
            float attackSpeed,
            float maxHealth,
            float detectionRange,
            float moveSpeed,
            float attackRange,
            float berserkInterval,
            float preChargeStunDuration,
            float chargeDuration,
            float postChargeStunDuration,
            float chargeDamageRadius,
            float chargeDamageMultiplier)
        {
            DisplayName = displayName;
            DataFolder = dataFolder;
            PrefabPath = prefabPath;
            ControllerPath = controllerPath;
            IdleTexturePath = idleTexturePath;
            AnimationConfigPath = dataFolder + "/" + animationConfigName;
            PropGroupPath = dataFolder + "/" + propGroupName;
            EnemyDataPath = dataFolder + "/" + enemyDataName;
            Attack = attack;
            AttackSpeed = attackSpeed;
            MaxHealth = maxHealth;
            DetectionRange = detectionRange;
            MoveSpeed = moveSpeed;
            AttackRange = attackRange;
            BerserkInterval = berserkInterval;
            PreChargeStunDuration = preChargeStunDuration;
            ChargeDuration = chargeDuration;
            PostChargeStunDuration = postChargeStunDuration;
            ChargeDamageRadius = chargeDamageRadius;
            ChargeDamageMultiplier = chargeDamageMultiplier;
        }
    }
}
#endif
