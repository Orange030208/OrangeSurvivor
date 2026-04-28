#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(
    fileName = "图片动画变体构建器设置",
    menuName = "Survivors/编辑器/图片动画变体构建器设置")]
public sealed class SpriteVariantAnimationBuilderSettings : ScriptableObject
{
    public const string DEFAULT_INPUT_ROOT = "Assets/Resources/Sprites";
    public const string DEFAULT_ANIMATION_OUTPUT_ROOT = "Assets/Resources/Animation";
    public const string DEFAULT_PREFAB_OUTPUT_ROOT = "Assets/Resources/Prefabs";

    public enum SpriteFrameSortMode
    {
        [InspectorName("按名称")]
        Name,

        [InspectorName("图集位置：从左上开始")]
        TexturePositionTopLeft,

        [InspectorName("图集位置：从左下开始")]
        TexturePositionBottomLeft
    }

    [SerializeField, FormerlySerializedAs("spriteInputRoot")]
    private DefaultAsset inputRoot;

    [SerializeField, HideInInspector]
    private string spriteInputRootPath = DEFAULT_INPUT_ROOT;

    [SerializeField]
    private DefaultAsset animationOutputRoot;

    [SerializeField, HideInInspector]
    private string animationOutputRootPath = DEFAULT_ANIMATION_OUTPUT_ROOT;

    [SerializeField]
    private DefaultAsset prefabOutputRoot;

    [SerializeField, HideInInspector]
    private string prefabOutputRootPath = DEFAULT_PREFAB_OUTPUT_ROOT;

    [SerializeField, HideInInspector]
    private DefaultAsset outputRoot;

    [SerializeField]
    private GameObject templatePrefab;

    [SerializeField]
    private string spriteRendererPath;

    [SerializeField]
    private bool assignControllerToPrefab = true;

    [SerializeField]
    private bool assignPreviewSpriteToPrefab = true;

    [SerializeField, Min(1f)]
    private float animationFrameRate = 12f;

    [SerializeField]
    private SpriteFrameSortMode spriteSortMode = SpriteFrameSortMode.TexturePositionTopLeft;

    [SerializeField]
    private bool includeNestedTextureFolders;

    [SerializeField]
    private bool loopClipsByDefault = true;

    [SerializeField]
    private List<string> nonLoopingClipNames = new();

    [SerializeField]
    private bool overwriteExistingAssets = true;

    [SerializeField]
    private bool removeStaleAnimatorStates = true;

    [SerializeField]
    private bool deleteStaleAnimationClips;

    [SerializeField]
    private bool selectGeneratedAssets = true;

    public string InputRootPath => ResolveFolderPath(inputRoot, spriteInputRootPath, DEFAULT_INPUT_ROOT);
    public string AnimationOutputRootPath =>
        ResolveFolderPath(animationOutputRoot, ResolveLegacyOutputFallback(animationOutputRootPath), DEFAULT_ANIMATION_OUTPUT_ROOT);
    public string PrefabOutputRootPath =>
        ResolveFolderPath(prefabOutputRoot, ResolveLegacyOutputFallback(prefabOutputRootPath), DEFAULT_PREFAB_OUTPUT_ROOT);
    public GameObject TemplatePrefab => templatePrefab;
    public string SpriteRendererPath => string.IsNullOrWhiteSpace(spriteRendererPath) ? string.Empty : spriteRendererPath.Trim();
    public bool AssignControllerToPrefab => assignControllerToPrefab;
    public bool AssignPreviewSpriteToPrefab => assignPreviewSpriteToPrefab;
    public float AnimationFrameRate => Mathf.Max(1f, animationFrameRate);
    public SpriteFrameSortMode FrameSortMode => spriteSortMode;
    public bool IncludeNestedTextureFolders => includeNestedTextureFolders;
    public bool LoopClipsByDefault => loopClipsByDefault;
    public IReadOnlyList<string> NonLoopingClipNames => nonLoopingClipNames;
    public bool OverwriteExistingAssets => overwriteExistingAssets;
    public bool RemoveStaleAnimatorStates => removeStaleAnimatorStates;
    public bool DeleteStaleAnimationClips => deleteStaleAnimationClips;
    public bool SelectGeneratedAssets => selectGeneratedAssets;

    public bool ShouldLoopClip(string clipName)
    {
        return loopClipsByDefault && !ContainsNonLoopingClipName(clipName);
    }

