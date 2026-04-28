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
    private const string WINDOW_TITLE = "图片动画变体构建器";
    private const string SETTINGS_ASSET_PATH =
        "Assets/Scripts/Editor/SpriteVariantAnimationBuilder/图片动画变体构建器设置.asset";
    private const string LEGACY_SETTINGS_ASSET_PATH =
        "Assets/Scripts/Editor/SpriteVariantAnimationBuilder/Sprite Variant Animation Builder Settings.asset";
    private const float LEFT_PANEL_WIDTH = 360f;
    private const float PATH_LABEL_WIDTH = 86f;
    private const float COUNTER_WIDTH = 76f;
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
    private string lastReport = "点击扫描，预览输入根目录下的构建分组。";

    [MenuItem("Tools/Animation/图片动画变体构建器")]
    public static void OpenFromMenu()
    {
        SpriteVariantAnimationBuilderWindow window =
            GetWindow<SpriteVariantAnimationBuilderWindow>(WINDOW_TITLE);
        window.minSize = new Vector2(1040f, 600f);
        window.Show();
    }

    [MenuItem("Assets/图片动画变体构建器/构建选中文件夹", true)]
    private static bool ValidateBuildSelectedSpriteFolder()
    {
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        string spriteRootPath = GetConfiguredSpriteRootPath();
        return AssetDatabase.IsValidFolder(path) && IsPathUnder(path, spriteRootPath);
    }

    [MenuItem("Assets/图片动画变体构建器/构建选中文件夹")]
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
        using (new EditorGUILayout.VerticalScope(GUILayout.MinWidth(360f), GUILayout.ExpandWidth(true)))
        {
            EditorGUILayout.LabelField(WINDOW_TITLE, EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            settings = (SpriteVariantAnimationBuilderSettings)EditorGUILayout.ObjectField(
                "配置",
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
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(210f)))
        {
            EditorGUILayout.LabelField("操作", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("扫描", GUILayout.Height(ACTION_BUTTON_HEIGHT)))
                {
                    Scan();
                    GUIUtility.ExitGUI();
                }

                using (new EditorGUI.DisabledScope(previews.Count == 0))
                {
                    if (GUILayout.Button("构建全部", GUILayout.Height(ACTION_BUTTON_HEIGHT)))
                    {
                        SelectAll(true);
                        BuildSelected();
                        GUIUtility.ExitGUI();
                    }
                }
            }

            using (new EditorGUI.DisabledScope(previews.Count == 0))
            {
                if (GUILayout.Button("构建选中", GUILayout.Height(ACTION_BUTTON_HEIGHT)))
                {
                    BuildSelected();
                    GUIUtility.ExitGUI();
                }
            }
        }
    }

    private void DrawBuildScopeTopBlock()
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(180f)))
        {
            EditorGUILayout.LabelField("构建内容", EditorStyles.boldLabel);
            buildAnimations = EditorGUILayout.ToggleLeft("动画与控制器", buildAnimations);
            buildPrefabs = EditorGUILayout.ToggleLeft("预制体变体", buildPrefabs);
        }
    }

    private void DrawSettingsPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(LEFT_PANEL_WIDTH));
        settingsScroll = EditorGUILayout.BeginScrollView(settingsScroll);

        showSettings = EditorGUILayout.Foldout(showSettings, "配置", true);
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
                    if (GUILayout.Button("补全默认目录"))
                    {
                        settings.ResolveDefaultReferences();
                        EditorUtility.SetDirty(settings);
                        Scan();
                    }

                    if (GUILayout.Button("重置"))
                    {
                        if (EditorUtility.DisplayDialog(
                                "重置配置",
                                "确定要将图片动画变体构建器配置恢复为默认值吗？",
                                "重置",
                                "取消"))
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
        EditorGUILayout.LabelField("报告", EditorStyles.boldLabel);
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
                "没有找到可构建的文件夹。请在输入根目录下按“一个子文件夹一个变体”的方式放置图片。",
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
            if (GUILayout.Button("全选", GUILayout.Width(64f)))
            {
                SelectAll(true);
            }

            if (GUILayout.Button("全不选", GUILayout.Width(76f)))
            {
                SelectAll(false);
            }

            GUILayout.FlexibleSpace();

            int selectedCount = previews.Count(preview =>
                selectedByFolder.TryGetValue(preview.FolderPath, out bool selected) && selected);
            GUILayout.Label($"分组 {previews.Count} / 已选 {selectedCount}", EditorStyles.miniLabel);
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
                DrawCounter("动画", preview.ValidClipCount);
                DrawCounter("帧", preview.FrameCount);

                if (GUILayout.Button("定位", GUILayout.Width(58f)))
                {
                    PingPath(preview.FolderPath);
                }
            }

            DrawPathRow("来源", preview.FolderPath);
            DrawPathRow("动画目录", preview.AnimationFolderPath);
            DrawPathRow("控制器", preview.ControllerPath);
            DrawPathRow("预制体", preview.PrefabPath);

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
            settings = LoadOrMoveLegacySettings();
        }

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

    private static SpriteVariantAnimationBuilderSettings LoadOrMoveLegacySettings()
    {
        SpriteVariantAnimationBuilderSettings legacySettings =
            AssetDatabase.LoadAssetAtPath<SpriteVariantAnimationBuilderSettings>(LEGACY_SETTINGS_ASSET_PATH);
        if (legacySettings == null)
        {
            return null;
        }

        string moveError = AssetDatabase.MoveAsset(LEGACY_SETTINGS_ASSET_PATH, SETTINGS_ASSET_PATH);
        if (!string.IsNullOrEmpty(moveError))
        {
            Debug.LogWarning($"迁移图片动画变体构建器配置失败，将继续使用旧配置：{moveError}");
            return legacySettings;
        }

        AssetDatabase.SaveAssets();
        return AssetDatabase.LoadAssetAtPath<SpriteVariantAnimationBuilderSettings>(SETTINGS_ASSET_PATH);
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
            lastReport = "配置资产不存在。";
            return;
        }

        settings.ResolveDefaultReferences();

        string inputRoot = settings.InputRootPath;
        if (!AssetDatabase.IsValidFolder(inputRoot))
        {
            lastReport = $"输入根目录不存在：{inputRoot}";
            return;
        }

        string[] buildFolders = AssetDatabase.GetSubFolders(inputRoot);
        foreach (string buildFolder in buildFolders.OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
        {
            BuildFolderPreview preview = CreatePreview(buildFolder);
            previews.Add(preview);

            if (!selectedByFolder.ContainsKey(preview.FolderPath))
            {
                selectedByFolder[preview.FolderPath] = true;
            }
        }

        lastReport = $"在 {inputRoot} 下找到 {previews.Count} 个构建分组。";
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
            preview.Warnings.Add("没有找到图片。");
        }

        if (preview.ValidClipCount == 0)
        {
            preview.Warnings.Add("图片中没有可用 Sprite，请检查 Sprite Mode 和切片设置。");
        }

        if (settings.TemplatePrefab == null)
        {
            preview.Warnings.Add("未设置模板预制体。");
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
            lastReport = "没有选中任何构建分组。";
            return;
        }

        if (!ConfirmOverwriteExistingAssets(selectedPreviews))
        {
            lastReport = "已取消构建，未覆盖已有资源。";
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
                    $"正在构建 {preview.VariantName}",
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

    private bool ConfirmOverwriteExistingAssets(IReadOnlyList<BuildFolderPreview> selectedPreviews)
    {
        if (!settings.OverwriteExistingAssets)
        {
            return true;
        }

        List<string> existingAssetPaths = GetExistingBuildAssetPaths(selectedPreviews)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (existingAssetPaths.Count == 0)
        {
            return true;
        }

        const int MAX_DISPLAY_COUNT = 12;
        string displayedPaths = string.Join(
            "\n",
            existingAssetPaths.Take(MAX_DISPLAY_COUNT).Select(path => $"- {path}"));
        int hiddenCount = existingAssetPaths.Count - MAX_DISPLAY_COUNT;
        string hiddenSummary = hiddenCount > 0
            ? $"\n...还有 {hiddenCount} 个资源"
            : string.Empty;

        return EditorUtility.DisplayDialog(
            "确认覆盖已有资源",
            $"检测到本次构建目标中已有 {existingAssetPaths.Count} 个资源存在：\n\n" +
            $"{displayedPaths}{hiddenSummary}\n\n" +
            "继续构建会覆盖或更新这些资源。",
            "继续覆盖",
            "取消构建");
    }

    private IEnumerable<string> GetExistingBuildAssetPaths(IEnumerable<BuildFolderPreview> selectedPreviews)
    {
        foreach (BuildFolderPreview preview in selectedPreviews)
        {
            if (buildAnimations)
            {
                foreach (AnimationAtlasPreview atlas in preview.Atlases)
                {
                    if (AssetDatabase.LoadAssetAtPath<AnimationClip>(atlas.OutputClipPath) != null)
                    {
                        yield return atlas.OutputClipPath;
                    }
                }

                if (AssetDatabase.LoadAssetAtPath<AnimatorController>(preview.ControllerPath) != null)
                {
                    yield return preview.ControllerPath;
                }
            }

            if (buildPrefabs && AssetDatabase.LoadAssetAtPath<GameObject>(preview.PrefabPath) != null)
            {
                yield return preview.PrefabPath;
            }
        }
    }

    private void BuildFolder(BuildFolderPreview preview, BuildReport report)
    {
        if (preview.ValidClipCount == 0)
        {
            report.AddSkipped(preview.VariantName, "没有有效的 Sprite 图集。");
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
                report.AddSkipped($"{preview.VariantName}/{atlas.ClipName}", "图集中没有 Sprite。");
                continue;
            }

            AnimationClip clip = CreateOrUpdateAnimationClip(atlas.OutputClipPath, atlas);
            if (clip == null)
            {
                report.AddSkipped(
                    $"{preview.VariantName}/{atlas.ClipName}",
                    "动画片段已存在，且未开启覆盖。");
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
        bool shouldLoop = settings.ShouldLoopClip(atlas.ClipName);
        clip.wrapMode = shouldLoop ? WrapMode.Loop : WrapMode.Default;

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
        clipSettings.loopTime = shouldLoop;
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
            report.AddSkipped(preview.VariantName, "动画控制器已存在，且未开启覆盖。");
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
            report.AddSkipped(preview.VariantName, "缺少模板预制体。");
            return AssetDatabase.LoadAssetAtPath<GameObject>(preview.PrefabPath);
        }

        GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(preview.PrefabPath);
        if (existingPrefab != null && !settings.OverwriteExistingAssets)
        {
            report.AddSkipped(preview.VariantName, "预制体已存在，且未开启覆盖。");
            return existingPrefab;
        }

        EnsureFolder(settings.PrefabOutputRootPath);

        bool loadedExistingPrefab = existingPrefab != null;
        GameObject instance = loadedExistingPrefab
            ? PrefabUtility.LoadPrefabContents(preview.PrefabPath)
            : (GameObject)PrefabUtility.InstantiatePrefab(settings.TemplatePrefab);
        if (instance == null)
        {
            report.AddSkipped(preview.VariantName, "实例化模板预制体失败。");
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
                report.AddSkipped(preview.VariantName, $"保存预制体失败：{preview.PrefabPath}");
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
        if (configuredSettings == null)
        {
            configuredSettings =
                AssetDatabase.LoadAssetAtPath<SpriteVariantAnimationBuilderSettings>(LEGACY_SETTINGS_ASSET_PATH);
        }

        return configuredSettings != null
            ? configuredSettings.InputRootPath
            : SpriteVariantAnimationBuilderSettings.DEFAULT_INPUT_ROOT;
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
            throw new ArgumentException($"Unity 资产目录必须以 Assets 开头：{folderPath}", nameof(folderPath));
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
                ? $"{TexturePath} | 无 Sprite"
                : $"{TexturePath} | {Sprites.Count} 帧";
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
                "图片动画变体构建完成。",
                $"动画片段：{AnimationClipCount}",
                $"新建动画控制器：{ControllerCreateCount}",
                $"新建预制体变体：{PrefabCreateCount}",
                $"更新预制体：{PrefabUpdateCount}",
                $"删除过期动画片段：{DeletedStaleClipCount}"
            };

            if (skipped.Count > 0)
            {
                lines.Add("跳过：");
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
