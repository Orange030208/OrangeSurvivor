#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using Object = UnityEngine.Object;

public static class ProjectileContentAssetBuilder
{
    private const string MENU_PATH = "Tools/Orange/Content/Build Projectile Core Content";
    private const string LEGACY_ATLAS_PATH = "Assets/GameContent/Combat/VFX/Sprites/Projectiles/ProjectileCoreAtlas.png";
    private const string ATLAS_PATH = GameContentAssetPaths.WeaponProjectileAtlas;
    private const string PROJECTILE_FOLDER = GameContentAssetPaths.WeaponProjectileData;
    private const string PREFAB_FOLDER = GameContentAssetPaths.WeaponProjectilePrefabs;
    private const string BASE_PREFAB_PATH = GameContentAssetPaths.WeaponProjectilePrefabs + "/ProjectileBase.prefab";
    private const string LEGACY_PROJECTILE_ROOT = "Assets/GameContent/Weapons/Legacy/Projectiles";
    private const string LEGACY_PROJECTILE_DATA_FOLDER = LEGACY_PROJECTILE_ROOT + "/Data";
    private const string LEGACY_PROJECTILE_PREFAB_FOLDER = LEGACY_PROJECTILE_ROOT + "/Prefabs";
    private const int ATLAS_GRID_SIZE = 4;
    private const int ATLAS_CELL_SIZE = 128;

    private static readonly string[] LegacyProjectileIds =
    {
        "Projectile1",
        "Projectile2",
        "GolemMechaStoneArmProjectile",
    };