    public void ResetToDefaults()
    {
        inputRoot = AssetDatabase.LoadAssetAtPath<DefaultAsset>(DEFAULT_INPUT_ROOT);
        spriteInputRootPath = DEFAULT_INPUT_ROOT;
        animationOutputRoot = AssetDatabase.LoadAssetAtPath<DefaultAsset>(DEFAULT_ANIMATION_OUTPUT_ROOT);
        animationOutputRootPath = DEFAULT_ANIMATION_OUTPUT_ROOT;
        prefabOutputRoot = AssetDatabase.LoadAssetAtPath<DefaultAsset>(DEFAULT_PREFAB_OUTPUT_ROOT);
        prefabOutputRootPath = DEFAULT_PREFAB_OUTPUT_ROOT;
        outputRoot = null;
        spriteRendererPath = string.Empty;
        animationFrameRate = 12f;
        spriteSortMode = SpriteFrameSortMode.TexturePositionTopLeft;
        includeNestedTextureFolders = false;
        loopClipsByDefault = true;
        nonLoopingClipNames.Clear();
        templatePrefab = null;
        assignControllerToPrefab = true;
        assignPreviewSpriteToPrefab = true;
        overwriteExistingAssets = true;
        removeStaleAnimatorStates = true;
        deleteStaleAnimationClips = false;
        selectGeneratedAssets = true;
    }

    public void ResolveDefaultReferences()
    {
        inputRoot = ResolveFolderReference(inputRoot, spriteInputRootPath, DEFAULT_INPUT_ROOT);
        MigrateLegacyOutputRoot();
        animationOutputRoot = ResolveFolderReference(
            animationOutputRoot,
            ResolveLegacyOutputFallback(animationOutputRootPath),
            DEFAULT_ANIMATION_OUTPUT_ROOT);
        prefabOutputRoot = ResolveFolderReference(
            prefabOutputRoot,
            ResolveLegacyOutputFallback(prefabOutputRootPath),
            DEFAULT_PREFAB_OUTPUT_ROOT);
    }

    private void OnValidate()
    {
        animationFrameRate = Mathf.Max(1f, animationFrameRate);
        nonLoopingClipNames ??= new List<string>();
        MigrateLegacyOutputRoot();
    }

    private void MigrateLegacyOutputRoot()
    {
        if (outputRoot == null)
        {
            return;
        }

        animationOutputRoot ??= outputRoot;
        prefabOutputRoot ??= outputRoot;
    }

