#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public sealed class Projectile2AnimatorWindow : EditorWindow
{
    private const string DefaultSourceFolder = "Assets/Resources/Sprites/Projectils";
    private const string DefaultOutputFolder = "Assets/Resources/Animation/Projectile";
    private const float DefaultSampleRate = 12f;
    private const string SpritePropertyName = "m_Sprite";
    private const string StateName = "Idle";

    // These values are intentionally plain strings so the tool stays usable in fresh checkouts
    // without requiring a ScriptableObject settings asset.
    private string sourceFolder = DefaultSourceFolder;
    private string outputFolder = DefaultOutputFolder;
    private float sampleRate = DefaultSampleRate;
    private Vector2 scrollPosition;

    [MenuItem("Tools/Projectiles/Projectile2Animator")]
    private static void OpenWindow()
    {
        Projectile2AnimatorWindow window = GetWindow<Projectile2AnimatorWindow>();
        window.titleContent = new GUIContent("Projectile2Animator");
        window.minSize = new Vector2(520f, 300f);
        window.Show();
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Projectile To Animator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "遍历输入目录下的图片资源。每张图片会读取其 Multiple Sprite 子图，生成一套动画和 Animator Controller。",
            MessageType.Info);

        DrawFolderField("Source Folder", ref sourceFolder, DefaultSourceFolder);
        DrawFolderField("Output Folder", ref outputFolder, DefaultOutputFolder);

        sampleRate = EditorGUILayout.FloatField("Sample Rate", sampleRate);
        sampleRate = Mathf.Max(1f, sampleRate);

        EditorGUILayout.Space(12f);
        using (new EditorGUI.DisabledScope(!CanGenerate()))
        {
            if (GUILayout.Button("Generate Projectile Animators", GUILayout.Height(32f)))
            {
                Generate();
            }
        }

        if (!AssetDatabase.IsValidFolder(sourceFolder))
        {
            EditorGUILayout.HelpBox($"Source folder not found: {sourceFolder}", MessageType.Warning);
        }

        EditorGUILayout.EndScrollView();
    }

    private bool CanGenerate()
    {
        return IsProjectAssetPath(sourceFolder) && IsProjectAssetPath(outputFolder);
    }

    private void DrawFolderField(string label, ref string folderPath, string defaultPath)
    {
        EditorGUILayout.BeginHorizontal();
        folderPath = EditorGUILayout.TextField(label, folderPath);
        if (GUILayout.Button("Browse", GUILayout.Width(80f)))
        {
            string selectedFolder = EditorUtility.OpenFolderPanel(label, GetAbsoluteFolderPath(folderPath), string.Empty);
            if (!string.IsNullOrWhiteSpace(selectedFolder) && TryConvertToAssetPath(selectedFolder, out string assetPath))
            {
                folderPath = assetPath;
            }
            else if (!string.IsNullOrWhiteSpace(selectedFolder))
            {
                Debug.LogWarning($"[Projectile2Animator] Folder must be inside project: {selectedFolder}");
            }
        }

        if (GUILayout.Button("Reset", GUILayout.Width(80f)))
        {
            folderPath = defaultPath;
        }
        EditorGUILayout.EndHorizontal();
    }

    private void Generate()
    {
        if (!AssetDatabase.IsValidFolder(sourceFolder))
        {
            EditorUtility.DisplayDialog("Projectile2Animator", $"Source folder not found:\n{sourceFolder}", "OK");
            return;
        }

        EnsureFolder(outputFolder);

        // Collect textures under the source folder and generate assets per texture.
        string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { sourceFolder });
        List<string> texturePaths = textureGuids
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(IsSupportedImageAsset)
            .Distinct()
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (texturePaths.Count == 0)
        {
            EditorUtility.DisplayDialog("Projectile2Animator", $"No image assets found in:\n{sourceFolder}", "OK");
            return;
        }

        int generatedCount = 0;
        int skippedCount = 0;
        try
        {
            for (int i = 0; i < texturePaths.Count; i++)
            {
                string texturePath = texturePaths[i];
                EditorUtility.DisplayProgressBar(
                    "Projectile2Animator",
                    $"Generating {Path.GetFileNameWithoutExtension(texturePath)} ({i + 1}/{texturePaths.Count})",
                    (float)(i + 1) / texturePaths.Count);

                if (TryGenerateForTexture(texturePath))
                {
                    generatedCount++;
                }
                else
                {
                    skippedCount++;
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Projectile2Animator",
            $"Finished.\nGenerated: {generatedCount}\nSkipped: {skippedCount}",
            "OK");
    }

    private bool TryGenerateForTexture(string texturePath)
    {
        // Ensure we can load sub-sprites via LoadAllAssetRepresentationsAtPath.
        EnsureMultipleSpriteImport(texturePath);

        List<Sprite> sprites = LoadSprites(texturePath);
        if (sprites.Count == 0)
        {
            Debug.LogWarning($"[Projectile2Animator] No sub-sprites found in texture: {texturePath}");
            return false;
        }

        string projectileName = Path.GetFileNameWithoutExtension(texturePath);
        string projectileOutputFolder = $"{outputFolder}/{projectileName}";
        EnsureFolder(projectileOutputFolder);

        string clipPath = $"{projectileOutputFolder}/{projectileName}.anim";
        string controllerPath = $"{projectileOutputFolder}/{projectileName}.controller";

        AnimationClip clip = CreateOrUpdateAnimationClip(clipPath, sprites);
        CreateOrUpdateAnimatorController(controllerPath, clip);

        Debug.Log($"[Projectile2Animator] Generated animation assets for {projectileName} at {projectileOutputFolder}");
        return true;
    }

    private void EnsureMultipleSpriteImport(string texturePath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer == null)
        {
            throw new InvalidOperationException($"[Projectile2Animator] TextureImporter not found for path: {texturePath}");
        }

        bool changed = false;
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            changed = true;
        }

        if (importer.spriteImportMode != SpriteImportMode.Multiple)
        {
            importer.spriteImportMode = SpriteImportMode.Multiple;
            changed = true;
        }

        if (changed)
        {
            importer.SaveAndReimport();
        }
    }

    private List<Sprite> LoadSprites(string texturePath)
    {
        return AssetDatabase.LoadAllAssetRepresentationsAtPath(texturePath)
            .OfType<Sprite>()
            .OrderBy(sprite => sprite.name, Comparer<string>.Create(EditorUtility.NaturalCompare))
            .ToList();
    }

    private AnimationClip CreateOrUpdateAnimationClip(string clipPath, List<Sprite> sprites)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, clipPath);
        }

        // Rebuild the clip each run so this tool is deterministic and idempotent.
        clip.frameRate = sampleRate;
        clip.name = Path.GetFileNameWithoutExtension(clipPath);

        foreach (EditorCurveBinding binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
        {
            AnimationUtility.SetObjectReferenceCurve(clip, binding, Array.Empty<ObjectReferenceKeyframe>());
        }

        EditorCurveBinding spriteBinding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = string.Empty,
            propertyName = SpritePropertyName
        };

        // Keyframes are spaced at 1 / sampleRate seconds.
        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Count];
        for (int i = 0; i < sprites.Count; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = i / sampleRate,
                value = sprites[i]
            };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyframes);
        SetLoopTime(clip, true);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private void CreateOrUpdateAnimatorController(string controllerPath, AnimationClip clip)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        }

        // Keep a single default state ("Idle") playing the generated clip.
        AnimatorControllerLayer layer = controller.layers.Length > 0 ? controller.layers[0] : null;
        if (layer == null)
        {
            layer = new AnimatorControllerLayer
            {
                name = "Base Layer",
                stateMachine = new AnimatorStateMachine()
            };
            controller.layers = new[] { layer };
        }

        AnimatorStateMachine stateMachine = layer.stateMachine;
        AnimatorState state = stateMachine.states
            .Select(child => child.state)
            .FirstOrDefault(existingState => existingState != null && existingState.name == StateName);

        if (state == null)
        {
            state = stateMachine.AddState(StateName);
        }

        state.motion = clip;
        state.speed = 1f;
        stateMachine.defaultState = state;
        EditorUtility.SetDirty(controller);
    }

    private static void SetLoopTime(AnimationClip clip, bool loopTime)
    {
        SerializedObject serializedClip = new SerializedObject(clip);
        SerializedProperty settings = serializedClip.FindProperty("m_AnimationClipSettings");
        if (settings == null)
        {
            return;
        }

        SerializedProperty loopProperty = settings.FindPropertyRelative("m_LoopTime");
        if (loopProperty == null)
        {
            return;
        }

        loopProperty.boolValue = loopTime;
        serializedClip.ApplyModifiedPropertiesWithoutUndo();
    }

    private static bool IsSupportedImageAsset(string assetPath)
    {
        string extension = Path.GetExtension(assetPath);
        return string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(extension, ".psd", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(extension, ".tga", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase);
    }

    private static void EnsureFolder(string assetFolderPath)
    {
        if (AssetDatabase.IsValidFolder(assetFolderPath))
        {
            return;
        }

        // Create folders segment-by-segment so nested output paths work reliably.
        string normalizedPath = assetFolderPath.Replace("\\", "/");
        string[] segments = normalizedPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            throw new InvalidOperationException("Asset folder path is empty.");
        }

        string currentPath = segments[0];
        for (int i = 1; i < segments.Length; i++)
        {
            string nextPath = $"{currentPath}/{segments[i]}";
            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(currentPath, segments[i]);
            }

            currentPath = nextPath;
        }
    }

    private static bool IsProjectAssetPath(string assetPath)
    {
        return !string.IsNullOrWhiteSpace(assetPath) && assetPath.StartsWith("Assets/", StringComparison.Ordinal);
    }

    private static string GetAbsoluteFolderPath(string assetPath)
    {
        if (TryConvertToAbsolutePath(assetPath, out string absolutePath))
        {
            return absolutePath;
        }

        return Directory.GetCurrentDirectory();
    }

    private static bool TryConvertToAbsolutePath(string assetPath, out string absolutePath)
    {
        absolutePath = string.Empty;
        if (!IsProjectAssetPath(assetPath))
        {
            return false;
        }

        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrWhiteSpace(projectRoot))
        {
            return false;
        }

        absolutePath = Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        return true;
    }

    private static bool TryConvertToAssetPath(string absolutePath, out string assetPath)
    {
        assetPath = string.Empty;
        if (string.IsNullOrWhiteSpace(absolutePath))
        {
            return false;
        }

        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrWhiteSpace(projectRoot))
        {
            return false;
        }

        string fullProjectRoot = Path.GetFullPath(projectRoot)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string fullFolderPath = Path.GetFullPath(absolutePath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (!fullFolderPath.StartsWith(fullProjectRoot, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string relativePath = fullFolderPath.Substring(fullProjectRoot.Length)
            .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Replace("\\", "/");

        assetPath = relativePath;
        return assetPath.StartsWith("Assets", StringComparison.Ordinal);
    }
}
#endif