    private static readonly ProjectileVariantDefinition[] Variants =
    {
        new("ProjectileCore_Capsule_Cyan", "Projectile Core Capsule Cyan", "ProjectileCore_Capsule_Cyan", ProjectileColliderKind.Circle, 0.14f, new Vector2(0f, 0f), new Vector2(0f, 0f), new Color(0.29f, 0.92f, 1f, 1f), 1f, 1f, true, 0f),
        new("ProjectileCore_Capsule_Magenta", "Projectile Core Capsule Magenta", "ProjectileCore_Capsule_Magenta", ProjectileColliderKind.Circle, 0.14f, new Vector2(0f, 0f), new Vector2(0f, 0f), new Color(1f, 0.28f, 0.83f, 1f), 1f, 1f, true, 0f),
        new("ProjectileCore_Capsule_Orange", "Projectile Core Capsule Orange", "ProjectileCore_Capsule_Orange", ProjectileColliderKind.Circle, 0.145f, new Vector2(0f, 0f), new Vector2(0f, 0f), new Color(1f, 0.55f, 0.16f, 1f), 1f, 1f, true, 0f),
        new("ProjectileCore_Capsule_CyanMagenta", "Projectile Core Capsule Cyan Magenta", "ProjectileCore_Capsule_CyanMagenta", ProjectileColliderKind.Circle, 0.15f, new Vector2(0f, 0f), new Vector2(0f, 0f), new Color(0.75f, 0.48f, 1f, 1f), 1f, 1f, true, 0f),
        new("ProjectileCore_Line_Cyan", "Projectile Core Line Cyan", "ProjectileCore_Line_Cyan", ProjectileColliderKind.Box, 0f, new Vector2(0.62f, 0.10f), new Vector2(0f, 0f), new Color(0.2f, 0.95f, 1f, 1f), 1f, 1f, true, 0f),
        new("ProjectileCore_Line_Magenta", "Projectile Core Line Magenta", "ProjectileCore_Line_Magenta", ProjectileColliderKind.Box, 0f, new Vector2(0.62f, 0.10f), new Vector2(0f, 0f), new Color(1f, 0.22f, 0.82f, 1f), 1f, 1f, true, 0f),
        new("ProjectileCore_Streak_Orange", "Projectile Core Streak Orange", "ProjectileCore_Streak_Orange", ProjectileColliderKind.Box, 0f, new Vector2(0.64f, 0.11f), new Vector2(0f, 0f), new Color(1f, 0.63f, 0.15f, 1f), 1f, 1f, true, 0f),
        new("ProjectileCore_Taper_BlueWhite", "Projectile Core Taper Blue White", "ProjectileCore_Taper_BlueWhite", ProjectileColliderKind.Box, 0f, new Vector2(0.48f, 0.10f), new Vector2(0f, 0f), new Color(0.76f, 0.92f, 1f, 1f), 1f, 1f, true, 0f),
        new("ProjectileCore_Wedge_Cyan", "Projectile Core Wedge Cyan", "ProjectileCore_Wedge_Cyan", ProjectileColliderKind.Box, 0f, new Vector2(0.56f, 0.16f), new Vector2(0f, 0f), new Color(0.24f, 0.91f, 1f, 1f), 1f, 1f, true, 0f),
        new("ProjectileCore_Diamond_Magenta", "Projectile Core Diamond Magenta", "ProjectileCore_Diamond_Magenta", ProjectileColliderKind.Circle, 0.13f, new Vector2(0f, 0f), new Vector2(0f, 0f), new Color(1f, 0.28f, 0.84f, 1f), 1f, 1f, true, 0f),
        new("ProjectileCore_Needle_Orange", "Projectile Core Needle Orange", "ProjectileCore_Needle_Orange", ProjectileColliderKind.Box, 0f, new Vector2(0.42f, 0.08f), new Vector2(0f, 0f), new Color(1f, 0.58f, 0.18f, 1f), 1f, 1f, true, 0f),
        new("ProjectileCore_Shard_Violet", "Projectile Core Shard Violet", "ProjectileCore_Shard_Violet", ProjectileColliderKind.Box, 0f, new Vector2(0.44f, 0.10f), new Vector2(0f, 0f), new Color(0.76f, 0.47f, 1f, 1f), 1f, 1f, true, 0f),
        new("ProjectileCore_SolidSlug_Orange", "Projectile Core Solid Slug Orange", "ProjectileCore_SolidSlug_Orange", ProjectileColliderKind.Circle, 0.165f, new Vector2(0f, 0f), new Vector2(0f, 0f), new Color(1f, 0.56f, 0.18f, 1f), 1f, 1f, true, 0f),
        new("ProjectileCore_SolidSlug_Cyan", "Projectile Core Solid Slug Cyan", "ProjectileCore_SolidSlug_Cyan", ProjectileColliderKind.Circle, 0.165f, new Vector2(0f, 0f), new Vector2(0f, 0f), new Color(0.28f, 0.94f, 1f, 1f), 1f, 1f, true, 0f),
        new("ProjectileCore_HexSlug_Magenta", "Projectile Core Hex Slug Magenta", "ProjectileCore_HexSlug_Magenta", ProjectileColliderKind.Circle, 0.172f, new Vector2(0f, 0f), new Vector2(0f, 0f), new Color(1f, 0.32f, 0.84f, 1f), 1f, 1f, true, 0f),
        new("ProjectileCore_OvalSlug_Purple", "Projectile Core Oval Slug Purple", "ProjectileCore_OvalSlug_Purple", ProjectileColliderKind.Circle, 0.185f, new Vector2(0f, 0f), new Vector2(0f, 0f), new Color(0.76f, 0.48f, 1f, 1f), 1f, 1f, true, 0f),
    };

    private static readonly ConsumerBinding[] ConsumerBindings =
    {
        new("Weapon_NeonPistol", ConsumerKind.Weapon, "ProjectileCore_SolidSlug_Cyan"),
        new("Weapon_NeonSMG", ConsumerKind.Weapon, "ProjectileCore_Line_Cyan"),
        new("Weapon_NeonMinigun", ConsumerKind.Weapon, "ProjectileCore_Line_Magenta"),
        new("Weapon_NeonShotgun", ConsumerKind.Weapon, "ProjectileCore_Capsule_Orange"),
        new("Weapon_NeonRocketLauncher", ConsumerKind.Weapon, "ProjectileCore_OvalSlug_Purple"),
        new("Weapon_NeonHeartScepter", ConsumerKind.Weapon, "ProjectileCore_Capsule_Magenta"),
        new("Weapon_NeonCrownScepter", ConsumerKind.Weapon, "ProjectileCore_Diamond_Magenta"),
        new("Weapon_NeonStarWand", ConsumerKind.Weapon, "ProjectileCore_Taper_BlueWhite"),
        new("CyberOrangeRangedEnemy", ConsumerKind.Enemy, "ProjectileCore_SolidSlug_Orange"),
        new("CyberOrangeKitingRangedEnemy", ConsumerKind.Enemy, "ProjectileCore_Needle_Orange"),
    };

