#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public sealed class SpriteVariantAnimationBuilderWindow : EditorWindow
{
    private const string WINDOW_TITLE = "Sprite Variant Animation Builder";
    private const string SETTINGS_ASSET_PATH =
        "Assets/Scripts/Editor/SpriteVariantAnimationBuilder/Sprite Variant Animation Builder Settings.asset";
    private const float LEFT_PANEL_WIDTH = 340f;
    private const float PATH_LABEL_WIDTH = 74f;
    private const float COUNTER_WIDTH = 72f;
    private const float TOP_PANEL_SPACING = 8f;
    private const float ACTION_BUTTON_HEIGHT = 24f;

    [SerializeField] private SpriteVariantAnimationBuilderSettings settings;
    [SerializeField] private bool showSettings = true;
    [SerializeField] private bool buildAnimations = true;
    [SerializeField] private bool buildPrefabs = true;

    private readonly List<BuildFolderPreview> previews = new();
    private readonly Dictionary<string, bool> selectedByFolder = new();
    private Vector2 settingsScroll;
    private Vector2 previewScroll;
    private Editor settingsEditor;
    private string lastReport = "Scan sprite folders to preview the build plan.";

    [MenuItem("Tools/Animation/Sprite Variant Animation Builder")]
    public static void OpenFromMenu()
    {
        SpriteVariantAnimationBuilderWindow window =
            GetWindow<SpriteVariantAnimationBuilderWindow>(WINDOW_TITLE);
        window.minSize = new Vector2(980f, 560f);
        window.Show();
    }

    [MenuItem("Assets/Sprite Variant Animation Builder/Build Selected Folder", true)]
    private static bool ValidateBuildSelectedSpriteFolder()
    {
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        string spriteRootPath = GetConfiguredSpriteRootPath();
        return AssetDatabase.IsValidFolder(path) && IsPathUnder(path, spriteRootPath);
    }

    [MenuItem("Assets/Sprite Variant Animation Builder/Build Selected Folder")]
    private static void BuildSelectedSpriteFolder()
    {
        SpriteVariantAnimationBuilderWindow window =
            GetWindow<SpriteVariantAnimationBuilderWindow>(WINDOW_TITLE);
        window.EnsureSettings();
        window.Scan();

        string selectedPath = NormalizeAssetPath(AssetDatabase.GetAssetPath(Selection.activeObject));
        foreach (BuildFolderPreview preview in window.previews)
        {
            window.selectedByFolder[preview.FolderPath] = preview.FolderPath == selectedPath;
        }

        window.BuildSelected();
        window.Show();
    }

    private void OnEnable()
    {
        EnsureSettings();
        Scan();
    }

    private void OnDisable()
    {
        DestroyImmediate(settingsEditor);
    }

    private void OnGUI()
    {
        EnsureSettings();
        DrawTopPanel();

        EditorGUILayout.BeginHorizontal();
        DrawSettingsPanel();
        DrawPreviewPanel();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawTopPanel()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawSettingsTopBlock();
                GUILayout.Space(TOP_PANEL_SPACING);
                DrawActionTopBlock();
                GUILayout.Space(TOP_PANEL_SPACING);
                DrawBuildScopeTopBlock();
            }
        }
    }

    private void DrawSettingsTopBlock()
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.MinWidth(260f), GUILayout.ExpandWidth(true)))
        {
            EditorGUILayout.LabelField(WINDOW_TITLE, EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            settings = (SpriteVariantAnimationBuilderSettings)EditorGUILayout.ObjectField(
                "Settings",
                settings,
                typeof(SpriteVariantAnimationBuilderSettings),
                false);
            if (EditorGUI.EndChangeCheck())
            {
                RebuildSettingsEditor();
                Scan();
            }
        }
    }

    private void DrawActionTopBlock()
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(176f)))
        {
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Scan", GUILayout.Height(ACTION_BUTTON_HEIGHT)))
                {
                    Scan();
                    GUIUtility.ExitGUI();
                }

                using (new EditorGUI.DisabledScope(previews.Count == 0))
                {
                    if (GUILayout.Button("Build All", GUILayout.Height(ACTION_BUTTON_HEIGHT)))
                    {
                        SelectAll(true);
                        BuildSelected();
                        GUIUtility.ExitGUI();
                    }
                }
            }

            using (new EditorGUI.DisabledScope(previews.Count == 0))
            {
                if (GUILayout.Button("Build Selected", GUILayout.Height(ACTION_BUTTON_HEIGHT)))
                {
                    BuildSelected();
                    GUIUtility.ExitGUI();
                }
            }
        }
    }

    private void DrawBuildScopeTopBlock()
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(160f)))
        {
            EditorGUILayout.LabelField("Build", EditorStyles.boldLabel);
            buildAnimations = EditorGUILayout.ToggleLeft("Animations", buildAnimations);
            buildPrefabs = EditorGUILayout.ToggleLeft("Prefab Variants", buildPrefabs);
        }
    }

    private void DrawSettingsPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(LEFT_PANEL_WIDTH));
        settingsScroll = EditorGUILayout.BeginScrollView(settingsScroll);

        showSettings = EditorGUILayout.Foldout(showSettings, "Settings", true);
        if (showSettings)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (settingsEditor == null && settings != null)
                {
                    RebuildSettingsEditor();
                }

                settingsEditor?.OnInspectorGUI();

                EditorGUILayout.Space(4f);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Resolve Defaults"))
                    {
                        settings.ResolveDefaultReferences();
                        EditorUtility.SetDirty(settings);
                        Scan();
                    }

                    if (GUILayout.Button("Reset"))
                    {
                        if (EditorUtility.DisplayDialog(
                                "Reset Settings",
                                "Reset Sprite Variant Animation Builder settings to defaults?",
                                "Reset",
                                "Cancel"))
                        {
                            settings.ResetToDefaults();
                            EditorUtility.SetDirty(settings);
                            Scan();
                        }
                    }
                }
            }
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Report", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(lastReport, MessageType.Info);

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawPreviewPanel()
    {
        EditorGUILayout.BeginVertical();
        DrawPreviewHeader();

        previewScroll = EditorGUILayout.BeginScrollView(previewScroll);

        if (previews.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "No build folders found. Put one folder per variant under the configured sprite root.",
                MessageType.Warning);
        }
        else
        {
            foreach (BuildFolderPreview preview in previews)
            {
                DrawPreviewRow(preview);
            }
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawPreviewHeader()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            if (GUILayout.Button("All", GUILayout.Width(56f)))
            {
                SelectAll(true);
            }

            if (GUILayout.Button("None", GUILayout.Width(68f)))
            {
                SelectAll(false);
            }

            GUILayout.FlexibleSpace();

            int selectedCount = previews.Count(preview =>
                selectedByFolder.TryGetValue(preview.FolderPath, out bool selected) && selected);
            GUILayout.Label($"Folders {previews.Count} / Selected {selectedCount}", EditorStyles.miniLabel);
        }
    }

    private void DrawPreviewRow(BuildFolderPreview preview)
    {
        bool selected = selectedByFolder.TryGetValue(preview.FolderPath, out bool current) && current;

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                selectedByFolder[preview.FolderPath] =
                    GUILayout.Toggle(selected, GUIContent.none, GUILayout.Width(20f));
                EditorGUILayout.LabelField(preview.VariantName, EditorStyles.boldLabel, GUILayout.MinWidth(120f));
                GUILayout.FlexibleSpace();
                DrawCounter("clips", preview.ValidClipCount);
                DrawCounter("frames", preview.FrameCount);

                if (GUILayout.Button("Ping", GUILayout.Width(58f)))
                {
                    PingPath(preview.FolderPath);
                }
            }

            DrawPathRow("Source", preview.FolderPath);
            DrawPathRow("Anim", preview.AnimationFolderPath);
            DrawPathRow("Controller", preview.ControllerPath);
            DrawPathRow("Prefab", preview.PrefabPath);

            foreach (AnimationAtlasPreview atlas in preview.Atlases)
            {
                DrawAtlasRow(atlas);
            }

            if (preview.Warnings.Count > 0)
            {
                EditorGUILayout.HelpBox(string.Join("\n", preview.Warnings), MessageType.Warning);
            }
        }
    }

    private static void DrawCounter(string label, int value)
    {
        EditorGUILayout.LabelField($"{label} {value}", EditorStyles.miniLabel, GUILayout.Width(COUNTER_WIDTH));
    }

    private static void DrawPathRow(string label, string path)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField(label, EditorStyles.miniLabel, GUILayout.Width(PATH_LABEL_WIDTH));
            EditorGUILayout.SelectableLabel(
                path,
                EditorStyles.miniLabel,
                GUILayout.Height(EditorGUIUtility.singleLineHeight));
        }
    }

    private static void DrawAtlasRow(AnimationAtlasPreview atlas)
    {
        EditorGUILayout.Space(2f);
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Space(12f);
            EditorGUILayout.LabelField(atlas.ClipName, EditorStyles.miniBoldLabel, GUILayout.Width(130f));
            EditorGUILayout.SelectableLabel(
                atlas.GetDisplaySummary(),
                EditorStyles.miniLabel,
                GUILayout.Height(EditorGUIUtility.singleLineHeight));
        }
    }

    private void EnsureSettings()
    {
        if (settings != null)
        {
            return;
        }

        settings = AssetDatabase.LoadAssetAtPath<SpriteVariantAnimationBuilderSettings>(SETTINGS_ASSET_PATH);
        if (settings == null)
        {
            EnsureFolder(Path.GetDirectoryName(SETTINGS_ASSET_PATH)?.Replace('\\', '/'));
            settings = CreateInstance<SpriteVariantAnimationBuilderSettings>();
            settings.ResetToDefaults();
            AssetDatabase.CreateAsset(settings, SETTINGS_ASSET_PATH);
            AssetDatabase.SaveAssets();
        }

        settings.ResolveDefaultReferences();
        RebuildSettingsEditor();
    }

    private void RebuildSettingsEditor()
    {
        DestroyImmediate(settingsEditor);
        settingsEditor = settings != null ? Editor.CreateEditor(settings) : null;
    }

    private void Scan()
    {
        previews.Clear();

        if (settings == null)
        {
            lastReport = "Settings asset is missing.";
            return;
        }

        settings.ResolveDefaultReferences();

        string spriteRoot = settings.SpriteInputRootPath;
        if (!AssetDatabase.IsValidFolder(spriteRoot))
        {
            lastReport = $"Sprite input root does not exist: {spriteRoot}";
            return;
        }

        string[] buildFolders = AssetDatabase.GetSubFolders(spriteRoot);
        foreach (string buildFolder in buildFolders.OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
        {
            BuildFolderPreview preview = CreatePreview(buildFolder);
            previews.Add(preview);

            if (!selectedByFolder.ContainsKey(preview.FolderPath))
            {
                selectedByFolder[preview.FolderPath] = true;
            }
        }

        lastReport = $"Found {previews.Count} build folder(s) under {spriteRoot}.";
        Repaint();
    }

    private BuildFolderPreview CreatePreview(string folderPath)
    {
        string normalizedFolder = NormalizeAssetPath(folderPath);
        string variantName = Path.GetFileName(normalizedFolder);
        string animationFolder = CombineAssetPath(settings.AnimationOutputRootPath, variantName);
        string prefabPath = CombineAssetPath(settings.PrefabOutputRootPath, $"{variantName}.prefab");

        BuildFolderPreview preview = new(
            variantName,
            normalizedFolder,
            animationFolder,
            CombineAssetPath(animationFolder, $"{variantName}.controller"),
            prefabPath);

        foreach (string texturePath in FindTexturePaths(normalizedFolder))
        {
            AnimationAtlasPreview atlas = CreateAtlasPreview(texturePath, animationFolder);
            preview.Atlases.Add(atlas);
        }

        if (preview.Atlases.Count == 0)
        {
            preview.Warnings.Add("No texture atlases found.");
        }

        if (preview.ValidClipCount == 0)
        {
            preview.Warnings.Add("No sprites found in the atlases. Check Sprite Mode and slicing.");
        }

        if (settings.TemplatePrefab == null)
        {
            preview.Warnings.Add("Template prefab is not assigned.");
        }

        return preview;
    }

    private AnimationAtlasPreview CreateAtlasPreview(string texturePath, string animationFolder)
    {
        string clipName = Path.GetFileNameWithoutExtension(texturePath);
        return new AnimationAtlasPreview(
            clipName,
            NormalizeAssetPath(texturePath),
            CombineAssetPath(animationFolder, $"{clipName}.anim"),
            LoadSprites(texturePath));
    }

    private IEnumerable<string> FindTexturePaths(string folder)
    {
        IEnumerable<string> paths = AssetDatabase.FindAssets("t:Texture2D", new[] { folder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(NormalizeAssetPath);

        if (!settings.IncludeNestedTextureFolders)
        {
            paths = paths.Where(path => NormalizeAssetPath(Path.GetDirectoryName(path)) == folder);
        }

        return paths.OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase);
    }

    private List<Sprite> LoadSprites(string texturePath)
    {
        IEnumerable<Sprite> sprites = AssetDatabase.LoadAllAssetsAtPath(texturePath).OfType<Sprite>();

        switch (settings.FrameSortMode)
        {
            case SpriteVariantAnimationBuilderSettings.SpriteFrameSortMode.TexturePositionTopLeft:
                sprites = sprites.OrderByDescending(sprite => sprite.rect.yMax).ThenBy(sprite => sprite.rect.xMin);
                break;
            case SpriteVariantAnimationBuilderSettings.SpriteFrameSortMode.TexturePositionBottomLeft:
                sprites = sprites.OrderBy(sprite => sprite.rect.yMin).ThenBy(sprite => sprite.rect.xMin);
                break;
            default:
                sprites = sprites.OrderBy(sprite => sprite.name, NaturalStringComparer.Instance);
                break;
        }

        return sprites.ToList();
    }

    private void BuildSelected()
    {
        List<BuildFolderPreview> selectedPreviews = previews
            .Where(preview => selectedByFolder.TryGetValue(preview.FolderPath, out bool selected) && selected)
            .ToList();

        if (selectedPreviews.Count == 0)
        {
            lastReport = "No build folders selected.";
            return;
        }

        BuildReport report = new();
        AssetDatabase.Refresh();

        try
        {
            for (int i = 0; i < selectedPreviews.Count; i++)
            {
                BuildFolderPreview preview = selectedPreviews[i];
                EditorUtility.DisplayProgressBar(
                    WINDOW_TITLE,
                    $"Building {preview.VariantName}",
                    (float)i / selectedPreviews.Count);

                BuildFolder(preview, report);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Scan();

        lastReport = report.ToString();
        Debug.Log(lastReport);

        if (settings.SelectGeneratedAssets)
        {
            SelectGeneratedObjects(selectedPreviews);
        }
    }

    private void BuildFolder(BuildFolderPreview preview, BuildReport report)
    {
        if (preview.ValidClipCount == 0)
        {
            report.AddSkipped(preview.VariantName, "No valid sprite atlas.");
            return;
        }

        AnimatorController controller = null;
        if (buildAnimations)
        {
            List<AnimationClip> clips = BuildAnimationClips(preview, report);
            controller = BuildAnimatorController(preview, clips, report);
        }
        else
        {
            controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(preview.ControllerPath);
        }

        if (buildPrefabs)
        {
            BuildPrefabVariant(preview, controller, report);
        }
    }

    private List<AnimationClip> BuildAnimationClips(BuildFolderPreview preview, BuildReport report)
    {
        EnsureFolder(preview.AnimationFolderPath);

        List<AnimationClip> clips = new();
        HashSet<string> generatedClipNames = new(StringComparer.OrdinalIgnoreCase);
        foreach (AnimationAtlasPreview atlas in preview.Atlases)
        {
            if (atlas.Sprites.Count == 0)
            {
                report.AddSkipped($"{preview.VariantName}/{atlas.ClipName}", "Atlas has no sprites.");
                continue;
            }

            AnimationClip clip = CreateOrUpdateAnimationClip(atlas.OutputClipPath, atlas);
            if (clip == null)
            {
                report.AddSkipped(
                    $"{preview.VariantName}/{atlas.ClipName}",
                    "Animation clip exists and overwrite is disabled.");
                continue;
            }

            clips.Add(clip);
            generatedClipNames.Add(atlas.ClipName);
            report.AnimationClipCount++;
        }

        if (settings.DeleteStaleAnimationClips && settings.OverwriteExistingAssets)
        {
            DeleteStaleAnimationClips(preview.AnimationFolderPath, generatedClipNames, report);
        }

        return clips;
    }

    private AnimationClip CreateOrUpdateAnimationClip(string clipPath, AnimationAtlasPreview atlas)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip != null && !settings.OverwriteExistingAssets)
        {
            return null;
        }

        bool isNew = clip == null;
        if (isNew)
        {
            clip = new AnimationClip();
        }

        clip.name = atlas.ClipName;
        clip.frameRate = settings.AnimationFrameRate;
        clip.wrapMode = settings.LoopClipsByDefault ? WrapMode.Loop : WrapMode.Default;

        ClearAnimationClipCurves(clip);

        EditorCurveBinding spriteBinding = new()
        {
            path = settings.SpriteRendererPath,
            type = typeof(SpriteRenderer),
            propertyName = "m_Sprite"
        };

        ObjectReferenceKeyframe[] keyframes = CreateSpriteKeyframes(atlas.Sprites, settings.AnimationFrameRate);
        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyframes);

        AnimationClipSettings clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
        clipSettings.loopTime = settings.LoopClipsByDefault;
        AnimationUtility.SetAnimationClipSettings(clip, clipSettings);

        EditorUtility.SetDirty(clip);

        if (isNew)
        {
            AssetDatabase.CreateAsset(clip, clipPath);
        }

        return clip;
    }

    private static ObjectReferenceKeyframe[] CreateSpriteKeyframes(IReadOnlyList<Sprite> sprites, float frameRate)
    {
        List<ObjectReferenceKeyframe> keyframes = new();
        for (int i = 0; i < sprites.Count; i++)
        {
            keyframes.Add(new ObjectReferenceKeyframe
            {
                time = i / frameRate,
                value = sprites[i]
            });
        }

        if (sprites.Count > 0)
        {
            keyframes.Add(new ObjectReferenceKeyframe
            {
                time = sprites.Count / frameRate,
                value = sprites[sprites.Count - 1]
            });
        }

        return keyframes.ToArray();
    }

    private static void ClearAnimationClipCurves(AnimationClip clip)
    {
        foreach (EditorCurveBinding binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
        {
            AnimationUtility.SetObjectReferenceCurve(clip, binding, null);
        }

        foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
        {
            AnimationUtility.SetEditorCurve(clip, binding, null);
        }
    }

    private AnimatorController BuildAnimatorController(
        BuildFolderPreview preview,
        IReadOnlyList<AnimationClip> clips,
        BuildReport report)
    {
        if (clips.Count == 0)
        {
            return AssetDatabase.LoadAssetAtPath<AnimatorController>(preview.ControllerPath);
        }

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(preview.ControllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(preview.ControllerPath);
            report.ControllerCreateCount++;
        }
        else if (!settings.OverwriteExistingAssets)
        {
            report.AddSkipped(preview.VariantName, "Animator controller exists and overwrite is disabled.");
            return controller;
        }

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        HashSet<string> generatedStateNames = new(clips.Select(clip => clip.name), StringComparer.OrdinalIgnoreCase);

        if (settings.RemoveStaleAnimatorStates)
        {
            foreach (ChildAnimatorState childState in stateMachine.states.ToArray())
            {
                if (!generatedStateNames.Contains(childState.state.name))
                {
                    stateMachine.RemoveState(childState.state);
                }
            }
        }

        foreach (AnimationClip clip in clips)
        {
            AnimatorState state = FindState(stateMachine, clip.name);
            if (state == null)
            {
                state = stateMachine.AddState(clip.name);
            }

            state.motion = clip;
            state.writeDefaultValues = true;
        }

        AnimationClip defaultClip = clips[0];
        AnimatorState defaultState = FindState(stateMachine, defaultClip.name);
        if (defaultState != null)
        {
            stateMachine.defaultState = defaultState;
        }

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private GameObject BuildPrefabVariant(
        BuildFolderPreview preview,
        RuntimeAnimatorController controller,
        BuildReport report)
    {
        if (settings.TemplatePrefab == null)
        {
            report.AddSkipped(preview.VariantName, "Template prefab is missing.");
            return AssetDatabase.LoadAssetAtPath<GameObject>(preview.PrefabPath);
        }

        GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(preview.PrefabPath);
        if (existingPrefab != null && !settings.OverwriteExistingAssets)
        {
            report.AddSkipped(preview.VariantName, "Prefab exists and overwrite is disabled.");
            return existingPrefab;
        }

        EnsureFolder(settings.PrefabOutputRootPath);

        bool loadedExistingPrefab = existingPrefab != null;
        GameObject instance = loadedExistingPrefab
            ? PrefabUtility.LoadPrefabContents(preview.PrefabPath)
            : (GameObject)PrefabUtility.InstantiatePrefab(settings.TemplatePrefab);
        if (instance == null)
        {
            report.AddSkipped(preview.VariantName, "Failed to instantiate template prefab.");
            return existingPrefab;
        }

        try
        {
            instance.name = preview.VariantName;

            if (settings.AssignControllerToPrefab &&
                controller != null &&
                instance.TryGetComponent(out Animator animator))
            {
                animator.runtimeAnimatorController = controller;
            }

            if (settings.AssignPreviewSpriteToPrefab)
            {
                Sprite previewSprite = preview.GetFirstSprite();
                SpriteRenderer spriteRenderer = FindSpriteRenderer(instance);
                if (spriteRenderer != null && previewSprite != null)
                {
                    spriteRenderer.sprite = previewSprite;
                }
            }

            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(instance, preview.PrefabPath, out bool success);
            if (!success || savedPrefab == null)
            {
                report.AddSkipped(preview.VariantName, $"Failed to save prefab: {preview.PrefabPath}");
                return existingPrefab;
            }

            if (loadedExistingPrefab)
            {
                report.PrefabUpdateCount++;
            }
            else
            {
                report.PrefabCreateCount++;
            }

            return savedPrefab;
        }
        finally
        {
            if (loadedExistingPrefab)
            {
                PrefabUtility.UnloadPrefabContents(instance);
            }
            else
            {
                DestroyImmediate(instance);
            }
        }
    }

    private SpriteRenderer FindSpriteRenderer(GameObject root)
    {
        string path = settings.SpriteRendererPath;
        if (!string.IsNullOrWhiteSpace(path))
        {
            Transform child = root.transform.Find(path);
            if (child != null && child.TryGetComponent(out SpriteRenderer childRenderer))
            {
                return childRenderer;
            }
        }

        return root.GetComponentInChildren<SpriteRenderer>();
    }

    private void DeleteStaleAnimationClips(
        string animationFolder,
        HashSet<string> generatedClipNames,
        BuildReport report)
    {
        string[] clipGuids = AssetDatabase.FindAssets("t:AnimationClip", new[] { animationFolder });
        foreach (string guid in clipGuids)
        {
            string clipPath = AssetDatabase.GUIDToAssetPath(guid);
            string clipName = Path.GetFileNameWithoutExtension(clipPath);
            if (generatedClipNames.Contains(clipName))
            {
                continue;
            }

            if (AssetDatabase.DeleteAsset(clipPath))
            {
                report.DeletedStaleClipCount++;
            }
        }
    }

    private void SelectAll(bool selected)
    {
        foreach (BuildFolderPreview preview in previews)
        {
            selectedByFolder[preview.FolderPath] = selected;
        }
    }

    private void SelectGeneratedObjects(IReadOnlyList<BuildFolderPreview> selectedPreviews)
    {
        List<UnityEngine.Object> objects = new();
        foreach (BuildFolderPreview preview in selectedPreviews)
        {
            UnityEngine.Object controller = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(preview.ControllerPath);
            UnityEngine.Object prefab = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(preview.PrefabPath);

            if (controller != null)
            {
                objects.Add(controller);
            }

            if (prefab != null)
            {
                objects.Add(prefab);
            }
        }

        Selection.objects = objects.ToArray();
    }

    private static void PingPath(string assetPath)
    {
        UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
        if (asset != null)
        {
            EditorGUIUtility.PingObject(asset);
        }
    }

    private static string GetConfiguredSpriteRootPath()
    {
        SpriteVariantAnimationBuilderSettings configuredSettings =
            AssetDatabase.LoadAssetAtPath<SpriteVariantAnimationBuilderSettings>(SETTINGS_ASSET_PATH);
        return configuredSettings != null
            ? configuredSettings.SpriteInputRootPath
            : SpriteVariantAnimationBuilderSettings.DEFAULT_SPRITE_INPUT_ROOT;
    }

    private static AnimatorState FindState(AnimatorStateMachine stateMachine, string stateName)
    {
        return stateMachine.states
            .Select(childState => childState.state)
            .FirstOrDefault(state => string.Equals(state.name, stateName, StringComparison.OrdinalIgnoreCase));
    }

    private static void EnsureFolder(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string normalizedPath = NormalizeAssetPath(folderPath);
        string[] parts = normalizedPath.Split('/');
        if (parts.Length == 0 || parts[0] != "Assets")
        {
            throw new ArgumentException($"Unity asset folders must start with Assets: {folderPath}", nameof(folderPath));
        }

        string current = "Assets";
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    private static string CombineAssetPath(string folder, string child)
    {
        return $"{NormalizeAssetPath(folder)}/{child.TrimStart('/', '\\')}";
    }

    private static string NormalizeAssetPath(string path)
    {
        return string.IsNullOrWhiteSpace(path)
            ? string.Empty
            : path.Replace('\\', '/').Trim().TrimEnd('/');
    }

    private static bool IsPathUnder(string path, string root)
    {
        string normalizedPath = NormalizeAssetPath(path);
        string normalizedRoot = NormalizeAssetPath(root);
        return normalizedPath.StartsWith(normalizedRoot + "/", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class BuildFolderPreview
    {
        public BuildFolderPreview(
            string variantName,
            string folderPath,
            string animationFolderPath,
            string controllerPath,
            string prefabPath)
        {
            VariantName = variantName;
            FolderPath = folderPath;
            AnimationFolderPath = animationFolderPath;
            ControllerPath = controllerPath;
            PrefabPath = prefabPath;
        }

        public string VariantName { get; }
        public string FolderPath { get; }
        public string AnimationFolderPath { get; }
        public string ControllerPath { get; }
        public string PrefabPath { get; }
        public List<AnimationAtlasPreview> Atlases { get; } = new();
        public List<string> Warnings { get; } = new();
        public int ValidClipCount => Atlases.Count(atlas => atlas.Sprites.Count > 0);
        public int FrameCount => Atlases.Sum(atlas => atlas.Sprites.Count);

        public Sprite GetFirstSprite()
        {
            return Atlases.SelectMany(atlas => atlas.Sprites).FirstOrDefault();
        }
    }

    private sealed class AnimationAtlasPreview
    {
        public AnimationAtlasPreview(
            string clipName,
            string texturePath,
            string outputClipPath,
            List<Sprite> sprites)
        {
            ClipName = clipName;
            TexturePath = texturePath;
            OutputClipPath = outputClipPath;
            Sprites = sprites;
        }

        public string ClipName { get; }
        public string TexturePath { get; }
        public string OutputClipPath { get; }
        public List<Sprite> Sprites { get; }

        public string GetDisplaySummary()
        {
            return Sprites.Count == 0
                ? $"{TexturePath} | no sprites"
                : $"{TexturePath} | {Sprites.Count} frame(s)";
        }
    }

    private sealed class BuildReport
    {
        private readonly List<string> skipped = new();

        public int AnimationClipCount { get; set; }
        public int ControllerCreateCount { get; set; }
        public int PrefabCreateCount { get; set; }
        public int PrefabUpdateCount { get; set; }
        public int DeletedStaleClipCount { get; set; }

        public void AddSkipped(string target, string reason)
        {
            skipped.Add($"{target}: {reason}");
        }

        public override string ToString()
        {
            List<string> lines = new()
            {
                "Sprite Variant Animation Builder completed.",
                $"Animation clips: {AnimationClipCount}",
                $"Animator controllers created: {ControllerCreateCount}",
                $"Prefab variants created: {PrefabCreateCount}",
                $"Prefabs updated: {PrefabUpdateCount}",
                $"Stale clips deleted: {DeletedStaleClipCount}"
            };

            if (skipped.Count > 0)
            {
                lines.Add("Skipped:");
                lines.AddRange(skipped.Select(item => $"  {item}"));
            }

            return string.Join("\n", lines);
        }
    }

    private sealed class NaturalStringComparer : IComparer<string>
    {
        public static readonly NaturalStringComparer Instance = new();

        public int Compare(string x, string y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (x == null)
            {
                return -1;
            }

            if (y == null)
            {
                return 1;
            }

            int xIndex = 0;
            int yIndex = 0;
            while (xIndex < x.Length && yIndex < y.Length)
            {
                char xChar = x[xIndex];
                char yChar = y[yIndex];

                if (char.IsDigit(xChar) && char.IsDigit(yChar))
                {
                    long xNumber = ReadNumber(x, ref xIndex);
                    long yNumber = ReadNumber(y, ref yIndex);
                    int numberCompare = xNumber.CompareTo(yNumber);
                    if (numberCompare != 0)
                    {
                        return numberCompare;
                    }

                    continue;
                }

                int charCompare = char.ToUpperInvariant(xChar).CompareTo(char.ToUpperInvariant(yChar));
                if (charCompare != 0)
                {
                    return charCompare;
                }

                xIndex++;
                yIndex++;
            }

            return x.Length.CompareTo(y.Length);
        }

        private static long ReadNumber(string value, ref int index)
        {
            long result = 0;
            while (index < value.Length && char.IsDigit(value[index]))
            {
                result = result * 10 + value[index] - '0';
                index++;
            }

            return result;
        }
    }
}
#endif
