#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(
    fileName = "Sprite Variant Animation Builder Settings",
    menuName = "Survivors/Editor/Sprite Variant Animation Builder Settings")]
public sealed class SpriteVariantAnimationBuilderSettings : ScriptableObject
{
    public const string DEFAULT_SPRITE_INPUT_ROOT = "Assets/Resources/Sprites";
    public const string DEFAULT_ANIMATION_OUTPUT_ROOT = "Assets/Resources/Animation";
    public const string DEFAULT_PREFAB_OUTPUT_ROOT = "Assets/Resources/Prefabs";

    public enum SpriteFrameSortMode
    {
        Name,
        TexturePositionTopLeft,
        TexturePositionBottomLeft
    }

    [Header("Folders")]
    [SerializeField] private DefaultAsset spriteInputRoot;
    [SerializeField] private string spriteInputRootPath = DEFAULT_SPRITE_INPUT_ROOT;
    [SerializeField] private DefaultAsset animationOutputRoot;
    [SerializeField] private string animationOutputRootPath = DEFAULT_ANIMATION_OUTPUT_ROOT;
    [SerializeField] private DefaultAsset prefabOutputRoot;
    [SerializeField] private string prefabOutputRootPath = DEFAULT_PREFAB_OUTPUT_ROOT;

    [Header("Prefab")]
    [SerializeField] private GameObject templatePrefab;
    [SerializeField] private string spriteRendererPath;
    [SerializeField] private bool assignControllerToPrefab = true;
    [SerializeField] private bool assignPreviewSpriteToPrefab = true;

    [Header("Animation")]
    [SerializeField, Min(1f)] private float animationFrameRate = 12f;
    [SerializeField] private SpriteFrameSortMode spriteSortMode = SpriteFrameSortMode.TexturePositionTopLeft;
    [SerializeField] private bool includeNestedTextureFolders;
    [SerializeField] private bool loopClipsByDefault = true;

    [Header("Generation")]
    [SerializeField] private bool overwriteExistingAssets = true;
    [SerializeField] private bool removeStaleAnimatorStates = true;
    [SerializeField] private bool deleteStaleAnimationClips;
    [SerializeField] private bool selectGeneratedAssets = true;

    public string SpriteInputRootPath => ResolveFolderPath(spriteInputRoot, spriteInputRootPath, DEFAULT_SPRITE_INPUT_ROOT);
    public string AnimationOutputRootPath => ResolveFolderPath(animationOutputRoot, animationOutputRootPath, DEFAULT_ANIMATION_OUTPUT_ROOT);
    public string PrefabOutputRootPath => ResolveFolderPath(prefabOutputRoot, prefabOutputRootPath, DEFAULT_PREFAB_OUTPUT_ROOT);
    public GameObject TemplatePrefab => templatePrefab;
    public string SpriteRendererPath => string.IsNullOrWhiteSpace(spriteRendererPath) ? string.Empty : spriteRendererPath.Trim();
    public bool AssignControllerToPrefab => assignControllerToPrefab;
    public bool AssignPreviewSpriteToPrefab => assignPreviewSpriteToPrefab;
    public float AnimationFrameRate => Mathf.Max(1f, animationFrameRate);
    public SpriteFrameSortMode FrameSortMode => spriteSortMode;
    public bool IncludeNestedTextureFolders => includeNestedTextureFolders;
    public bool LoopClipsByDefault => loopClipsByDefault;
    public bool OverwriteExistingAssets => overwriteExistingAssets;
    public bool RemoveStaleAnimatorStates => removeStaleAnimatorStates;
    public bool DeleteStaleAnimationClips => deleteStaleAnimationClips;
    public bool SelectGeneratedAssets => selectGeneratedAssets;

    public void ResetToDefaults()
    {
        spriteInputRootPath = DEFAULT_SPRITE_INPUT_ROOT;
        animationOutputRootPath = DEFAULT_ANIMATION_OUTPUT_ROOT;
        prefabOutputRootPath = DEFAULT_PREFAB_OUTPUT_ROOT;
        spriteRendererPath = string.Empty;
        animationFrameRate = 12f;
        spriteSortMode = SpriteFrameSortMode.TexturePositionTopLeft;
        includeNestedTextureFolders = false;
        loopClipsByDefault = true;
        templatePrefab = null;
        assignControllerToPrefab = true;
        assignPreviewSpriteToPrefab = true;
        overwriteExistingAssets = true;
        removeStaleAnimatorStates = true;
        deleteStaleAnimationClips = false;
        selectGeneratedAssets = true;
        ResolveDefaultReferences();
    }

    public void ResolveDefaultReferences()
    {
        spriteInputRoot = AssetDatabase.LoadAssetAtPath<DefaultAsset>(spriteInputRootPath);
        animationOutputRoot = AssetDatabase.LoadAssetAtPath<DefaultAsset>(animationOutputRootPath);
        prefabOutputRoot = AssetDatabase.LoadAssetAtPath<DefaultAsset>(prefabOutputRootPath);

    }

    private void OnValidate()
    {
        animationFrameRate = Mathf.Max(1f, animationFrameRate);
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

    private static string NormalizeAssetPath(string assetPath)
    {
        return assetPath.Replace('\\', '/').Trim().TrimEnd('/');
    }
}
#endif
