#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class NeonRoundCatDefaultCharacterBuilder
{
    private const string MENU_PATH = "Tools/Orange/Content/Build Neon Round Cat Default Character";
    private const string CHARACTER1_PATH = "Assets/GameContent/Characters/Data/Character1.asset";
    private const string CHARACTER_PATH = "Assets/GameContent/Characters/Data/NeonRoundCat.asset";
    private const string ANIMATION_CONFIG_PATH = "Assets/GameContent/Characters/Data/NeonRoundCatAnimationConfig.asset";
    private const string PROCEDURAL_PROFILE_PATH = "Assets/GameContent/Characters/Data/NeonRoundCatProceduralAnimationProfile.asset";
    private const string SPRITE_PATH = "Assets/GameContent/Characters/Sprites/NeonRoundCat/NeonRoundCatHero_512.png";
    private const string PLAYER_PREFAB_PATH = "Assets/GameContent/Characters/Prefabs/Character.prefab";
    private const string GAME_CATALOG_PATH = "Assets/GameContent/Catalog/Game Content Catalog.asset";
    private const string TEST_CATALOG_PATH = "Assets/GameContent/Catalog/Test Game Content Catalog.asset";
    private const string VISUAL_ROOT_NAME = "VisualRoot";
    private const float CAT_PIXELS_PER_UNIT = 256f;

    [MenuItem(MENU_PATH)]
    public static void Build()
    {
        EnsureSpriteImportSettings();

        Sprite characterSprite = LoadRequired<Sprite>(SPRITE_PATH);
        EntityAnimationConfig animationConfig = CreateOrUpdateAnimationConfig();
        ProceduralAnimationProfileSO profile = CreateOrUpdateProceduralProfile();
        CharacterDataSO character = CreateOrUpdateCharacter(characterSprite, animationConfig, profile);

        UpdatePlayerPrefab(characterSprite);
        SetCatalogDefaultCharacter(GAME_CATALOG_PATH, character);
        SetCatalogDefaultCharacter(TEST_CATALOG_PATH, character);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[{nameof(NeonRoundCatDefaultCharacterBuilder)}] Built default character '{character.name}'.");
    }

    private static void EnsureSpriteImportSettings()
    {
        TextureImporter importer = AssetImporter.GetAtPath(SPRITE_PATH) as TextureImporter;
        if (importer == null)
        {
            throw new FileNotFoundException($"Missing sprite texture importer at {SPRITE_PATH}.");
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = CAT_PIXELS_PER_UNIT;
        importer.spritePivot = new Vector2(0.5f, 0.5f);
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.maxTextureSize = 512;

        TextureImporterPlatformSettings defaultSettings = importer.GetDefaultPlatformTextureSettings();
        defaultSettings.maxTextureSize = 512;
        importer.SetPlatformTextureSettings(defaultSettings);

        importer.SaveAndReimport();
    }

    private static EntityAnimationConfig CreateOrUpdateAnimationConfig()
    {
        EntityAnimationConfig config = GetOrCreateAsset<EntityAnimationConfig>(ANIMATION_CONFIG_PATH);
        SerializedObject serializedObject = new(config);
        SetObject(serializedObject, "AnimatorController", null);
        SetInt(serializedObject, "DefaultFacingDirection", (int)EntityAnimationConfig.FacingDirection.Right);
        SetString(serializedObject, "Idle", "Idle");
        SetString(serializedObject, "Move", "Move");
        SetString(serializedObject, "Charge", "Charge");
        SetString(serializedObject, "Float", "Float");
        SetString(serializedObject, "Open", "Open");
        SetString(serializedObject, "Attack", "Attack");
        SetString(serializedObject, "Attack1", "Attack1");
        SetString(serializedObject, "Attack2", "Attack2");
        SetString(serializedObject, "Attack3", "Attack3");
        SetString(serializedObject, "MeleeAttack", "MeleeAttack");
        SetString(serializedObject, "MeleeAttack1", "MeleeAttack1");
        SetString(serializedObject, "MeleeAttack2", "MeleeAttack2");
        SetString(serializedObject, "MeleeAttack3", "MeleeAttack3");
        SetString(serializedObject, "RangeAttack", "RangeAttack");
        SetString(serializedObject, "RangeAttack1", "RangeAttack1");
        SetString(serializedObject, "RangeAttack2", "RangeAttack2");
        SetString(serializedObject, "RangeAttack3", "RangeAttack3");
        SetString(serializedObject, "Death", "Death");
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(config);
        return config;
    }

    private static ProceduralAnimationProfileSO CreateOrUpdateProceduralProfile()
    {
        ProceduralAnimationProfileSO profile = GetOrCreateAsset<ProceduralAnimationProfileSO>(PROCEDURAL_PROFILE_PATH);
        SerializedObject serializedObject = new(profile);
        SerializedProperty states = RequireProperty(serializedObject, "states");
        states.arraySize = 5;

        ConfigureState(
            states.GetArrayElementAtIndex(0),
            "Spawn",
            0.5f,
            false,
            Curve(0f, -0.12f, 0.25f, 0.035f, 1f, 0f),
            Curve(0f, 0.06f, 0.25f, -0.025f, 1f, 0f),
            Curve(0f, -0.045f, 0.35f, 0.01f, 1f, 0f),
            Curve(0f, 0.12f, 0.3f, 0.05f, 1f, 0f),
            Curve(0f, 1f, 0.26f, 0.25f, 1f, 0f),
            0f,
            0.1f);

        ConfigureState(
            states.GetArrayElementAtIndex(1),
            "Idle",
            1.2f,
            true,
            Curve(0f, 0f, 0.5f, 0.018f, 1f, 0f),
            Curve(0f, 0f, 0.5f, -0.008f, 1f, 0f),
            Curve(0f, 0f, 0.5f, 0.006f, 1f, 0f),
            FlatCurve(),
            FlatCurve(),
            0f,
            0.035f);

        ConfigureState(
            states.GetArrayElementAtIndex(2),
            "Move",
            0.55f,
            true,
            Curve(0f, 0.035f, 0.5f, -0.018f, 1f, 0.035f),
            Curve(0f, -0.015f, 0.5f, 0.024f, 1f, -0.015f),
            Curve(0f, 0f, 0.5f, 0.018f, 1f, 0f),
            FlatCurve(),
            FlatCurve(),
            0f,
            0.06f);

        ConfigureState(
            states.GetArrayElementAtIndex(3),
            "Hurt",
            0.14f,
            false,
            Curve(0f, 0.1f, 1f, 0f),
            Curve(0f, -0.045f, 1f, 0f),
            Curve(0f, 0f, 0.5f, -0.015f, 1f, 0f),
            Curve(0f, 0.32f, 1f, 0f),
            FlatCurve(),
            0f,
            0.075f);

        ConfigureState(
            states.GetArrayElementAtIndex(4),
            "Death",
            0.75f,
            false,
            Curve(0f, 0f, 0.5f, 0.06f, 1f, 0.12f),
            Curve(0f, 0f, 0.55f, -0.04f, 1f, -0.08f),
            Curve(0f, 0f, 1f, -0.08f),
            Curve(0f, 0f, 0.2f, 0.2f, 1f, 0f),
            Curve(0f, 0f, 1f, 1f),
            0f,
            0.08f);

        SetFloat(serializedObject, "hurtOverlayDuration", 0.14f);
        SetCurve(serializedObject, "hurtSquashCurve", Curve(0f, 0.1f, 1f, 0f));
        SetCurve(serializedObject, "hurtStretchCurve", Curve(0f, -0.045f, 1f, 0f));
        SetCurve(serializedObject, "hurtFlashCurve", Curve(0f, 0.32f, 1f, 0f));
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static CharacterDataSO CreateOrUpdateCharacter(
        Sprite characterSprite,
        EntityAnimationConfig animationConfig,
        ProceduralAnimationProfileSO profile)
    {
        CharacterDataSO source = LoadRequired<CharacterDataSO>(CHARACTER1_PATH);
        CharacterDataSO character = AssetDatabase.LoadAssetAtPath<CharacterDataSO>(CHARACTER_PATH);
        if (character == null)
        {
            EnsureFolder(Path.GetDirectoryName(CHARACTER_PATH)?.Replace('\\', '/'));
            character = ScriptableObject.CreateInstance<CharacterDataSO>();
            AssetDatabase.CreateAsset(character, CHARACTER_PATH);
        }

        EditorUtility.CopySerialized(source, character);
        character.name = "NeonRoundCat";

        SerializedObject serializedObject = new(character);
        SetString(serializedObject, "<CharacterName>k__BackingField", "\u9713\u8679\u732B");
        SetObject(serializedObject, "<CharacterIcon>k__BackingField", characterSprite);
        SetObject(serializedObject, "<CharacterSprite>k__BackingField", characterSprite);
        SetObject(serializedObject, "<CharacterAnimatorController>k__BackingField", null);
        SetObject(serializedObject, "<ProceduralAnimationConfig>k__BackingField", animationConfig);
        SetObject(serializedObject, "<ProceduralAnimationProfile>k__BackingField", profile);
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(character);
        return character;
    }

    private static void UpdatePlayerPrefab(Sprite characterSprite)
    {
        GameObject contents = PrefabUtility.LoadPrefabContents(PLAYER_PREFAB_PATH);
        try
        {
            SpriteRenderer spriteRenderer = contents.transform.Find("Render")?.GetComponent<SpriteRenderer>()
                ?? contents.GetComponentInChildren<SpriteRenderer>(true);
            if (spriteRenderer == null)
            {
                throw new MissingComponentException($"{PLAYER_PREFAB_PATH} has no child SpriteRenderer.");
            }

            spriteRenderer.sprite = characterSprite;
            EntityPrefabVisualRootNormalizer.NormalizeLoadedPrefab(contents, PLAYER_PREFAB_PATH);
            Transform visualRoot = contents.transform.Find(VISUAL_ROOT_NAME);
            if (visualRoot == null)
            {
                throw new MissingComponentException($"{PLAYER_PREFAB_PATH} has no {VISUAL_ROOT_NAME} after normalization.");
            }

            RemoveComponents<PlayerAnimationController>(contents);

            ProceduralEntityAnimationComponent proceduralAnimation =
                contents.GetComponent<ProceduralEntityAnimationComponent>();
            if (proceduralAnimation == null)
            {
                proceduralAnimation = contents.AddComponent<ProceduralEntityAnimationComponent>();
            }

            SerializedObject serializedProceduralAnimation = new(proceduralAnimation);
            SetObject(serializedProceduralAnimation, "visualRoot", visualRoot);
            serializedProceduralAnimation.ApplyModifiedPropertiesWithoutUndo();

            if (contents.GetComponent<PlayerProceduralAnimationDriver>() == null)
            {
                contents.AddComponent<PlayerProceduralAnimationDriver>();
            }

            Player player = contents.GetComponent<Player>();
            if (player != null)
            {
                SerializedObject serializedPlayer = new(player);
                SetObject(serializedPlayer, "characterData", null);
                serializedPlayer.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.SaveAsPrefabAsset(contents, PLAYER_PREFAB_PATH);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    private static void RemoveComponents<T>(GameObject root) where T : Component
    {
        T[] components = root.GetComponents<T>();
        for (int i = components.Length - 1; i >= 0; i--)
        {
            UnityEngine.Object.DestroyImmediate(components[i]);
        }
    }

    private static void SetCatalogDefaultCharacter(string catalogPath, CharacterDataSO character)
    {
        GameContentCatalogSO catalog = LoadRequired<GameContentCatalogSO>(catalogPath);
        SerializedObject serializedObject = new(catalog);
        SetObject(serializedObject, "defaultCharacter", character);
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(catalog);
    }

    private static void ConfigureState(
        SerializedProperty state,
        string stateName,
        float duration,
        bool loop,
        AnimationCurve squash,
        AnimationCurve stretch,
        AnimationCurve verticalOffset,
        AnimationCurve flash,
        AnimationCurve dissolve,
        float hueShift,
        float glowAmount)
    {
        SetString(state, "stateName", stateName);
        SetFloat(state, "duration", duration);
        SetBool(state, "loop", loop);
        SetFloat(state, "playbackSpeedMultiplier", 1f);
        SetCurve(state, "squashCurve", squash);
        SetCurve(state, "stretchCurve", stretch);
        SetCurve(state, "verticalOffsetCurve", verticalOffset);
        SetCurve(state, "flashCurve", flash);
        SetCurve(state, "dissolveCurve", dissolve);
        SetFloat(state, "hueShift", hueShift);
        SetFloat(state, "glowAmount", glowAmount);
    }

    private static T GetOrCreateAsset<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null)
        {
            return asset;
        }

        EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static T LoadRequired<T>(string path) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            throw new FileNotFoundException($"Missing required {typeof(T).Name} at {path}.");
        }

        return asset;
    }

    private static void EnsureFolder(string folderPath)
    {
        if (string.IsNullOrEmpty(folderPath) || AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
        string folderName = Path.GetFileName(folderPath);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folderName);
    }

    private static AnimationCurve FlatCurve()
    {
        return Curve(0f, 0f, 1f, 0f);
    }

    private static AnimationCurve Curve(params float[] timeValuePairs)
    {
        if (timeValuePairs == null || timeValuePairs.Length == 0 || timeValuePairs.Length % 2 != 0)
        {
            throw new System.ArgumentException("Curve requires time/value pairs.", nameof(timeValuePairs));
        }

        Keyframe[] keys = new Keyframe[timeValuePairs.Length / 2];
        for (int i = 0; i < keys.Length; i++)
        {
            keys[i] = new Keyframe(timeValuePairs[i * 2], timeValuePairs[i * 2 + 1]);
        }

        return new AnimationCurve(keys);
    }

    private static SerializedProperty RequireProperty(SerializedObject serializedObject, string propertyName)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            throw new MissingFieldException(serializedObject.targetObject.GetType().Name, propertyName);
        }

        return property;
    }

    private static SerializedProperty RequireRelativeProperty(SerializedProperty parent, string propertyName)
    {
        SerializedProperty property = parent.FindPropertyRelative(propertyName);
        if (property == null)
        {
            throw new MissingFieldException(parent.displayName, propertyName);
        }

        return property;
    }

    private static void SetObject(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
    {
        RequireProperty(serializedObject, propertyName).objectReferenceValue = value;
    }

    private static void SetString(SerializedObject serializedObject, string propertyName, string value)
    {
        RequireProperty(serializedObject, propertyName).stringValue = value;
    }

    private static void SetInt(SerializedObject serializedObject, string propertyName, int value)
    {
        RequireProperty(serializedObject, propertyName).intValue = value;
    }

    private static void SetFloat(SerializedObject serializedObject, string propertyName, float value)
    {
        RequireProperty(serializedObject, propertyName).floatValue = value;
    }

    private static void SetCurve(SerializedObject serializedObject, string propertyName, AnimationCurve value)
    {
        RequireProperty(serializedObject, propertyName).animationCurveValue = value;
    }

    private static void SetString(SerializedProperty parent, string propertyName, string value)
    {
        RequireRelativeProperty(parent, propertyName).stringValue = value;
    }

    private static void SetFloat(SerializedProperty parent, string propertyName, float value)
    {
        RequireRelativeProperty(parent, propertyName).floatValue = value;
    }

    private static void SetBool(SerializedProperty parent, string propertyName, bool value)
    {
        RequireRelativeProperty(parent, propertyName).boolValue = value;
    }

    private static void SetCurve(SerializedProperty parent, string propertyName, AnimationCurve value)
    {
        RequireRelativeProperty(parent, propertyName).animationCurveValue = value;
    }
}
#endif
