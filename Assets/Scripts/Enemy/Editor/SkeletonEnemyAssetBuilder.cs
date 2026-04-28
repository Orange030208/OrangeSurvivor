#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class SkeletonEnemyAssetBuilder
{
    private const string MENU_PATH = "Tools/Enemies/Build Skeleton Enemy";
    private const string ENEMY_FOLDER = "Assets/Resources/Data/Enemies/Skeleton";
    private const string PREFAB_PATH = "Assets/Resources/Prefabs/Enemies/Skeleton.prefab";
    private const string BASE_PREFAB_PATH = "Assets/Resources/Prefabs/Enemies/EnemyBase.prefab";
    private const string ANIMATION_CONFIG_PATH = ENEMY_FOLDER + "/SkeletonAnimationConfig.asset";
    private const string PROP_GROUP_PATH = ENEMY_FOLDER + "/SkeletonPropGroup.asset";
    private const string ENEMY_DATA_PATH = ENEMY_FOLDER + "/SkeletonEnemy.asset";
    private const string CONTROLLER_PATH = "Assets/Resources/Animation/Enemies/Skeleton/Skeleton.controller";
    private const string IDLE_TEXTURE_PATH = "Assets/Resources/Sprites/Enemies/Skeleton/Idle.png";
    private const string DIRECT_CHASE_PATH = "Assets/Resources/Data/Enemies/DirectChaseStrategy.asset";

    [MenuItem(MENU_PATH)]
    public static void Build()
    {
        EnsureFolder("Assets/Resources/Data/Enemies");
        EnsureFolder(ENEMY_FOLDER);

        EntityAnimationConfig animationConfig = GetOrCreateAsset<EntityAnimationConfig>(ANIMATION_CONFIG_PATH);
        ConfigureAnimationConfig(animationConfig);

        BasePropGroupSO propGroup = GetOrCreateAsset<BasePropGroupSO>(PROP_GROUP_PATH);
        ConfigurePropGroup(propGroup);

        SkeletonEnemySO enemyData = GetOrCreateAsset<SkeletonEnemySO>(ENEMY_DATA_PATH);
        GameObject prefabRoot = BuildPrefab();
        ConfigureEnemyData(enemyData, propGroup, animationConfig, prefabRoot);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SkeletonEnemyAssetBuilder] Skeleton enemy assets built.");
    }

    private static GameObject BuildPrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
        if (prefab == null)
        {
            GameObject basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BASE_PREFAB_PATH);
            if (basePrefab == null)
            {
                throw new MissingReferenceException($"Cannot find base enemy prefab at {BASE_PREFAB_PATH}.");
            }

            AssetDatabase.CopyAsset(BASE_PREFAB_PATH, PREFAB_PATH);
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
        }

        GameObject instance = PrefabUtility.LoadPrefabContents(PREFAB_PATH);
        try
        {
            instance.name = "Skeleton";
            ConfigurePrefabInstance(instance);
            PrefabUtility.SaveAsPrefabAsset(instance, PREFAB_PATH);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(instance);
        }

        return AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
    }

    private static void ConfigurePrefabInstance(GameObject root)
    {
        Animator animator = root.GetComponent<Animator>();
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(CONTROLLER_PATH);

        CircleCollider2D collider = root.GetComponent<CircleCollider2D>();
        collider.offset = new Vector2(0f, 0.32f);
        collider.radius = 0.34f;

        RangeAttackComponent rangeAttack = root.GetComponent<RangeAttackComponent>();
        if (rangeAttack != null)
        {
            Object.DestroyImmediate(rangeAttack, true);
        }

        if (root.GetComponent<MeleeAttackComponent>() == null)
        {
            root.AddComponent<MeleeAttackComponent>();
        }

        if (root.GetComponent<SkeletonBrain>() == null)
        {
            root.AddComponent<SkeletonBrain>();
        }

        SpriteRenderer spriteRenderer = root.GetComponentInChildren<SpriteRenderer>();
        Sprite idleSprite = LoadFirstSprite(IDLE_TEXTURE_PATH);
        if (spriteRenderer != null && idleSprite != null)
        {
            spriteRenderer.sprite = idleSprite;
            spriteRenderer.color = Color.white;
        }
    }

    private static void ConfigureAnimationConfig(EntityAnimationConfig config)
    {
        config.AnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(CONTROLLER_PATH);
        config.Idle = "Idle";
        config.Move = "Move";
        config.Attack = "Attack";
        config.Death = "Dead";
        EditorUtility.SetDirty(config);
    }

    private static void ConfigurePropGroup(BasePropGroupSO propGroup)
    {
        SerializedObject serializedObject = new(propGroup);
        SerializedProperty values = serializedObject.FindProperty("values");
        values.arraySize = 5;
        SetProp(values.GetArrayElementAtIndex(0), PropType.Attack, 18f);
        SetProp(values.GetArrayElementAtIndex(1), PropType.AttackSpeed, 0.75f);
        SetProp(values.GetArrayElementAtIndex(2), PropType.MaxHealth, 90f);
        SetProp(values.GetArrayElementAtIndex(3), PropType.Range, 1.35f);
        SetProp(values.GetArrayElementAtIndex(4), PropType.MoveSpeed, 3.2f);
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(propGroup);
    }

    private static void ConfigureEnemyData(SkeletonEnemySO enemyData, BasePropGroupSO propGroup, EntityAnimationConfig animationConfig, GameObject prefabRoot)
    {
        SerializedObject serializedObject = new(enemyData);
        serializedObject.FindProperty("prefab").objectReferenceValue = prefabRoot != null ? prefabRoot.GetComponent<Enemy>() : null;
        serializedObject.FindProperty("role").enumValueIndex = (int)EnemyRole.Normal;
        serializedObject.FindProperty("basePropsAsset").objectReferenceValue = propGroup;
        serializedObject.FindProperty("animConfig").objectReferenceValue = animationConfig;
        serializedObject.FindProperty("attackCommitNormalizedTime").floatValue = 0.55f;
        serializedObject.FindProperty("attackFinishNormalizedTime").floatValue = 0.95f;
        serializedObject.FindProperty("chaseMoveStrategy").objectReferenceValue = AssetDatabase.LoadAssetAtPath<MovementStrategyBase>(DIRECT_CHASE_PATH);
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(enemyData);
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
}
#endif