    [MenuItem(MENU_PATH)]
    public static void Build()
    {
        EnsureFolders();
        ArchiveLegacyProjectileAssets();
        EnsureAtlasLocation();

        SpriteLookup spriteLookup = BuildSpriteLookup();
        GameObject basePrefabAsset = LoadRequired<GameObject>(BASE_PREFAB_PATH);
        Dictionary<string, ProjectileDefinitionSO> definitionsById = new(StringComparer.Ordinal);

        for (int i = 0; i < Variants.Length; i++)
        {
            ProjectileVariantDefinition variant = Variants[i];
            Sprite sprite = ResolveSlice(spriteLookup, variant.SpriteName);
            Projectile prefab = CreateOrUpdatePrefab(basePrefabAsset, variant, sprite);
            ProjectileDefinitionSO definition = CreateOrUpdateDefinition(variant, sprite, prefab);
            definitionsById[variant.Id] = definition;
        }

        UpdateConsumers(definitionsById);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            $"[{nameof(ProjectileContentAssetBuilder)}] Built {Variants.Length} projectile definitions, {Variants.Length} prefab variants, and {ConsumerBindings.Length} consumer bindings.");
    }

    private static void EnsureFolders()
    {
        EnsureFolder(PROJECTILE_FOLDER);
        EnsureFolder(PREFAB_FOLDER);
        EnsureFolder(LEGACY_PROJECTILE_DATA_FOLDER);
        EnsureFolder(LEGACY_PROJECTILE_PREFAB_FOLDER);
        EnsureFolder(Path.GetDirectoryName(ATLAS_PATH)?.Replace('\\', '/'));
    }

    private static void ArchiveLegacyProjectileAssets()
    {
        for (int i = 0; i < LegacyProjectileIds.Length; i++)
        {
            string legacyId = LegacyProjectileIds[i];
            MoveAssetIfExists(
                $"{PROJECTILE_FOLDER}/{legacyId}.asset",
                $"{LEGACY_PROJECTILE_DATA_FOLDER}/{legacyId}.asset");
            MoveAssetIfExists(
                $"{PREFAB_FOLDER}/{legacyId}.prefab",
                $"{LEGACY_PROJECTILE_PREFAB_FOLDER}/{legacyId}.prefab");
        }
    }

    private static void EnsureAtlasLocation()
    {
        if (File.Exists(ATLAS_PATH))
        {
            return;
        }

        if (!File.Exists(LEGACY_ATLAS_PATH))
        {
            throw new FileNotFoundException($"Missing projectile atlas at '{ATLAS_PATH}' or '{LEGACY_ATLAS_PATH}'.");
        }

        string moveError = AssetDatabase.MoveAsset(LEGACY_ATLAS_PATH, ATLAS_PATH);
        if (!string.IsNullOrWhiteSpace(moveError))
        {
            throw new InvalidOperationException(
                $"Failed to move projectile atlas from '{LEGACY_ATLAS_PATH}' to '{ATLAS_PATH}': {moveError}");
        }
    }

    private static SpriteLookup BuildSpriteLookup()
    {
        TextureImporter importer = AssetImporter.GetAtPath(ATLAS_PATH) as TextureImporter;
        if (importer == null)
        {
            throw new FileNotFoundException($"Missing texture importer at '{ATLAS_PATH}'.");
        }

        ConfigureAtlasImporter(importer);
        importer.SaveAndReimport();

        Dictionary<string, Sprite> spritesByName = new(StringComparer.Ordinal);
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(ATLAS_PATH);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite sprite)
            {
                spritesByName[sprite.name] = sprite;
            }
        }

        return new SpriteLookup(spritesByName);
    }

    private static void ConfigureAtlasImporter(TextureImporter importer)
    {
        if (Variants.Length != ATLAS_GRID_SIZE * ATLAS_GRID_SIZE)
        {
            throw new InvalidOperationException(
                $"Projectile atlas expects {ATLAS_GRID_SIZE * ATLAS_GRID_SIZE} variants, but found {Variants.Length}.");
        }

        SpriteDataProviderFactories factory = new();
        factory.Init();

        ISpriteEditorDataProvider dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
        if (dataProvider == null)
        {
            throw new InvalidOperationException($"Failed to acquire sprite data provider for '{ATLAS_PATH}'.");
        }

        dataProvider.InitSpriteEditorDataProvider();

        SerializedObject serializedObject = new(importer);
        SerializedProperty textureTypeProperty = serializedObject.FindProperty("textureType");
        SerializedProperty spriteImportModeProperty = serializedObject.FindProperty("spriteImportMode");
        SerializedProperty spritePixelsPerUnitProperty = serializedObject.FindProperty("spritePixelsPerUnit");
        SerializedProperty mipmapEnabledProperty = serializedObject.FindProperty("mipmapEnabled");
        SerializedProperty alphaIsTransparencyProperty = serializedObject.FindProperty("alphaIsTransparency");
        SerializedProperty textureCompressionProperty = serializedObject.FindProperty("textureCompression");
        SerializedProperty filterModeProperty = serializedObject.FindProperty("textureSettings.m_FilterMode");
        SerializedProperty maxTextureSizeProperty = serializedObject.FindProperty("maxTextureSize");
        SerializedProperty spritePivotProperty = serializedObject.FindProperty("spritePivot");

        if (textureTypeProperty != null) textureTypeProperty.intValue = (int)TextureImporterType.Sprite;
        if (spriteImportModeProperty != null) spriteImportModeProperty.intValue = (int)SpriteImportMode.Multiple;
        if (spritePixelsPerUnitProperty != null) spritePixelsPerUnitProperty.floatValue = 256f;
        if (mipmapEnabledProperty != null) mipmapEnabledProperty.boolValue = false;
        if (alphaIsTransparencyProperty != null) alphaIsTransparencyProperty.boolValue = true;
        if (textureCompressionProperty != null) textureCompressionProperty.intValue = (int)TextureImporterCompression.Uncompressed;
        if (filterModeProperty != null) filterModeProperty.intValue = (int)FilterMode.Bilinear;
        if (maxTextureSizeProperty != null) maxTextureSizeProperty.intValue = 2048;
        if (spritePivotProperty != null) spritePivotProperty.vector2Value = new Vector2(0.5f, 0.5f);
        serializedObject.ApplyModifiedPropertiesWithoutUndo();

        SpriteRect[] spriteRects = BuildAtlasRects();
        dataProvider.SetSpriteRects(spriteRects);

        ISpriteNameFileIdDataProvider nameFileIdDataProvider = dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (nameFileIdDataProvider == null)
        {
            throw new InvalidOperationException($"Failed to acquire sprite name/file-id provider for '{ATLAS_PATH}'.");
        }

        List<SpriteNameFileIdPair> nameFileIdPairs = new(spriteRects.Length);
        for (int i = 0; i < spriteRects.Length; i++)
        {
            nameFileIdPairs.Add(new SpriteNameFileIdPair(spriteRects[i].name, spriteRects[i].spriteID));
        }

        nameFileIdDataProvider.SetNameFileIdPairs(nameFileIdPairs);
        dataProvider.Apply();
    }

    private static SpriteRect[] BuildAtlasRects()
    {
        SpriteRect[] slices = new SpriteRect[Variants.Length];
        for (int index = 0; index < Variants.Length; index++)
        {
            int row = index / ATLAS_GRID_SIZE;
            int column = index % ATLAS_GRID_SIZE;
            slices[index] = new SpriteRect
            {
                name = Variants[index].SpriteName,
                rect = new Rect(
                    column * ATLAS_CELL_SIZE,
                    (ATLAS_GRID_SIZE - 1 - row) * ATLAS_CELL_SIZE,
                    ATLAS_CELL_SIZE,
                    ATLAS_CELL_SIZE),
                alignment = SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f),
                border = Vector4.zero,
                spriteID = GUID.Generate()
            };
        }

        return slices;
    }

    private static void MoveAssetIfExists(string sourcePath, string destinationPath)
    {
        if (AssetDatabase.LoadAssetAtPath<Object>(sourcePath) == null)
        {
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<Object>(destinationPath) != null || File.Exists(destinationPath))
        {
            if (!AssetDatabase.DeleteAsset(destinationPath))
            {
                throw new InvalidOperationException($"Failed to clear existing legacy asset at '{destinationPath}'.");
            }
        }

        string moveError = AssetDatabase.MoveAsset(sourcePath, destinationPath);
        if (!string.IsNullOrWhiteSpace(moveError))
        {
            throw new InvalidOperationException(
                $"Failed to move legacy projectile asset from '{sourcePath}' to '{destinationPath}': {moveError}");
        }
    }

    private static Sprite ResolveSlice(SpriteLookup spriteLookup, string sliceName)
    {
        if (!spriteLookup.TryGet(sliceName, out Sprite sprite) || sprite == null)
        {
            throw new MissingReferenceException($"Missing projectile atlas slice '{sliceName}' in '{ATLAS_PATH}'.");
        }

        return sprite;
    }

    private static ProjectileDefinitionSO CreateOrUpdateDefinition(
        ProjectileVariantDefinition variant,
        Sprite sprite,
        Projectile projectilePrefab)
    {
        string path = $"{PROJECTILE_FOLDER}/{variant.Id}.asset";
        ProjectileDefinitionSO definition = AssetDatabase.LoadAssetAtPath<ProjectileDefinitionSO>(path);
        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<ProjectileDefinitionSO>();
            AssetDatabase.CreateAsset(definition, path);
        }

        SerializedObject serializedObject = new(definition);
        SetString(serializedObject, "id", variant.Id);
        SetString(serializedObject, "displayName", variant.DisplayName);
        SetObject(serializedObject, "projectilePrefab", projectilePrefab);
        SetFloat(serializedObject, "damageMultiplier", 1f);
        SetFloat(serializedObject, "speedMultiplier", 1f);
        SetFloat(serializedObject, "lifetimeMultiplier", 1f);
        SetFloat(serializedObject, "scaleMultiplier", 1f);
        SetObject(serializedObject, "sprite", sprite);
        SetObject(serializedObject, "animatorController", null);
        SetObject(serializedObject, "material", null);
        SetInt(serializedObject, "sortingOrder", 0);
        SetObject(serializedObject, "icon", null);
        SetColor(serializedObject, "debugColor", variant.DebugColor);
        SetInt(serializedObject, "launchSfxKey", 0);
        SetObject(serializedObject, "launchVfxPrefab", null);
        SetObject(serializedObject, "impactVfxPrefab", null);
        SetBool(serializedObject, "useDirectionFacing", variant.UseDirectionFacing);
        SetFloat(serializedObject, "rotationOffset", variant.RotationOffset);
        SetString(serializedObject, "launchAnimationTrigger", string.Empty);
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);
        return definition;
    }

    private static Projectile CreateOrUpdatePrefab(GameObject basePrefabAsset, ProjectileVariantDefinition variant, Sprite sprite)
    {
        string path = $"{PREFAB_FOLDER}/{variant.Id}.prefab";
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefabAsset);
        if (instance == null)
        {
            throw new InvalidOperationException($"Failed to instantiate base projectile prefab from '{BASE_PREFAB_PATH}'.");
        }

        try
        {
            instance.name = variant.Id;
            ConfigurePrefab(instance, variant, sprite);

            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(instance, path, out bool success);
            if (!success || savedPrefab == null)
            {
                throw new InvalidOperationException($"Unity failed to save projectile prefab variant '{path}'.");
            }

            if (PrefabUtility.GetPrefabAssetType(savedPrefab) != PrefabAssetType.Variant)
            {
                throw new InvalidOperationException($"Projectile prefab '{path}' was not saved as a prefab variant.");
            }

            Projectile projectile = savedPrefab.GetComponent<Projectile>();
            if (projectile == null)
            {
                throw new MissingComponentException($"{path} has no {nameof(Projectile)} component.");
            }

            return projectile;
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    private static void ConfigurePrefab(GameObject root, ProjectileVariantDefinition variant, Sprite sprite)
    {
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one * variant.RootScale;
        EditorUtility.SetDirty(root.transform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(root.transform);

        ConfigureProjectileComponent(root);
        ConfigureCollider(root, variant);
        ConfigureAnimator(root);
        ConfigureRenderer(root, sprite, variant);
    }

    private static void ConfigureProjectileComponent(GameObject root)
    {
        Projectile projectile = root.GetComponent<Projectile>();
        if (projectile == null)
        {
            throw new MissingComponentException($"{root.name} requires a root {nameof(Projectile)} component.");
        }

        Rigidbody2D rigidbody = root.GetComponent<Rigidbody2D>();
        if (rigidbody == null)
        {
            throw new MissingComponentException($"{root.name} requires a root {nameof(Rigidbody2D)} component.");
        }

        SerializedObject serializedObject = new(projectile);
        SetFloat(serializedObject, "moveSpeed", 10f);
        SetFloat(serializedObject, "maxLifetime", 5f);
        SetInt(serializedObject, "maxHitCount", 1);
        SetObject(serializedObject, "rb", rigidbody);
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(projectile);
        PrefabUtility.RecordPrefabInstancePropertyModifications(projectile);
    }

    private static void ConfigureCollider(GameObject root, ProjectileVariantDefinition variant)
    {
        Collider2D collider = root.GetComponent<Collider2D>();
        if (collider == null)
        {
            throw new MissingComponentException($"{root.name} requires a root Collider2D.");
        }

        switch (variant.ColliderKind)
        {
            case ProjectileColliderKind.Box:
                if (collider is not BoxCollider2D boxCollider)
                {
                    Object.DestroyImmediate(collider, true);
                    boxCollider = root.AddComponent<BoxCollider2D>();
                }

                boxCollider.isTrigger = true;
                boxCollider.offset = variant.ColliderOffset;
                boxCollider.size = variant.ColliderSize;
                EditorUtility.SetDirty(boxCollider);
                PrefabUtility.RecordPrefabInstancePropertyModifications(boxCollider);
                break;

            case ProjectileColliderKind.Circle:
            default:
                if (collider is not CircleCollider2D circleCollider)
                {
                    Object.DestroyImmediate(collider, true);
                    circleCollider = root.AddComponent<CircleCollider2D>();
                }

                circleCollider.isTrigger = true;
                circleCollider.offset = variant.ColliderOffset;
                circleCollider.radius = variant.ColliderRadius;
                EditorUtility.SetDirty(circleCollider);
                PrefabUtility.RecordPrefabInstancePropertyModifications(circleCollider);
                break;
        }
    }

    private static void ConfigureRenderer(GameObject root, Sprite sprite, ProjectileVariantDefinition variant)
    {
        SpriteRenderer spriteRenderer = root.GetComponentInChildren<SpriteRenderer>(true);
        if (spriteRenderer == null)
        {
            throw new MissingComponentException($"{root.name} has no child SpriteRenderer.");
        }

        spriteRenderer.flipX = false;
        spriteRenderer.flipY = false;
        spriteRenderer.color = Color.white;
        spriteRenderer.sortingOrder = 0;
        spriteRenderer.material = null;
        spriteRenderer.drawMode = SpriteDrawMode.Simple;
        spriteRenderer.size = Vector2.one;
        spriteRenderer.transform.localPosition = Vector3.zero;
        spriteRenderer.transform.localRotation = Quaternion.identity;
        spriteRenderer.transform.localScale = Vector3.one * variant.RenderScale;

        SerializedObject spriteRendererObject = new(spriteRenderer);
        SetObject(spriteRendererObject, "m_Sprite", sprite);
        spriteRendererObject.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(spriteRenderer);
        EditorUtility.SetDirty(spriteRenderer.transform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(spriteRenderer);
        PrefabUtility.RecordPrefabInstancePropertyModifications(spriteRenderer.transform);

        EntityRenderer entityRenderer = root.GetComponent<EntityRenderer>();
        if (entityRenderer == null)
        {
            throw new MissingComponentException($"{root.name} requires an {nameof(EntityRenderer)} component.");
        }

        SerializedObject serializedObject = new(entityRenderer);
        SetObject(serializedObject, "spriteRenderer", spriteRenderer);
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(entityRenderer);
        PrefabUtility.RecordPrefabInstancePropertyModifications(entityRenderer);
    }

    private static void ConfigureAnimator(GameObject root)
    {
        Animator animator = root.GetComponentInChildren<Animator>(true);
        if (animator != null)
        {
            animator.runtimeAnimatorController = null;
            EditorUtility.SetDirty(animator);
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
        }
    }

    private static void UpdateConsumers(Dictionary<string, ProjectileDefinitionSO> definitionsById)
    {
        for (int i = 0; i < ConsumerBindings.Length; i++)
        {
            ConsumerBinding binding = ConsumerBindings[i];
            ProjectileDefinitionSO definition = ResolveDefinition(definitionsById, binding.ProjectileId);
            if (binding.Kind == ConsumerKind.Weapon)
            {
                ApplyWeaponBinding(binding.ContentId, definition);
            }
            else
            {
                ApplyEnemyBinding(binding.ContentId, definition);
            }
        }
    }

    private static ProjectileDefinitionSO ResolveDefinition(Dictionary<string, ProjectileDefinitionSO> definitionsById, string projectileId)
    {
        if (!definitionsById.TryGetValue(projectileId, out ProjectileDefinitionSO definition) || definition == null)
        {
            throw new MissingReferenceException($"Missing projectile definition '{projectileId}'.");
        }

        return definition;
    }

    private static void ApplyWeaponBinding(string weaponId, ProjectileDefinitionSO definition)
    {
        string path = $"{GameContentAssetPaths.WeaponsData}/{weaponId}.asset";
        WeaponDataSO weapon = LoadRequired<WeaponDataSO>(path);
        SerializedObject serializedObject = new(weapon);
        SerializedProperty sequenceProjectiles = serializedObject.FindProperty("sequenceProjectileList");
        if (sequenceProjectiles == null || sequenceProjectiles.arraySize == 0)
        {
            throw new MissingFieldException(nameof(WeaponDataSO), "sequenceProjectileList");
        }

        for (int i = 0; i < sequenceProjectiles.arraySize; i++)
        {
            SerializedProperty element = sequenceProjectiles.GetArrayElementAtIndex(i);
            SerializedProperty projectileDefinitionProperty = element.FindPropertyRelative("projectileDefinition");
            if (projectileDefinitionProperty == null)
            {
                throw new MissingFieldException(nameof(WeaponSequenceProjectileDefinition), "projectileDefinition");
            }

            projectileDefinitionProperty.objectReferenceValue = definition;
        }

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(weapon);
    }

    private static void ApplyEnemyBinding(string enemyId, ProjectileDefinitionSO definition)
    {
        string path = $"{GameContentAssetPaths.EnemiesData}/CyberOrange/{enemyId}.asset";
        EnemySO enemy = LoadRequired<EnemySO>(path);
        SerializedObject serializedObject = new(enemy);

        bool bound = false;
        SerializedProperty normalAttackProperty = serializedObject.FindProperty("normalAttackProjectileDefinition");
        if (normalAttackProperty != null)
        {
            normalAttackProperty.objectReferenceValue = definition;
            bound = true;
        }

        SerializedProperty attackProperty = serializedObject.FindProperty("attackProjectileDefinition");
        if (attackProperty != null)
        {
            attackProperty.objectReferenceValue = definition;
            bound = true;
        }

        if (!bound)
        {
            throw new InvalidOperationException($"{path} has no projectile definition field that can be bound.");
        }

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(enemy);
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
        if (string.IsNullOrWhiteSpace(folderPath) || AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
        string folderName = Path.GetFileName(folderPath);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folderName);
    }

    private static void SetString(SerializedObject serializedObject, string propertyName, string value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            throw new MissingFieldException(serializedObject.targetObject.GetType().Name, propertyName);
        }

        property.stringValue = value ?? string.Empty;
    }

    private static void SetInt(SerializedObject serializedObject, string propertyName, int value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            throw new MissingFieldException(serializedObject.targetObject.GetType().Name, propertyName);
        }

        property.intValue = value;
    }

    private static void SetFloat(SerializedObject serializedObject, string propertyName, float value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            throw new MissingFieldException(serializedObject.targetObject.GetType().Name, propertyName);
        }

        property.floatValue = value;
    }

    private static void SetBool(SerializedObject serializedObject, string propertyName, bool value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            throw new MissingFieldException(serializedObject.targetObject.GetType().Name, propertyName);
        }

        property.boolValue = value;
    }

    private static void SetColor(SerializedObject serializedObject, string propertyName, Color value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            throw new MissingFieldException(serializedObject.targetObject.GetType().Name, propertyName);
        }

        property.colorValue = value;
    }

    private static void SetObject(SerializedObject serializedObject, string propertyName, Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            throw new MissingFieldException(serializedObject.targetObject.GetType().Name, propertyName);
        }

        property.objectReferenceValue = value;
    }

    private enum ConsumerKind
    {
        Weapon,
        Enemy
    }

    private enum ProjectileColliderKind
    {
        Circle,
        Box
    }

    private readonly struct ProjectileVariantDefinition
    {
        public ProjectileVariantDefinition(
            string id,
            string displayName,
            string spriteName,
            ProjectileColliderKind colliderKind,
            float colliderRadius,
            Vector2 colliderSize,
            Vector2 colliderOffset,
            Color debugColor,
            float rootScale,
            float renderScale,
            bool useDirectionFacing,
            float rotationOffset)
        {
            Id = id;
            DisplayName = displayName;
            SpriteName = spriteName;
            ColliderKind = colliderKind;
            ColliderRadius = colliderRadius;
            ColliderSize = colliderSize;
            ColliderOffset = colliderOffset;
            DebugColor = debugColor;
            RootScale = rootScale;
            RenderScale = renderScale;
            UseDirectionFacing = useDirectionFacing;
            RotationOffset = rotationOffset;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string SpriteName { get; }
        public ProjectileColliderKind ColliderKind { get; }
        public float ColliderRadius { get; }
        public Vector2 ColliderSize { get; }
        public Vector2 ColliderOffset { get; }
        public Color DebugColor { get; }
        public float RootScale { get; }
        public float RenderScale { get; }
        public bool UseDirectionFacing { get; }
        public float RotationOffset { get; }
    }

    private readonly struct ConsumerBinding
    {
        public ConsumerBinding(string contentId, ConsumerKind kind, string projectileId)
        {
            ContentId = contentId;
            Kind = kind;
            ProjectileId = projectileId;
        }

        public string ContentId { get; }
        public ConsumerKind Kind { get; }
        public string ProjectileId { get; }
    }

    private readonly struct SpriteLookup
    {
        private readonly Dictionary<string, Sprite> spritesByName;

        public SpriteLookup(Dictionary<string, Sprite> spritesByName)
        {
            this.spritesByName = spritesByName;
        }

        public bool TryGet(string name, out Sprite sprite)
        {
            sprite = null;
            return spritesByName != null && spritesByName.TryGetValue(name, out sprite);
        }
    }
}
#endif

