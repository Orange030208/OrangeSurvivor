#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public sealed class Character2AnimatorWindow : EditorWindow
{
    private const string DefaultSourceFolder = "Assets/GameContent/Characters/Sprites";
    private const string DefaultOutputFolder = "Assets/GameContent/Characters/Animations";
    private const float DefaultSampleRate = 12f;
    private const string SpritePropertyName = "m_Sprite";
    private const string IdleStateName = "Idle";
    private const string RunStateName = "Run";
    private const string DieStateName = "Die";
    private const string IsMovingParameterName = "IsMoving";
    private const string DieParameterName = "Die";

    private static readonly string[] RequiredAnimationNames = { DieStateName, IdleStateName, RunStateName };

    private string sourceFolder = DefaultSourceFolder;
    private string outputFolder = DefaultOutputFolder;
    private float sampleRate = DefaultSampleRate;
    private Vector2 scrollPosition;

    [MenuItem("Tools/Characters/Character2Animator")]
    private static void OpenWindow()
    {
        Character2AnimatorWindow window = GetWindow<Character2AnimatorWindow>();
        window.titleContent = new GUIContent("Character2Animator");
        window.minSize = new Vector2(560f, 320f);
        window.Show();
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Character To Animator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Each character folder must contain three multi-sprite textures ending with Die, Idle, and Run. The tool generates three animation clips and one animator controller per character.",
            MessageType.Info);

        DrawFolderField("Source Folder", ref sourceFolder, DefaultSourceFolder);
        DrawFolderField("Output Folder", ref outputFolder, DefaultOutputFolder);

        sampleRate = EditorGUILayout.FloatField("Sample Rate", sampleRate);
        sampleRate = Mathf.Max(1f, sampleRate);

        EditorGUILayout.Space(12f);
        using (new EditorGUI.DisabledScope(!CanGenerate()))
        {
            if (GUILayout.Button("Generate Character Animators", GUILayout.Height(32f)))
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
                Debug.LogWarning($"[Character2Animator] Folder must be inside project: {selectedFolder}");
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
            EditorUtility.DisplayDialog("Character2Animator", $"Source folder not found:\n{sourceFolder}", "OK");
            return;
        }

        EnsureFolder(outputFolder);

        List<string> characterFolders = GetCharacterFolders(sourceFolder);
        if (characterFolders.Count == 0)
        {
            EditorUtility.DisplayDialog("Character2Animator", $"No character folders found in:\n{sourceFolder}", "OK");
            return;
        }

        int generatedCount = 0;
        int skippedCount = 0;
        try
        {
            for (int i = 0; i < characterFolders.Count; i++)
            {
                string characterFolder = characterFolders[i];
                EditorUtility.DisplayProgressBar(
                    "Character2Animator",
                    $"Generating {Path.GetFileName(characterFolder)} ({i + 1}/{characterFolders.Count})",
                    (float)(i + 1) / characterFolders.Count);

                if (TryGenerateForCharacterFolder(characterFolder))
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
            "Character2Animator",
            $"Finished.\nGenerated: {generatedCount}\nSkipped: {skippedCount}",
            "OK");
    }

    private bool TryGenerateForCharacterFolder(string characterFolder)
    {
        Dictionary<string, string> animationTexturePaths = FindRequiredTextures(characterFolder);
        if (animationTexturePaths == null)
        {
            return false;
        }

        string characterName = Path.GetFileName(characterFolder);
        string characterOutputFolder = $"{outputFolder}/{characterName}";
        EnsureFolder(characterOutputFolder);

        Dictionary<string, AnimationClip> clipsByState = new Dictionary<string, AnimationClip>(StringComparer.Ordinal);
        foreach (string animationName in RequiredAnimationNames)
        {
            string texturePath = animationTexturePaths[animationName];
            EnsureMultipleSpriteImport(texturePath);

            List<Sprite> sprites = LoadSprites(texturePath);
            if (sprites.Count == 0)
            {
                Debug.LogWarning($"[Character2Animator] No sub-sprites found in texture: {texturePath}");
                return false;
            }

            string clipPath = $"{characterOutputFolder}/{characterName}_{animationName}.anim";
            AnimationClip clip = CreateOrUpdateAnimationClip(clipPath, sprites, animationName != DieStateName);
            clipsByState[animationName] = clip;
        }

        string controllerPath = $"{characterOutputFolder}/{characterName}.controller";
        CreateOrUpdateAnimatorController(controllerPath, clipsByState);

        Debug.Log($"[Character2Animator] Generated animator assets for {characterName} at {characterOutputFolder}");
        return true;
    }

    private static List<string> GetCharacterFolders(string rootFolder)
    {
        string absoluteRootFolder = GetAbsoluteFolderPath(rootFolder);
        if (string.IsNullOrWhiteSpace(absoluteRootFolder) || !Directory.Exists(absoluteRootFolder))
        {
            return new List<string>();
        }

        List<string> assetFolders = new List<string>();
        foreach (string directory in Directory.GetDirectories(absoluteRootFolder, "*", SearchOption.AllDirectories))
        {
            if (!Directory.GetFiles(directory).Any(IsSupportedImageAsset))
            {
                continue;
            }

            if (TryConvertToAssetPath(directory, out string assetPath))
            {
                assetFolders.Add(assetPath);
            }
        }

        assetFolders.Sort(StringComparer.OrdinalIgnoreCase);
        return assetFolders;
    }

    private static Dictionary<string, string> FindRequiredTextures(string characterFolder)
    {
        string absoluteFolder = GetAbsoluteFolderPath(characterFolder);
        if (string.IsNullOrWhiteSpace(absoluteFolder) || !Directory.Exists(absoluteFolder))
        {
            Debug.LogWarning($"[Character2Animator] Character folder not found: {characterFolder}");
            return null;
        }

        string[] files = Directory.GetFiles(absoluteFolder)
            .Where(file => IsSupportedImageAsset(file))
            .ToArray();

        Dictionary<string, string> texturePaths = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string animationName in RequiredAnimationNames)
        {
            string matchedFile = files.FirstOrDefault(file =>
                Path.GetFileNameWithoutExtension(file).EndsWith(animationName, StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(matchedFile) || !TryConvertToAssetPath(matchedFile, out string assetPath))
            {
                Debug.LogWarning($"[Character2Animator] Missing {animationName} texture in folder: {characterFolder}");
                return null;
            }

            texturePaths[animationName] = assetPath;
        }

        return texturePaths;
    }

    private static void EnsureMultipleSpriteImport(string texturePath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer == null)
        {
            throw new InvalidOperationException($"[Character2Animator] TextureImporter not found for path: {texturePath}");
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

    private static List<Sprite> LoadSprites(string texturePath)
    {
        return AssetDatabase.LoadAllAssetRepresentationsAtPath(texturePath)
            .OfType<Sprite>()
            .OrderBy(sprite => sprite.name, Comparer<string>.Create(EditorUtility.NaturalCompare))
            .ToList();
    }

    private AnimationClip CreateOrUpdateAnimationClip(string clipPath, List<Sprite> sprites, bool loopTime)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, clipPath);
        }

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
        SetLoopTime(clip, loopTime);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static void CreateOrUpdateAnimatorController(string controllerPath, IReadOnlyDictionary<string, AnimationClip> clipsByState)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        }

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

        EnsureParameter(controller, IsMovingParameterName, AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, DieParameterName, AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine stateMachine = layer.stateMachine;
        RemoveUnexpectedStates(stateMachine);
        AnimatorState idleState = CreateOrUpdateState(stateMachine, IdleStateName, clipsByState[IdleStateName]);
        AnimatorState runState = CreateOrUpdateState(stateMachine, RunStateName, clipsByState[RunStateName]);
        AnimatorState dieState = CreateOrUpdateState(stateMachine, DieStateName, clipsByState[DieStateName]);

        stateMachine.defaultState = idleState;
        EnsureIdleToRunTransition(idleState, runState);
        EnsureRunToIdleTransition(runState, idleState);
        EnsureDieTransition(idleState, dieState);
        EnsureDieTransition(runState, dieState);
        EditorUtility.SetDirty(controller);
    }

    private static AnimatorState CreateOrUpdateState(AnimatorStateMachine stateMachine, string stateName, Motion motion)
    {
        AnimatorState state = stateMachine.states
            .Select(child => child.state)
            .FirstOrDefault(existingState => existingState != null && existingState.name == stateName);

        if (state == null)
        {
            state = stateMachine.AddState(stateName);
        }

        state.motion = motion;
        state.speed = 1f;
        return state;
    }

    private static void EnsureParameter(AnimatorController controller, string parameterName, AnimatorControllerParameterType parameterType)
    {
        AnimatorControllerParameter existingParameter = controller.parameters
            .FirstOrDefault(parameter => parameter != null && parameter.name == parameterName);
        if (existingParameter != null)
        {
            return;
        }

        controller.AddParameter(parameterName, parameterType);
    }

    private static void RemoveUnexpectedStates(AnimatorStateMachine stateMachine)
    {
        ChildAnimatorState[] existingStates = stateMachine.states;
        foreach (ChildAnimatorState childState in existingStates)
        {
            AnimatorState state = childState.state;
            if (state == null)
            {
                continue;
            }

            if (state.name == IdleStateName || state.name == RunStateName || state.name == DieStateName)
            {
                continue;
            }

            stateMachine.RemoveState(state);
        }
    }

    private static void EnsureIdleToRunTransition(AnimatorState idleState, AnimatorState runState)
    {
        AnimatorStateTransition transition = FindTransition(idleState, runState)
            ?? idleState.AddTransition(runState);

        ConfigureConditionalTransition(
            transition,
            hasExitTime: false,
            duration: 0f,
            AnimatorConditionMode.If,
            0f,
            IsMovingParameterName);
    }

    private static void EnsureRunToIdleTransition(AnimatorState runState, AnimatorState idleState)
    {
        AnimatorStateTransition transition = FindTransition(runState, idleState)
            ?? runState.AddTransition(idleState);

        ConfigureConditionalTransition(
            transition,
            hasExitTime: false,
            duration: 0f,
            AnimatorConditionMode.IfNot,
            0f,
            IsMovingParameterName);
    }

    private static void EnsureDieTransition(AnimatorState fromState, AnimatorState dieState)
    {
        AnimatorStateTransition transition = FindTransition(fromState, dieState)
            ?? fromState.AddTransition(dieState);

        ConfigureConditionalTransition(
            transition,
            hasExitTime: false,
            duration: 0f,
            AnimatorConditionMode.If,
            0f,
            DieParameterName);
    }

    private static AnimatorStateTransition FindTransition(AnimatorState fromState, AnimatorState toState)
    {
        return fromState.transitions.FirstOrDefault(transition => transition != null && transition.destinationState == toState);
    }

    private static void ConfigureConditionalTransition(
        AnimatorStateTransition transition,
        bool hasExitTime,
        float duration,
        AnimatorConditionMode conditionMode,
        float threshold,
        string parameterName)
    {
        transition.hasExitTime = hasExitTime;
        transition.hasFixedDuration = true;
        transition.duration = duration;
        transition.exitTime = 0f;
        transition.canTransitionToSelf = false;
        transition.interruptionSource = TransitionInterruptionSource.None;
        transition.orderedInterruption = true;

        foreach (AnimatorCondition condition in transition.conditions.ToArray())
        {
            transition.RemoveCondition(condition);
        }

        transition.AddCondition(conditionMode, threshold, parameterName);
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

    private static bool IsSupportedImageAsset(string path)
    {
        string extension = Path.GetExtension(path);
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
        string fullPath = Path.GetFullPath(absolutePath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (!fullPath.StartsWith(fullProjectRoot, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string relativePath = fullPath.Substring(fullProjectRoot.Length)
            .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Replace("\\", "/");

        assetPath = relativePath;
        return assetPath.StartsWith("Assets", StringComparison.Ordinal);
    }
}
#endif