    private bool ContainsNonLoopingClipName(string clipName)
    {
        if (string.IsNullOrWhiteSpace(clipName) || nonLoopingClipNames == null)
        {
            return false;
        }

        string normalizedClipName = clipName.Trim();
        for (int i = 0; i < nonLoopingClipNames.Count; i++)
        {
            string configuredName = nonLoopingClipNames[i];
            if (string.IsNullOrWhiteSpace(configuredName))
            {
                continue;
            }

            if (string.Equals(configuredName.Trim(), normalizedClipName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static DefaultAsset ResolveFolderReference(DefaultAsset currentFolder, string fallbackPath, string defaultPath)
    {
        if (currentFolder != null)
        {
            string currentPath = AssetDatabase.GetAssetPath(currentFolder);
            if (AssetDatabase.IsValidFolder(currentPath))
            {
                return currentFolder;
            }
        }

        if (!string.IsNullOrWhiteSpace(fallbackPath))
        {
            DefaultAsset fallbackFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(NormalizeAssetPath(fallbackPath));
            if (fallbackFolder != null)
            {
                return fallbackFolder;
            }
        }

        return AssetDatabase.LoadAssetAtPath<DefaultAsset>(defaultPath);
    }

    private string ResolveLegacyOutputFallback(string fallbackPath)
    {
        if (outputRoot != null)
        {
            string outputRootPath = AssetDatabase.GetAssetPath(outputRoot);
            if (AssetDatabase.IsValidFolder(outputRootPath))
            {
                return NormalizeAssetPath(outputRootPath);
            }
        }

        return fallbackPath;
    }

    private static string ResolveFolderPath(DefaultAsset folderAsset, string fallbackPath, string defaultPath)
    {
        if (folderAsset != null)
        {
            string assetPath = AssetDatabase.GetAssetPath(folderAsset);
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return NormalizeAssetPath(assetPath);
            }
        }

        if (!string.IsNullOrWhiteSpace(fallbackPath))
        {
            return NormalizeAssetPath(fallbackPath);
        }

        return defaultPath;
    }

    private static string CombineAssetPath(string folder, string child)
    {
        return $"{NormalizeAssetPath(folder)}/{child.TrimStart('/', '\\')}";
    }

    private static string NormalizeAssetPath(string assetPath)
    {
        return assetPath.Replace('\\', '/').Trim().TrimEnd('/');
    }
}

[CustomEditor(typeof(SpriteVariantAnimationBuilderSettings))]
internal sealed class SpriteVariantAnimationBuilderSettingsEditor : Editor
{
    private SerializedProperty inputRoot;
    private SerializedProperty animationOutputRoot;
    private SerializedProperty prefabOutputRoot;
    private SerializedProperty templatePrefab;
    private SerializedProperty spriteRendererPath;
    private SerializedProperty assignControllerToPrefab;
    private SerializedProperty assignPreviewSpriteToPrefab;
    private SerializedProperty animationFrameRate;
    private SerializedProperty spriteSortMode;
    private SerializedProperty includeNestedTextureFolders;
    private SerializedProperty loopClipsByDefault;
    private SerializedProperty nonLoopingClipNames;
    private SerializedProperty overwriteExistingAssets;
    private SerializedProperty removeStaleAnimatorStates;
    private SerializedProperty deleteStaleAnimationClips;
    private SerializedProperty selectGeneratedAssets;

    private void OnEnable()
    {
        inputRoot = serializedObject.FindProperty("inputRoot");
        animationOutputRoot = serializedObject.FindProperty("animationOutputRoot");
        prefabOutputRoot = serializedObject.FindProperty("prefabOutputRoot");
        templatePrefab = serializedObject.FindProperty("templatePrefab");
        spriteRendererPath = serializedObject.FindProperty("spriteRendererPath");
        assignControllerToPrefab = serializedObject.FindProperty("assignControllerToPrefab");
        assignPreviewSpriteToPrefab = serializedObject.FindProperty("assignPreviewSpriteToPrefab");
        animationFrameRate = serializedObject.FindProperty("animationFrameRate");
        spriteSortMode = serializedObject.FindProperty("spriteSortMode");
        includeNestedTextureFolders = serializedObject.FindProperty("includeNestedTextureFolders");
        loopClipsByDefault = serializedObject.FindProperty("loopClipsByDefault");
        nonLoopingClipNames = serializedObject.FindProperty("nonLoopingClipNames");
        overwriteExistingAssets = serializedObject.FindProperty("overwriteExistingAssets");
        removeStaleAnimatorStates = serializedObject.FindProperty("removeStaleAnimatorStates");
        deleteStaleAnimationClips = serializedObject.FindProperty("deleteStaleAnimationClips");
        selectGeneratedAssets = serializedObject.FindProperty("selectGeneratedAssets");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawSectionTitle("目录");
        EditorGUILayout.PropertyField(inputRoot, new GUIContent("输入根目录"));
        EditorGUILayout.PropertyField(
            animationOutputRoot,
            new GUIContent("动画输出目录", "动画片段和 Animator Controller 会直接生成到此目录下的变体文件夹中，不会再自动追加 Animation 子目录。"));
        EditorGUILayout.PropertyField(
            prefabOutputRoot,
            new GUIContent("预制体输出目录", "预制体变体会直接生成到此目录，不会再自动追加 Prefabs 子目录。"));

        EditorGUILayout.Space(6f);
        DrawSectionTitle("预制体");
        EditorGUILayout.PropertyField(templatePrefab, new GUIContent("模板预制体"));
        EditorGUILayout.PropertyField(
            spriteRendererPath,
            new GUIContent(
                "精灵渲染器路径",
                "相对于挂 Animator 的对象填写 SpriteRenderer 所在子节点路径。SpriteRenderer 在根节点时留空；例如 Visual/Body。不要包含预制体根节点名称。"));
        EditorGUILayout.PropertyField(assignControllerToPrefab, new GUIContent("写入动画控制器"));
        EditorGUILayout.PropertyField(assignPreviewSpriteToPrefab, new GUIContent("写入预览精灵"));

        EditorGUILayout.Space(6f);
        DrawSectionTitle("动画");
        EditorGUILayout.PropertyField(animationFrameRate, new GUIContent("帧率"));
        EditorGUILayout.PropertyField(spriteSortMode, new GUIContent("精灵排序"));
        EditorGUILayout.PropertyField(includeNestedTextureFolders, new GUIContent("包含子文件夹图片"));
        EditorGUILayout.PropertyField(loopClipsByDefault, new GUIContent("默认循环"));
        EditorGUILayout.PropertyField(nonLoopingClipNames, new GUIContent("非循环动画名称"), true);

        EditorGUILayout.Space(6f);
        DrawSectionTitle("生成");
        EditorGUILayout.PropertyField(overwriteExistingAssets, new GUIContent("覆盖已有资源"));
        EditorGUILayout.PropertyField(removeStaleAnimatorStates, new GUIContent("移除过期状态"));
        EditorGUILayout.PropertyField(deleteStaleAnimationClips, new GUIContent("删除过期动画片段"));
        EditorGUILayout.PropertyField(selectGeneratedAssets, new GUIContent("选中生成资源"));

        serializedObject.ApplyModifiedProperties();
    }

    private static void DrawSectionTitle(string title)
    {
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
    }
}
#endif
