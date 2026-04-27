#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public sealed class AttackSequenceStudioWindow : EditorWindow
{
    private const float LEFT_PANEL_WIDTH = 270f;
    private const float PREVIEW_PANEL_WIDTH = 440f;
    private const float MOTION_LIST_MIN_WIDTH = 250f;
    private const float MOTION_LIST_MAX_WIDTH = 360f;
    private const float PANEL_PADDING = 8f;
    private const float MIN_PREVIEW_SCALE = 35f;
    private const float MAX_PREVIEW_SCALE = 120f;
    private const float DEFAULT_PREVIEW_RANGE = 3f;
    private const float DEFAULT_TARGET_DISTANCE = 2f;

    [SerializeField] private AttackSequenceDefinitionSO sequence;
    [SerializeField] private WeaponDataSO weaponData;
    [SerializeField] private WeaponAnimationSequencePresetId selectedPreset;
    [SerializeField] private int editTab;
    [SerializeField] private float previewNormalizedTime;
    [SerializeField] private bool previewPlaying;
    [SerializeField] private bool loopPreview = true;
    [SerializeField] private bool showMotionPath = true;
    [SerializeField] private bool showHitBox = true;
    [SerializeField] private bool showCurvePanel = true;
    [SerializeField] private bool showWeaponSprite = true;
    [SerializeField] private bool showVisualForwardGuide = true;
    [SerializeField] private float previewSpeed = 1f;
    [SerializeField] private float previewPixelsPerUnit = 62f;
    [SerializeField] private float previewAttackRange = DEFAULT_PREVIEW_RANGE;
    [SerializeField] private float previewTargetDistance = DEFAULT_TARGET_DISTANCE;
    [SerializeField] private PreviewCurveChannel curveChannel = PreviewCurveChannel.PositionY;

    private SerializedObject sequenceObject;
    private SerializedObject weaponObject;
    private ReorderableList motionList;
    private ReorderableList eventList;
    private Vector2 leftScroll;
    private Vector2 centerScroll;
    private int selectedMotionIndex = -1;
    private int selectedEventIndex = -1;
    private double lastEditorTime;
    private TimelineDragMode timelineDragMode;
    private int timelineDragIndex = -1;
    private int previewDragMotionIndex = -1;
    private Vector2 previewDragStartWorld;
    private Vector2 previewDragOriginalPosition;
    private int curveDragMotionIndex = -1;
    private float curveDragMinValue;
    private float curveDragMaxValue;

    private static readonly string[] MotionPositionModeLabels = { "固定", "跟随目标动态" };
    private static readonly string[] DynamicStrategyLabels = { "无", "朝目标并限制半径" };
    private static readonly string[] PresetLabels = { "重击挥砍", "蓄力大弧横扫", "半月横扫", "弧形横扫", "步枪后坐" };
    private static readonly string[] MotionEaseLabels =
    {
        "线性", "正弦缓入", "正弦缓出", "正弦缓入缓出",
        "二次缓入", "二次缓出", "二次缓入缓出",
        "三次缓入", "三次缓出", "三次缓入缓出",
        "指数缓入", "指数缓出", "指数缓入缓出",
        "Back 弹出", "Elastic 弹性", "自定义曲线"
    };
    private static readonly string[] EventTypeLabels = { "开启命中窗", "关闭命中窗", "生成投射物", "播放音效", "播放特效" };
    private static readonly string[] CurveChannelLabels = { "X 位移", "Y 位移", "Z 旋转" };

    [MenuItem("Tools/Weapons/攻击序列工作台")]
    public static void OpenFromMenu()
    {
        AttackSequenceStudioWindow window = GetWindow<AttackSequenceStudioWindow>("攻击序列工作台");
        window.TryLoadTargetsFromSelection(false);
        window.Show();
    }

    [MenuItem("Assets/在攻击序列工作台中打开", true)]
    private static bool ValidateOpenFromAssetMenu()
    {
        Object activeObject = Selection.activeObject;
        return activeObject is AttackSequenceDefinitionSO || activeObject is WeaponDataSO;
    }

    [MenuItem("Assets/在攻击序列工作台中打开")]
    private static void OpenFromAssetMenu()
    {
        AttackSequenceStudioWindow window = GetWindow<AttackSequenceStudioWindow>("攻击序列工作台");
        window.TryLoadTargetsFromSelection(true);
        window.Show();
    }

    public static void Open(AttackSequenceDefinitionSO targetSequence, WeaponDataSO targetWeapon = null)
    {
        AttackSequenceStudioWindow window = GetWindow<AttackSequenceStudioWindow>("攻击序列工作台");
        window.AssignTargets(targetSequence, targetWeapon);
        window.Show();
    }

    public static void Open(WeaponDataSO targetWeapon)
    {
        AttackSequenceDefinitionSO targetSequence = targetWeapon != null ? targetWeapon.AttackSequence : null;
        Open(targetSequence, targetWeapon);
    }

    private void OnEnable()
    {
        lastEditorTime = EditorApplication.timeSinceStartup;
        EditorApplication.update += OnEditorUpdate;
        RebuildSerializedObjects();
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
    }

    private void OnSelectionChange()
    {
        Repaint();
    }

    private void OnGUI()
    {
        DrawTopBar();
        EnsureSerializedObjects();

        if (sequenceObject != null)
        {
            sequenceObject.Update();
        }

        if (weaponObject != null)
        {
            weaponObject.Update();
        }

        EnsureListsReady();

        if (sequence == null && weaponData == null)
        {
            DrawEmptyState();
            return;
        }

        EditorGUILayout.BeginHorizontal();
        DrawLeftPanel();
        DrawCenterPanel();
        ApplyModifiedProperties();
        DrawPreviewPanel();
        EditorGUILayout.EndHorizontal();
    }

    private void OnEditorUpdate()
    {
        double time = EditorApplication.timeSinceStartup;
        float deltaTime = Mathf.Max(0f, (float)(time - lastEditorTime));
        lastEditorTime = time;

        if (!previewPlaying || sequence == null)
        {
            return;
        }

        float duration = Mathf.Max(0.01f, sequence.Duration);
        previewNormalizedTime += deltaTime * Mathf.Max(0.01f, previewSpeed) / duration;
        if (previewNormalizedTime > 1f)
        {
            if (loopPreview)
            {
                previewNormalizedTime %= 1f;
            }
            else
            {
                previewNormalizedTime = 1f;
                previewPlaying = false;
            }
        }

        Repaint();
    }

    private void DrawTopBar()
    {
        EditorGUILayout.BeginVertical(EditorStyles.toolbar);
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField("攻击序列工作台", EditorStyles.boldLabel, GUILayout.Width(180f));

        EditorGUI.BeginChangeCheck();
        AttackSequenceDefinitionSO nextSequence = (AttackSequenceDefinitionSO)EditorGUILayout.ObjectField(
            sequence,
            typeof(AttackSequenceDefinitionSO),
            false,
            GUILayout.MinWidth(180f));
        WeaponDataSO nextWeapon = (WeaponDataSO)EditorGUILayout.ObjectField(
            weaponData,
            typeof(WeaponDataSO),
            false,
            GUILayout.MinWidth(180f));
        if (EditorGUI.EndChangeCheck())
        {
            AssignTargets(nextSequence, nextWeapon);
        }

        if (GUILayout.Button("读取当前选择", EditorStyles.toolbarButton, GUILayout.Width(105f)))
        {
            TryLoadTargetsFromSelection(true);
            GUIUtility.ExitGUI();
        }

        using (new EditorGUI.DisabledScope(weaponData == null || weaponData.AttackSequence == null))
        {
            if (GUILayout.Button("使用武器已挂序列", EditorStyles.toolbarButton, GUILayout.Width(145f)))
            {
                AssignTargets(weaponData.AttackSequence, weaponData);
                GUIUtility.ExitGUI();
            }
        }

        using (new EditorGUI.DisabledScope(sequence == null || weaponData == null))
        {
            if (GUILayout.Button("指定给武器", EditorStyles.toolbarButton, GUILayout.Width(120f)))
            {
                AssignSequenceToWeapon();
                GUIUtility.ExitGUI();
            }
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void DrawEmptyState()
    {
        GUILayout.FlexibleSpace();
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(420f));
        EditorGUILayout.LabelField("当前没有加载资源", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("请选择一个 AttackSequenceDefinitionSO 或 WeaponDataSO，然后在这里载入。", MessageType.Info);

        if (GUILayout.Button("载入当前选择"))
        {
            TryLoadTargetsFromSelection(true);
            GUIUtility.ExitGUI();
        }

        if (GUILayout.Button("创建新攻击序列"))
        {
            CreateNewSequenceAsset();
            GUIUtility.ExitGUI();
        }

        EditorGUILayout.EndVertical();
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        GUILayout.FlexibleSpace();
    }

    private void DrawLeftPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(LEFT_PANEL_WIDTH), GUILayout.ExpandHeight(true));
        leftScroll = EditorGUILayout.BeginScrollView(leftScroll);

        DrawAssetSummary();
        EditorGUILayout.Space(8f);
        DrawPresetPanel();
        EditorGUILayout.Space(8f);
        DrawQuickActionsPanel();
        EditorGUILayout.Space(8f);
        DrawDiagnosticsPanel();

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawAssetSummary()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("资源概览", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("攻击序列", sequence != null ? sequence.name : "未指定");
        EditorGUILayout.LabelField("武器数据", weaponData != null ? weaponData.name : "未指定");

        if (sequence != null)
        {
            EditorGUILayout.LabelField("总时长", sequence.Duration.ToString("0.###") + "s");
            EditorGUILayout.LabelField("动作帧数", GetMotionCount().ToString());
            EditorGUILayout.LabelField("事件帧数", GetEventCount().ToString());
        }

        EditorGUILayout.Space(4f);

        using (new EditorGUI.DisabledScope(sequence == null))
        {
            if (GUILayout.Button("定位攻击序列资源"))
            {
                EditorGUIUtility.PingObject(sequence);
            }

            if (GUILayout.Button("复制当前攻击序列"))
            {
                DuplicateSequenceAsset();
                GUIUtility.ExitGUI();
            }
        }

        if (GUILayout.Button("创建新攻击序列"))
        {
            CreateNewSequenceAsset();
            GUIUtility.ExitGUI();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawPresetPanel()
    {
        using (new EditorGUI.DisabledScope(sequence == null))
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("预设库", EditorStyles.boldLabel);
            selectedPreset = (WeaponAnimationSequencePresetId)EditorGUILayout.Popup("预设", (int)selectedPreset, PresetLabels);

            if (TryGetPresetDefinition(selectedPreset, out WeaponAnimationSequencePresetDefinition definition))
            {
                WeaponAnimationSequencePresetData data = definition.Builder();
                EditorGUILayout.LabelField("名称", definition.DisplayName);
                EditorGUILayout.LabelField("时长", data.Duration.ToString("0.###") + "s");
                EditorGUILayout.LabelField("动作 / 事件", data.MotionFrameCount + " / " + data.EventCount);
            }

            if (GUILayout.Button("套用预设"))
            {
                ApplySelectedPreset();
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.EndVertical();
        }
    }

    private void DrawQuickActionsPanel()
    {
        using (new EditorGUI.DisabledScope(sequence == null))
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("快捷操作", EditorStyles.boldLabel);

            if (GUILayout.Button("按时间排序关键帧"))
            {
                SortSequenceKeyframes();
                GUIUtility.ExitGUI();
            }

            if (GUILayout.Button("修复并规范化序列"))
            {
                ClampAndRepairSequence();
                GUIUtility.ExitGUI();
            }

            if (GUILayout.Button("在当前预览时间插入动作帧"))
            {
                AddMotionFrameAtPreviewTime();
                GUIUtility.ExitGUI();
            }

            if (GUILayout.Button("在当前预览时间插入事件帧"))
            {
                AddEventFrameAtPreviewTime();
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.EndVertical();
        }
    }

    private void DrawDiagnosticsPanel()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("诊断", EditorStyles.boldLabel);

        List<string> warnings = BuildDiagnostics();
        if (warnings.Count == 0)
        {
            EditorGUILayout.HelpBox("当前序列结构和武器映射看起来正常。", MessageType.None);
        }
        else
        {
            for (int i = 0; i < warnings.Count; i++)
            {
                EditorGUILayout.HelpBox(warnings[i], MessageType.Warning);
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawCenterPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

        string[] tabs = weaponData != null
            ? new[] { "攻击序列", "武器配置" }
            : new[] { "攻击序列" };
        editTab = Mathf.Clamp(editTab, 0, tabs.Length - 1);
        editTab = GUILayout.Toolbar(editTab, tabs);

        centerScroll = EditorGUILayout.BeginScrollView(centerScroll);
        if (editTab == 1 && weaponData != null)
        {
            DrawWeaponEditorPanel();
        }
        else
        {
            DrawSequenceEditorPanel();
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawSequenceEditorPanel()
    {
        if (sequenceObject == null)
        {
            EditorGUILayout.HelpBox("请先载入一个 AttackSequenceDefinitionSO 后再编辑关键帧。", MessageType.Info);
            return;
        }

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("基础设置", EditorStyles.boldLabel);
        DrawSequenceProperty("duration");
        DrawSequenceProperty("restoreDefaultPoseOnComplete");
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(8f);
        DrawMotionEditor();
        EditorGUILayout.Space(8f);
        DrawEventEditor();
    }

    private void DrawMotionEditor()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("动作关键帧", EditorStyles.boldLabel);

        if (motionList == null)
        {
            EnsureListsReady();
        }

        if (motionList == null)
        {
            EditorGUILayout.HelpBox("动作关键帧列表暂时不可用，窗口会自动重建。", MessageType.Warning);
            EditorGUILayout.EndVertical();
            return;
        }

        DrawMotionListToolbar();
        EditorGUILayout.Space(4f);
        EditorGUILayout.BeginHorizontal();

        float listWidth = Mathf.Clamp(position.width * 0.19f, MOTION_LIST_MIN_WIDTH, MOTION_LIST_MAX_WIDTH);
        EditorGUILayout.BeginVertical(GUILayout.Width(listWidth));
        motionList.DoLayoutList();
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
        DrawSelectedMotionInspector();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawSelectedMotionInspector()
    {
        SerializedProperty frames = sequenceObject.FindProperty("motionKeyframes");
        if (frames == null || frames.arraySize == 0)
        {
            return;
        }

        selectedMotionIndex = Mathf.Clamp(selectedMotionIndex < 0 ? motionList.index : selectedMotionIndex, 0, frames.arraySize - 1);
        motionList.index = selectedMotionIndex;
        SerializedProperty element = frames.GetArrayElementAtIndex(selectedMotionIndex);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("当前动作帧 " + selectedMotionIndex, EditorStyles.boldLabel);
        if (GUILayout.Button("取当前预览时间", GUILayout.Width(130f)))
        {
            element.FindPropertyRelative("normalizedTime").floatValue = previewNormalizedTime;
        }

        if (GUILayout.Button("跳到此帧", GUILayout.Width(85f)))
        {
            previewNormalizedTime = element.FindPropertyRelative("normalizedTime").floatValue;
        }

        if (GUILayout.Button("复制", GUILayout.Width(85f)))
        {
            DuplicateMotionFrame(selectedMotionIndex);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return;
        }

        EditorGUILayout.EndHorizontal();

        SerializedProperty normalizedTime = element.FindPropertyRelative("normalizedTime");
        SerializedProperty xPositionMode = element.FindPropertyRelative("xPositionMode");
        SerializedProperty yPositionMode = element.FindPropertyRelative("yPositionMode");
        SerializedProperty localPositionX = element.FindPropertyRelative("localPositionX");
        SerializedProperty localPositionY = element.FindPropertyRelative("localPositionY");
        SerializedProperty dynamicPositionStrategy = element.FindPropertyRelative("dynamicPositionStrategy");
        SerializedProperty xDynamicMinNormalizedReach = element.FindPropertyRelative("xDynamicMinNormalizedReach");
        SerializedProperty xDynamicMaxNormalizedReach = element.FindPropertyRelative("xDynamicMaxNormalizedReach");
        SerializedProperty yDynamicMinNormalizedReach = element.FindPropertyRelative("yDynamicMinNormalizedReach");
        SerializedProperty yDynamicMaxNormalizedReach = element.FindPropertyRelative("yDynamicMaxNormalizedReach");
        SerializedProperty localEulerAngles = element.FindPropertyRelative("localEulerAngles");
        SerializedProperty ease = element.FindPropertyRelative("ease");
        SerializedProperty customCurve = element.FindPropertyRelative("customCurve");

        EditorGUILayout.Slider(normalizedTime, 0f, 1f, new GUIContent("归一化时间"));
        float resolvedXBeforeModeChange = ResolveSerializedMotionAxisValue(element, MotionAxis.X);
        EditorGUI.BeginChangeCheck();
        DrawLocalizedEnumPopup(xPositionMode, "X 轴模式", MotionPositionModeLabels);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(sequence, "切换动作帧 X 轴模式");
            ConvertMotionAxisModePreservingPose(element, MotionAxis.X, resolvedXBeforeModeChange);
        }

        DrawAxisValues("X 轴", xPositionMode, localPositionX, xDynamicMinNormalizedReach, xDynamicMaxNormalizedReach, dynamicPositionStrategy);

        float resolvedYBeforeModeChange = ResolveSerializedMotionAxisValue(element, MotionAxis.Y);
        EditorGUI.BeginChangeCheck();
        DrawLocalizedEnumPopup(yPositionMode, "Y 轴模式", MotionPositionModeLabels);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(sequence, "切换动作帧 Y 轴模式");
            ConvertMotionAxisModePreservingPose(element, MotionAxis.Y, resolvedYBeforeModeChange);
        }

        DrawAxisValues("Y 轴", yPositionMode, localPositionY, yDynamicMinNormalizedReach, yDynamicMaxNormalizedReach, dynamicPositionStrategy);

        bool hasDynamicAxis = (WeaponMotionPositionMode)xPositionMode.enumValueIndex == WeaponMotionPositionMode.DynamicFromTarget ||
                              (WeaponMotionPositionMode)yPositionMode.enumValueIndex == WeaponMotionPositionMode.DynamicFromTarget;
        if (hasDynamicAxis)
        {
            float resolvedXBeforeStrategyChange = ResolveSerializedMotionAxisValue(element, MotionAxis.X);
            float resolvedYBeforeStrategyChange = ResolveSerializedMotionAxisValue(element, MotionAxis.Y);
            EditorGUI.BeginChangeCheck();
            DrawLocalizedEnumPopup(dynamicPositionStrategy, "动态位置策略", DynamicStrategyLabels);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(sequence, "切换动作帧动态位置策略");
                if ((WeaponMotionPositionMode)xPositionMode.enumValueIndex == WeaponMotionPositionMode.DynamicFromTarget)
                {
                    SetSerializedMotionAxisResolvedValue(element, MotionAxis.X, resolvedXBeforeStrategyChange);
                }

                if ((WeaponMotionPositionMode)yPositionMode.enumValueIndex == WeaponMotionPositionMode.DynamicFromTarget)
                {
                    SetSerializedMotionAxisResolvedValue(element, MotionAxis.Y, resolvedYBeforeStrategyChange);
                }
            }
        }

        EditorGUILayout.PropertyField(localEulerAngles, new GUIContent("局部欧拉角"));
        DrawLocalizedEnumPopup(ease, "缓动类型", MotionEaseLabels);
        if ((WeaponMotionEase)ease.enumValueIndex == WeaponMotionEase.CustomCurve)
        {
            EditorGUILayout.PropertyField(customCurve, new GUIContent("自定义曲线"));
        }

        DrawSelectedMotionInspectorStatus(element);
        EditorGUILayout.EndVertical();
    }

    private void DrawEventEditor()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("事件关键帧", EditorStyles.boldLabel);

        if (eventList == null)
        {
            EnsureListsReady();
        }

        if (eventList == null)
        {
            EditorGUILayout.HelpBox("事件关键帧列表暂时不可用，窗口会自动重建。", MessageType.Warning);
            EditorGUILayout.EndVertical();
            return;
        }

        DrawEventListToolbar();
        eventList.DoLayoutList();
        DrawSelectedEventInspector();

        EditorGUILayout.EndVertical();
    }

    private void DrawSelectedEventInspector()
    {
        SerializedProperty frames = sequenceObject.FindProperty("eventKeyframes");
        if (frames == null || frames.arraySize == 0)
        {
            return;
        }

        selectedEventIndex = Mathf.Clamp(selectedEventIndex < 0 ? eventList.index : selectedEventIndex, 0, frames.arraySize - 1);
        eventList.index = selectedEventIndex;
        SerializedProperty element = frames.GetArrayElementAtIndex(selectedEventIndex);

        EditorGUILayout.Space(6f);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("当前事件帧 " + selectedEventIndex, EditorStyles.boldLabel);
        if (GUILayout.Button("取当前预览时间", GUILayout.Width(130f)))
        {
            element.FindPropertyRelative("normalizedTime").floatValue = previewNormalizedTime;
        }

        if (GUILayout.Button("复制", GUILayout.Width(85f)))
        {
            DuplicateEventFrame(selectedEventIndex);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return;
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Slider(element.FindPropertyRelative("normalizedTime"), 0f, 1f, new GUIContent("归一化时间"));
        DrawLocalizedEnumPopup(element.FindPropertyRelative("eventType"), "事件类型", EventTypeLabels);
        EditorGUILayout.PropertyField(element.FindPropertyRelative("eventKey"), new GUIContent("事件键值"));
        DrawEventWiringHint(element);
        EditorGUILayout.EndVertical();
    }

    private void DrawWeaponEditorPanel()
    {
        if (weaponObject == null)
        {
            EditorGUILayout.HelpBox("请先载入一个 WeaponDataSO 后再编辑武器配置。", MessageType.Info);
            return;
        }

        SerializedProperty attackSequence = weaponObject.FindProperty("attackSequence");
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("运行时引用", EditorStyles.boldLabel);
        DrawWeaponProperty("weaponPrefab");
        DrawWeaponProperty("constructionScheme");
        EditorGUILayout.PropertyField(attackSequence, new GUIContent("攻击序列"));

        EditorGUILayout.BeginHorizontal();
        using (new EditorGUI.DisabledScope(sequence == null))
        {
            if (GUILayout.Button("挂载当前序列"))
            {
                attackSequence.objectReferenceValue = sequence;
            }
        }

        using (new EditorGUI.DisabledScope(attackSequence.objectReferenceValue == null))
        {
            if (GUILayout.Button("编辑武器已挂序列"))
            {
                sequence = attackSequence.objectReferenceValue as AttackSequenceDefinitionSO;
                RebuildSerializedObjects();
                GUIUtility.ExitGUI();
            }
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(8f);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("攻击表现", EditorStyles.boldLabel);
        DrawWeaponProperty("visualForwardAngle");
        DrawWeaponProperty("stopAimingWhenAttackReady");
        DrawWeaponProperty("attackSequenceOccupancy");
        DrawWeaponProperty("sequenceProjectileList", true);
        DrawWeaponProperty("sequenceSfxList", true);
        DrawWeaponProperty("sequenceVfxList", true);
        DrawWeaponProperty("hitSfxKey");
        DrawWeaponProperty("meleeHitVfxPrefab");
        DrawWeaponProperty("meleeHitBoxSize");
        DrawWeaponProperty("meleeHitOffset");
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(8f);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("属性", EditorStyles.boldLabel);
        DrawWeaponProperty("attack");
        DrawWeaponProperty("attackSpeed");
        DrawWeaponProperty("criticalChance");
        DrawWeaponProperty("criticalPercent");
        DrawWeaponProperty("range");
        EditorGUILayout.EndVertical();
    }

    private void DrawPreviewPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(PREVIEW_PANEL_WIDTH), GUILayout.MaxWidth(PREVIEW_PANEL_WIDTH), GUILayout.ExpandHeight(true));
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(PREVIEW_PANEL_WIDTH), GUILayout.MaxWidth(PREVIEW_PANEL_WIDTH));
        EditorGUILayout.LabelField("图形预览", EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(sequence == null))
        {
            DrawPreviewControls();

            float previewHeight = Mathf.Clamp(position.height - 210f, 260f, 520f);
            Rect previewRect = GUILayoutUtility.GetRect(1f, previewHeight, GUILayout.ExpandWidth(true), GUILayout.MaxWidth(PREVIEW_PANEL_WIDTH - PANEL_PADDING * 2f));
            DrawPreviewCanvas(previewRect);

            Rect timelineRect = GUILayoutUtility.GetRect(1f, 72f, GUILayout.ExpandWidth(true), GUILayout.MaxWidth(PREVIEW_PANEL_WIDTH - PANEL_PADDING * 2f));
            DrawTimeline(timelineRect);

            if (showCurvePanel)
            {
                Rect curveRect = GUILayoutUtility.GetRect(1f, 158f, GUILayout.ExpandWidth(true), GUILayout.MaxWidth(PREVIEW_PANEL_WIDTH - PANEL_PADDING * 2f));
                DrawCurvePanel(curveRect);
            }
        }

        EditorGUILayout.EndVertical();
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndVertical();
    }

    private void DrawPreviewControls()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(previewPlaying ? "暂停" : "播放", GUILayout.Width(72f)))
        {
            previewPlaying = !previewPlaying;
            lastEditorTime = EditorApplication.timeSinceStartup;
        }

        if (GUILayout.Button("重置", GUILayout.Width(60f)))
        {
            previewNormalizedTime = 0f;
            previewPlaying = false;
        }

        loopPreview = GUILayout.Toggle(loopPreview, "循环", GUILayout.Width(54f));
        showMotionPath = GUILayout.Toggle(showMotionPath, "路径", GUILayout.Width(54f));
        showHitBox = GUILayout.Toggle(showHitBox, "命中框", GUILayout.Width(72f));
        showCurvePanel = GUILayout.Toggle(showCurvePanel, "曲线", GUILayout.Width(54f));
        showWeaponSprite = GUILayout.Toggle(showWeaponSprite, "武器图", GUILayout.Width(66f));
        showVisualForwardGuide = GUILayout.Toggle(showVisualForwardGuide, "朝向", GUILayout.Width(54f));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        using (new EditorGUI.DisabledScope(sequence == null))
        {
            if (GUILayout.Button("适配视图", GUILayout.Width(78f)))
            {
                FitPreviewScaleToCurrentData();
            }
        }

        EditorGUILayout.LabelField("美术角 " + ResolveVisualForwardAngle().ToString("0.#") + "°", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();

        previewNormalizedTime = EditorGUILayout.Slider("时间", previewNormalizedTime, 0f, 1f);
        previewSpeed = EditorGUILayout.Slider("播放速度", previewSpeed, 0.1f, 3f);
        previewPixelsPerUnit = EditorGUILayout.Slider("缩放", previewPixelsPerUnit, MIN_PREVIEW_SCALE, MAX_PREVIEW_SCALE);

        previewAttackRange = EditorGUILayout.Slider("预览攻击范围", previewAttackRange, 0.1f, 10f);
        previewTargetDistance = EditorGUILayout.Slider("目标距离", previewTargetDistance, 0f, Mathf.Max(0.1f, previewAttackRange));
        curveChannel = (PreviewCurveChannel)EditorGUILayout.Popup("曲线通道", (int)curveChannel, CurveChannelLabels);
    }

    private void DrawPreviewCanvas(Rect rect)
    {
        GUI.BeginGroup(rect);
        Rect localRect = new Rect(0f, 0f, rect.width, rect.height);
        EditorGUI.DrawRect(localRect, new Color(0.12f, 0.12f, 0.12f, 1f));
        Rect inner = new Rect(localRect.x + PANEL_PADDING, localRect.y + PANEL_PADDING, localRect.width - PANEL_PADDING * 2f, localRect.height - PANEL_PADDING * 2f);
        EditorGUI.DrawRect(inner, new Color(0.17f, 0.17f, 0.17f, 1f));

        if (sequence == null)
        {
            GUI.Label(inner, "当前没有攻击序列。", EditorStyles.centeredGreyMiniLabel);
            GUI.EndGroup();
            return;
        }

        HandlePreviewCanvasInput(inner);

        Handles.BeginGUI();
        DrawGrid(inner);
        DrawTargetMarker(inner);

        if (showMotionPath)
        {
            DrawMotionPath(inner);
        }

        DrawMotionKeyframeMarkers(inner);

        PreviewPose pose = SamplePose(previewNormalizedTime);
        if (showHitBox && weaponData != null)
        {
            DrawMeleeHitBox(inner, pose);
        }

        DrawWeaponPose(inner, pose);
        DrawVisualForwardGuide(inner, pose);
        Handles.EndGUI();

        DrawWeaponSpriteOverlay(inner, pose);
        DrawActiveEventBadges(inner);
        GUI.EndGroup();
    }

    private void DrawTimeline(Rect rect)
    {
        GUI.BeginGroup(rect);
        Rect localRect = new Rect(0f, 0f, rect.width, rect.height);
        EditorGUI.DrawRect(localRect, new Color(0.13f, 0.13f, 0.13f, 1f));
        Rect track = new Rect(localRect.x + 8f, localRect.y + 24f, localRect.width - 16f, 22f);
        EditorGUI.DrawRect(track, new Color(0.22f, 0.22f, 0.22f, 1f));
        GUI.Label(new Rect(track.x + 52f, localRect.y, 220f, 18f), "拖拽关键帧可直接调整时间", EditorStyles.miniLabel);

        HandleTimelineInput(track);
        DrawHitWindowBands(track);
        DrawTimelineMotionMarkers(track);
        DrawTimelineEventMarkers(track);

        float x = Mathf.Lerp(track.xMin, track.xMax, Mathf.Clamp01(previewNormalizedTime));
        EditorGUI.DrawRect(new Rect(x - 1f, track.y - 8f, 2f, track.height + 16f), Color.white);

        GUI.Label(new Rect(track.x, localRect.y, track.width, 16f), "0.00", EditorStyles.miniLabel);
        GUI.Label(new Rect(track.xMax - 35f, localRect.y, 35f, 16f), "1.00", EditorStyles.miniLabel);
        GUI.Label(new Rect(track.x + track.width * previewNormalizedTime - 24f, track.yMax + 2f, 48f, 16f),
            previewNormalizedTime.ToString("0.00"),
            EditorStyles.centeredGreyMiniLabel);
        GUI.EndGroup();
    }

    private void DrawGrid(Rect rect)
    {
        Vector2 origin = WorldToCanvas(Vector2.zero, rect);
        Handles.color = new Color(1f, 1f, 1f, 0.07f);
        float scale = Mathf.Max(1f, previewPixelsPerUnit);

        for (float x = origin.x % scale; x < rect.xMax; x += scale)
        {
            Handles.DrawLine(new Vector3(x, rect.yMin), new Vector3(x, rect.yMax));
        }

        for (float y = origin.y % scale; y < rect.yMax; y += scale)
        {
            Handles.DrawLine(new Vector3(rect.xMin, y), new Vector3(rect.xMax, y));
        }

        DrawGuiLine(new Vector2(rect.xMin, origin.y), new Vector2(rect.xMax, origin.y), new Color(1f, 0.35f, 0.35f, 0.6f), 1.5f);
        DrawGuiLine(new Vector2(origin.x, rect.yMin), new Vector2(origin.x, rect.yMax), new Color(0.45f, 1f, 0.45f, 0.6f), 1.5f);
    }

    private void DrawTargetMarker(Rect rect)
    {
        Vector2 target = WorldToCanvas(new Vector2(0f, previewTargetDistance), rect);
        Handles.color = new Color(1f, 0.75f, 0.2f, 1f);
        Handles.DrawWireDisc(target, Vector3.forward, 8f);
        Handles.DrawLine(target + new Vector2(-12f, 0f), target + new Vector2(12f, 0f));
        Handles.DrawLine(target + new Vector2(0f, -12f), target + new Vector2(0f, 12f));
    }

    private void DrawMotionPath(Rect rect)
    {
        const int SAMPLE_COUNT = 96;
        Vector3[] points = new Vector3[SAMPLE_COUNT];
        for (int i = 0; i < SAMPLE_COUNT; i++)
        {
            float t = i / (SAMPLE_COUNT - 1f);
            PreviewPose pose = SamplePose(t);
            Vector2 canvasPoint = WorldToCanvas(pose.position, rect);
            points[i] = new Vector3(canvasPoint.x, canvasPoint.y, 0f);
        }

        Handles.color = new Color(0.2f, 0.75f, 1f, 0.78f);
        Handles.DrawAAPolyLine(2.5f, points);
    }

    private void DrawMotionKeyframeMarkers(Rect rect)
    {
        IReadOnlyList<WeaponMotionKeyframe> frames = sequence.MotionKeyframes;
        if (frames == null)
        {
            return;
        }

        for (int i = 0; i < frames.Count; i++)
        {
            PreviewPose pose = SamplePose(frames[i].normalizedTime);
            Vector2 point = WorldToCanvas(pose.position, rect);
            Handles.color = i == selectedMotionIndex ? Color.yellow : new Color(0.8f, 0.95f, 1f, 1f);
            Handles.DrawSolidDisc(point, Vector3.forward, i == selectedMotionIndex ? 4.5f : 3.5f);
            GUI.Label(new Rect(point.x + 5f, point.y - 9f, 28f, 18f), i.ToString(), EditorStyles.miniLabel);
        }
    }

    private void DrawMeleeHitBox(Rect rect, PreviewPose pose)
    {
        Vector2 offset = weaponData.MeleeHitOffset;
        Vector2 size = weaponData.MeleeHitBoxSize;
        Vector2 center = pose.position + Rotate(offset, pose.angleZ);
        DrawRotatedRect(rect, center, size, pose.angleZ, new Color(1f, 0.2f, 0.2f, 0.8f));
    }

    private void DrawWeaponPose(Rect rect, PreviewPose pose)
    {
        if (showWeaponSprite && (ResolveWeaponPreviewSprite() != null || ResolveWeaponPrefabPreviewTexture() != null))
        {
            Vector2 centerOnly = WorldToCanvas(pose.position, rect);
            Handles.color = Color.white;
            Handles.DrawSolidDisc(centerOnly, Vector3.forward, 3.5f);
            return;
        }

        Vector2 center = WorldToCanvas(pose.position, rect);
        Vector2 forward = Rotate(Vector2.up, pose.angleZ);
        Vector2 right = Rotate(Vector2.right, pose.angleZ);

        Vector2 handle = center - forward * 24f;
        Vector2 tip = center + forward * 58f;
        Vector2 guardA = center - right * 18f;
        Vector2 guardB = center + right * 18f;

        DrawGuiLine(handle, tip, new Color(0.02f, 0.02f, 0.02f, 1f), 8f);
        DrawGuiLine(handle, tip, new Color(0.78f, 0.88f, 1f, 1f), 4f);
        DrawGuiLine(guardA, guardB, new Color(1f, 0.86f, 0.3f, 1f), 4f);

        Handles.color = Color.white;
        Handles.DrawSolidDisc(center, Vector3.forward, 4f);
        Handles.color = new Color(0.4f, 1f, 0.45f, 1f);
        Handles.DrawSolidDisc(tip, Vector3.forward, 5f);
    }

    private void DrawVisualForwardGuide(Rect rect, PreviewPose pose)
    {
        if (!showVisualForwardGuide)
        {
            return;
        }

        Vector2 center = WorldToCanvas(pose.position, rect);
        float visualAngle = pose.angleZ + ResolveVisualForwardAngle();
        Vector2 visualForward = Rotate(Vector2.up, visualAngle);
        Vector2 end = center + visualForward * 64f;
        DrawGuiLine(center, end, new Color(1f, 0.78f, 0.22f, 0.92f), 2f);
        Handles.color = new Color(1f, 0.78f, 0.22f, 0.92f);
        Handles.DrawSolidDisc(end, Vector3.forward, 4f);
        GUI.Label(new Rect(end.x + 5f, end.y - 10f, 70f, 18f), "美术朝向", EditorStyles.miniLabel);
    }

    private void DrawWeaponSpriteOverlay(Rect rect, PreviewPose pose)
    {
        if (!showWeaponSprite)
        {
            return;
        }

        Sprite sprite = ResolveWeaponPreviewSprite();
        Texture2D previewTexture = sprite != null ? sprite.texture : ResolveWeaponPrefabPreviewTexture();
        if (previewTexture == null)
        {
            return;
        }

        Vector2 center = WorldToCanvas(pose.position, rect);
        float visualAngle = pose.angleZ + ResolveVisualForwardAngle();
        Vector2 drawSize = sprite != null ? ResolveWeaponPreviewSpriteSize(sprite) : ResolveWeaponPreviewTextureSize(previewTexture);
        Rect drawRect = new Rect(center.x - drawSize.x * 0.5f, center.y - drawSize.y * 0.5f, drawSize.x, drawSize.y);

        Matrix4x4 previousMatrix = GUI.matrix;
        GUIUtility.RotateAroundPivot(-visualAngle, center);
        if (sprite != null)
        {
            DrawSpriteTexture(drawRect, sprite);
        }
        else
        {
            GUI.DrawTexture(drawRect, previewTexture, ScaleMode.ScaleToFit, true);
        }
        GUI.matrix = previousMatrix;
    }

    private Sprite ResolveWeaponPreviewSprite()
    {
        if (weaponData != null && weaponData.ItemIcon != null)
        {
            return weaponData.ItemIcon;
        }

        Weapon prefab = weaponData != null ? weaponData.WeaponPrefab : null;
        if (prefab == null)
        {
            return null;
        }

        EntityRenderer entityRenderer = prefab.GetComponentInChildren<EntityRenderer>(true);
        if (entityRenderer != null && entityRenderer.SpriteRenderer != null && entityRenderer.SpriteRenderer.sprite != null)
        {
            return entityRenderer.SpriteRenderer.sprite;
        }

        SpriteRenderer spriteRenderer = prefab.GetComponentInChildren<SpriteRenderer>(true);
        return spriteRenderer != null ? spriteRenderer.sprite : null;
    }

    private Texture2D ResolveWeaponPrefabPreviewTexture()
    {
        Weapon prefab = weaponData != null ? weaponData.WeaponPrefab : null;
        if (prefab == null)
        {
            return null;
        }

        Texture2D preview = AssetPreview.GetAssetPreview(prefab.gameObject);
        if (preview != null)
        {
            return preview;
        }

        return AssetPreview.GetMiniThumbnail(prefab.gameObject) as Texture2D;
    }

    private Vector2 ResolveWeaponPreviewSpriteSize(Sprite sprite)
    {
        if (sprite == null)
        {
            return Vector2.one * 72f;
        }

        Vector2 worldSize = sprite.bounds.size;
        if (worldSize.sqrMagnitude <= 0.0001f)
        {
            worldSize = Vector2.one;
        }

        Vector2 pixelSize = worldSize * Mathf.Max(1f, previewPixelsPerUnit);
        return ClampPreviewDrawSize(pixelSize, 42f, 150f);
    }

    private Vector2 ResolveWeaponPreviewTextureSize(Texture2D texture)
    {
        if (texture == null || texture.width <= 0 || texture.height <= 0)
        {
            return Vector2.one * 72f;
        }

        float maxDimension = 112f;
        float width = texture.width;
        float height = texture.height;
        float scale = maxDimension / Mathf.Max(width, height);
        return ClampPreviewDrawSize(new Vector2(width * scale, height * scale), 42f, 150f);
    }

    private static Vector2 ClampPreviewDrawSize(Vector2 size, float minDimension, float maxDimension)
    {
        float largest = Mathf.Max(size.x, size.y);
        if (largest <= 0.0001f)
        {
            return Vector2.one * minDimension;
        }

        float scale = 1f;
        if (largest < minDimension)
        {
            scale = minDimension / largest;
        }
        else if (largest > maxDimension)
        {
            scale = maxDimension / largest;
        }

        return size * scale;
    }

    private static void DrawSpriteTexture(Rect drawRect, Sprite sprite)
    {
        if (sprite == null || sprite.texture == null)
        {
            return;
        }

        Texture texture = sprite.texture;
        Rect textureRect = sprite.textureRect;
        Rect uv = new Rect(
            textureRect.x / texture.width,
            textureRect.y / texture.height,
            textureRect.width / texture.width,
            textureRect.height / texture.height);
        GUI.DrawTextureWithTexCoords(drawRect, texture, uv, true);
    }

    private void DrawActiveEventBadges(Rect rect)
    {
        IReadOnlyList<WeaponSequenceEventKeyframe> frames = sequence.EventKeyframes;
        if (frames == null)
        {
            return;
        }

        int badgeIndex = 0;
        for (int i = 0; i < frames.Count; i++)
        {
            WeaponSequenceEventKeyframe frame = frames[i];
            if (Mathf.Abs(frame.normalizedTime - previewNormalizedTime) > 0.018f)
            {
                continue;
            }

            Rect badgeRect = new Rect(rect.x + 10f, rect.y + 10f + badgeIndex * 22f, rect.width - 20f, 20f);
            EditorGUI.DrawRect(badgeRect, GetEventColor(frame.eventType));
            GUI.Label(badgeRect, GetEventTypeLabel(frame.eventType) + "  键值 " + frame.eventKey, EditorStyles.whiteMiniLabel);
            badgeIndex++;
        }
    }

    private void DrawHitWindowBands(Rect track)
    {
        IReadOnlyList<WeaponSequenceEventKeyframe> frames = sequence.EventKeyframes;
        if (frames == null)
        {
            return;
        }

        for (int i = 0; i < frames.Count; i++)
        {
            WeaponSequenceEventKeyframe open = frames[i];
            if (open.eventType != WeaponSequenceEventType.OpenHitWindow)
            {
                continue;
            }

            float closeTime = 1f;
            for (int j = i + 1; j < frames.Count; j++)
            {
                WeaponSequenceEventKeyframe candidate = frames[j];
                if (candidate.eventType == WeaponSequenceEventType.CloseHitWindow && candidate.eventKey == open.eventKey)
                {
                    closeTime = candidate.normalizedTime;
                    break;
                }
            }

            float xMin = Mathf.Lerp(track.xMin, track.xMax, Mathf.Clamp01(open.normalizedTime));
            float xMax = Mathf.Lerp(track.xMin, track.xMax, Mathf.Clamp01(closeTime));
            EditorGUI.DrawRect(new Rect(xMin, track.yMin, Mathf.Max(2f, xMax - xMin), track.height), new Color(1f, 0.25f, 0.15f, 0.28f));
        }
    }

    private void DrawTimelineMotionMarkers(Rect track)
    {
        IReadOnlyList<WeaponMotionKeyframe> frames = sequence.MotionKeyframes;
        if (frames == null)
        {
            return;
        }

        for (int i = 0; i < frames.Count; i++)
        {
            float x = Mathf.Lerp(track.xMin, track.xMax, Mathf.Clamp01(frames[i].normalizedTime));
            Color color = i == selectedMotionIndex ? Color.yellow : new Color(0.35f, 0.75f, 1f, 1f);
            EditorGUI.DrawRect(new Rect(x - 2f, track.yMin - 2f, 4f, track.height + 4f), color);
            GUI.Label(new Rect(x + 4f, track.yMin + 1f, 32f, 16f), "动" + i, EditorStyles.miniLabel);
        }
    }

    private void DrawTimelineEventMarkers(Rect track)
    {
        IReadOnlyList<WeaponSequenceEventKeyframe> frames = sequence.EventKeyframes;
        if (frames == null)
        {
            return;
        }

        for (int i = 0; i < frames.Count; i++)
        {
            WeaponSequenceEventKeyframe frame = frames[i];
            float x = Mathf.Lerp(track.xMin, track.xMax, Mathf.Clamp01(frame.normalizedTime));
            Color color = i == selectedEventIndex ? Color.yellow : GetEventColor(frame.eventType);
            EditorGUI.DrawRect(new Rect(x - 3f, track.yMin - 11f, 6f, 10f), color);
            GUI.Label(new Rect(x + 4f, track.yMin - 13f, 32f, 16f), "事" + i, EditorStyles.miniLabel);
        }
    }

    private void HandleTimelineInput(Rect track)
    {
        if (sequenceObject == null)
        {
            return;
        }

        Event current = Event.current;
        int controlId = GUIUtility.GetControlID(FocusType.Passive, track);

        if (current.type == EventType.MouseDown && track.Contains(current.mousePosition))
        {
            if (current.button == 1)
            {
                ShowTimelineContextMenu(track, current.mousePosition);
                current.Use();
                return;
            }

            int hitMotionIndex = FindMotionMarkerAtTimeline(track, current.mousePosition);
            int hitEventIndex = FindEventMarkerAtTimeline(track, current.mousePosition);

            GUIUtility.hotControl = controlId;
            previewPlaying = false;

            if (hitMotionIndex >= 0)
            {
                Undo.RecordObject(sequence, "调整动作关键帧时间");
                timelineDragMode = TimelineDragMode.Motion;
                timelineDragIndex = hitMotionIndex;
                selectedMotionIndex = hitMotionIndex;
                if (motionList != null)
                {
                    motionList.index = hitMotionIndex;
                }
            }
            else if (hitEventIndex >= 0)
            {
                Undo.RecordObject(sequence, "调整事件关键帧时间");
                timelineDragMode = TimelineDragMode.Event;
                timelineDragIndex = hitEventIndex;
                selectedEventIndex = hitEventIndex;
                if (eventList != null)
                {
                    eventList.index = hitEventIndex;
                }
            }
            else
            {
                timelineDragMode = TimelineDragMode.Scrub;
                timelineDragIndex = -1;
            }

            SetPreviewTimeFromTimeline(track, current.mousePosition.x);
            current.Use();
        }
        else if (current.type == EventType.MouseDrag && GUIUtility.hotControl == controlId)
        {
            float normalizedTime = NormalizeTimelinePosition(track, current.mousePosition.x);
            if (timelineDragMode == TimelineDragMode.Motion)
            {
                SetMotionFrameTime(timelineDragIndex, normalizedTime);
                ApplyModifiedProperties();
            }
            else if (timelineDragMode == TimelineDragMode.Event)
            {
                SetEventFrameTime(timelineDragIndex, normalizedTime);
                ApplyModifiedProperties();
            }

            SetPreviewTimeFromTimeline(track, current.mousePosition.x);
            current.Use();
        }
        else if (current.type == EventType.MouseUp && GUIUtility.hotControl == controlId)
        {
            GUIUtility.hotControl = 0;
            timelineDragMode = TimelineDragMode.None;
            timelineDragIndex = -1;
            current.Use();
        }
    }

    private void SetPreviewTimeFromTimeline(Rect track, float mouseX)
    {
        previewNormalizedTime = NormalizeTimelinePosition(track, mouseX);
        Repaint();
    }

    private void DrawCurvePanel(Rect rect)
    {
        GUI.BeginGroup(rect);
        Rect localRect = new Rect(0f, 0f, rect.width, rect.height);
        EditorGUI.DrawRect(localRect, new Color(0.11f, 0.11f, 0.11f, 1f));
        Rect inner = new Rect(localRect.x + PANEL_PADDING, localRect.y + PANEL_PADDING, localRect.width - PANEL_PADDING * 2f, localRect.height - PANEL_PADDING * 2f);
        EditorGUI.DrawRect(inner, new Color(0.18f, 0.18f, 0.18f, 1f));

        GUI.Label(new Rect(inner.x + 6f, inner.y + 4f, inner.width - 12f, 18f), "曲线编辑：横向拖动改时间，纵向拖动改当前通道值", EditorStyles.miniLabel);
        Rect graphRect = new Rect(inner.x + 8f, inner.y + 26f, inner.width - 16f, inner.height - 34f);
        HandleCurvePanelInput(graphRect);
        DrawCurveGrid(graphRect);
        DrawCurveLine(graphRect);
        DrawCurveMarkers(graphRect);
        GUI.EndGroup();
    }

    private PreviewPose SamplePose(float normalizedTime)
    {
        if (sequence == null || sequence.MotionKeyframes == null || sequence.MotionKeyframes.Count == 0)
        {
            return new PreviewPose(Vector2.zero, 0f);
        }

        IReadOnlyList<WeaponMotionKeyframe> frames = sequence.MotionKeyframes;
        WeaponMotionKeyframe from = frames[0];
        WeaponMotionKeyframe to = frames[frames.Count - 1];
        int fromIndex = 0;
        int toIndex = frames.Count - 1;

        for (int i = 0; i < frames.Count - 1; i++)
        {
            WeaponMotionKeyframe current = frames[i];
            WeaponMotionKeyframe next = frames[i + 1];
            if (normalizedTime >= current.normalizedTime && normalizedTime <= next.normalizedTime)
            {
                from = current;
                to = next;
                fromIndex = i;
                toIndex = i + 1;
                break;
            }
        }

        float segmentLength = Mathf.Max(0.0001f, to.normalizedTime - from.normalizedTime);
        float linearT = Mathf.Clamp01((normalizedTime - from.normalizedTime) / segmentLength);
        float easedT = EvaluateEase(linearT, to.ease, to.customCurve);

        Vector2 fromPosition = ResolvePreviewPosition(from, fromIndex);
        Vector2 toPosition = ResolvePreviewPosition(to, toIndex);
        Vector2 position = Vector2.LerpUnclamped(fromPosition, toPosition, easedT);
        float angle = Mathf.LerpAngle(from.localEulerAngles.z, to.localEulerAngles.z, easedT);
        return new PreviewPose(position, angle);
    }

    private Vector2 ResolvePreviewPosition(WeaponMotionKeyframe keyframe, int keyframeIndex)
    {
        float resolvedX = keyframe.localPositionX;
        if (keyframe.xPositionMode == WeaponMotionPositionMode.DynamicFromTarget)
        {
            resolvedX = ResolveDynamicAxis(
                keyframe.localPositionX,
                keyframe.xDynamicMinNormalizedReach,
                keyframe.xDynamicMaxNormalizedReach,
                keyframe.dynamicPositionStrategy);
        }

        float resolvedY = keyframe.localPositionY;
        if (keyframe.yPositionMode == WeaponMotionPositionMode.DynamicFromTarget)
        {
            resolvedY = ResolveDynamicAxis(
                keyframe.localPositionY,
                keyframe.yDynamicMinNormalizedReach,
                keyframe.yDynamicMaxNormalizedReach,
                keyframe.dynamicPositionStrategy);
        }

        return new Vector2(resolvedX, resolvedY);
    }

    private float ResolveDynamicAxis(float configuredValue, float minReach, float maxReach, WeaponMotionDynamicPositionStrategy strategy)
    {
        float range = Mathf.Max(0.1f, previewAttackRange);
        float normalizedTargetDistance = Mathf.Clamp01(previewTargetDistance / range);
        float min = Mathf.Clamp01(minReach);
        float max = Mathf.Clamp(maxReach, min, 1f);

        float normalizedDistance = strategy == WeaponMotionDynamicPositionStrategy.TowardTargetClampedRadius
            ? Mathf.Clamp(normalizedTargetDistance, min, max)
            : Mathf.Clamp01(Mathf.Abs(configuredValue));
        return normalizedDistance * range * Mathf.Sign(Mathf.Approximately(configuredValue, 0f) ? 1f : configuredValue);
    }

    private void DrawMotionListToolbar()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("当前时间智能插入动作帧"))
        {
            InsertMotionFrameAtTime(previewNormalizedTime, null);
            GUIUtility.ExitGUI();
        }

        using (new EditorGUI.DisabledScope(selectedMotionIndex < 0))
        {
            if (GUILayout.Button("在选中帧后插入"))
            {
                InsertMotionFrameAfterSelection();
                GUIUtility.ExitGUI();
            }

            if (GUILayout.Button("删除选中动作帧"))
            {
                DeleteSelectedMotionFrame();
                GUIUtility.ExitGUI();
            }
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.HelpBox("左边选帧，右边看详情。时间轴可拖时间，预览图可拖位置，曲线图可拖节点做通道微调。", MessageType.None);
    }

    private void DrawEventListToolbar()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("当前时间插入事件帧"))
        {
            InsertEventFrameAtTime(previewNormalizedTime);
            GUIUtility.ExitGUI();
        }

        using (new EditorGUI.DisabledScope(selectedEventIndex < 0))
        {
            if (GUILayout.Button("在选中帧后插入"))
            {
                InsertEventFrameAfterSelection();
                GUIUtility.ExitGUI();
            }

            if (GUILayout.Button("删除选中事件帧"))
            {
                DeleteSelectedEventFrame();
                GUIUtility.ExitGUI();
            }
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.HelpBox("时间轴可左右拖动事件帧时间，右键时间轴还能快速插入动作帧或事件帧。", MessageType.None);
    }

    private void HandlePreviewCanvasInput(Rect rect)
    {
        if (sequenceObject == null)
        {
            return;
        }

        Event current = Event.current;
        int controlId = GUIUtility.GetControlID(FocusType.Passive, rect);

        if (current.type == EventType.ContextClick && rect.Contains(current.mousePosition))
        {
            ShowPreviewContextMenu(rect, current.mousePosition);
            current.Use();
            return;
        }

        if (current.type == EventType.MouseDown && current.button == 0 && rect.Contains(current.mousePosition))
        {
            int motionIndex = FindMotionMarkerAtCanvas(rect, current.mousePosition);
            if (motionIndex >= 0)
            {
                previewPlaying = false;
                Undo.RecordObject(sequence, "拖动动作关键帧位置");
                previewDragMotionIndex = motionIndex;
                selectedMotionIndex = motionIndex;
                if (motionList != null)
                {
                    motionList.index = motionIndex;
                }

                GUIUtility.hotControl = controlId;
                previewDragStartWorld = CanvasToWorld(current.mousePosition, rect);
                previewDragOriginalPosition = GetMotionFrameEditablePosition(motionIndex);
                current.Use();
                return;
            }
        }

        if (current.type == EventType.MouseDrag && GUIUtility.hotControl == controlId && previewDragMotionIndex >= 0)
        {
            Vector2 currentWorld = CanvasToWorld(current.mousePosition, rect);
            Vector2 deltaWorld = currentWorld - previewDragStartWorld;
            SetMotionFramePosition(previewDragMotionIndex, previewDragOriginalPosition + deltaWorld);
            ApplyModifiedProperties();
            Repaint();
            current.Use();
            return;
        }

        if (current.type == EventType.MouseUp && GUIUtility.hotControl == controlId)
        {
            GUIUtility.hotControl = 0;
            previewDragMotionIndex = -1;
            current.Use();
        }
    }

    private void ShowTimelineContextMenu(Rect track, Vector2 mousePosition)
    {
        float normalizedTime = NormalizeTimelinePosition(track, mousePosition.x);
        GenericMenu menu = new GenericMenu();
        menu.AddItem(new GUIContent("在这里插入/动作帧"), false, () => InsertMotionFrameAtTime(normalizedTime, null));
        menu.AddItem(new GUIContent("在这里插入/事件帧"), false, () => InsertEventFrameAtTime(normalizedTime));

        if (selectedMotionIndex >= 0)
        {
            menu.AddItem(new GUIContent("动作帧/复制选中动作帧"), false, () => DuplicateMotionFrame(selectedMotionIndex));
            menu.AddItem(new GUIContent("动作帧/删除选中动作帧"), false, DeleteSelectedMotionFrame);
        }
        else
        {
            menu.AddDisabledItem(new GUIContent("动作帧/复制选中动作帧"));
            menu.AddDisabledItem(new GUIContent("动作帧/删除选中动作帧"));
        }

        if (CanInsertSmoothFramesAfterSelected())
        {
            menu.AddItem(new GUIContent("动作帧/选中段智能补 1 点"), false, () => InsertInterpolatedMotionFramesForSelectedSegment(1));
            menu.AddItem(new GUIContent("动作帧/选中段智能补 2 点"), false, () => InsertInterpolatedMotionFramesForSelectedSegment(2));
            menu.AddItem(new GUIContent("动作帧/选中段智能补 3 点"), false, () => InsertInterpolatedMotionFramesForSelectedSegment(3));
        }
        else
        {
            menu.AddDisabledItem(new GUIContent("动作帧/选中段智能补 1 点"));
            menu.AddDisabledItem(new GUIContent("动作帧/选中段智能补 2 点"));
            menu.AddDisabledItem(new GUIContent("动作帧/选中段智能补 3 点"));
        }

        if (selectedEventIndex >= 0)
        {
            menu.AddItem(new GUIContent("事件帧/复制选中事件帧"), false, () => DuplicateEventFrame(selectedEventIndex));
            menu.AddItem(new GUIContent("事件帧/删除选中事件帧"), false, DeleteSelectedEventFrame);
        }
        else
        {
            menu.AddDisabledItem(new GUIContent("事件帧/复制选中事件帧"));
            menu.AddDisabledItem(new GUIContent("事件帧/删除选中事件帧"));
        }

        menu.ShowAsContext();
    }

    private void ShowPreviewContextMenu(Rect rect, Vector2 mousePosition)
    {
        Vector2 worldPosition = CanvasToWorld(mousePosition, rect);
        GenericMenu menu = new GenericMenu();
        menu.AddItem(new GUIContent("在这里插入/动作帧"), false, () => InsertMotionFrameAtTime(previewNormalizedTime, worldPosition));
        menu.AddItem(new GUIContent("在这里插入/事件帧"), false, () => InsertEventFrameAtTime(previewNormalizedTime));
        AddMotionGraphEditMenuItems(menu, worldPosition);

        menu.ShowAsContext();
    }

    private void AddMotionGraphEditMenuItems(GenericMenu menu, Vector2? moveSelectedToPosition)
    {
        menu.AddSeparator(string.Empty);

        if (selectedMotionIndex >= 0)
        {
            menu.AddItem(new GUIContent("选中动作帧/捕捉当前预览姿态"), false, CaptureSelectedMotionFrameFromPreview);
            if (moveSelectedToPosition.HasValue)
            {
                Vector2 targetPosition = moveSelectedToPosition.Value;
                menu.AddItem(new GUIContent("选中动作帧/移动到鼠标位置"), false, () => MoveSelectedMotionFrameToPosition(targetPosition));
            }
        }
        else
        {
            menu.AddDisabledItem(new GUIContent("选中动作帧/捕捉当前预览姿态"));
            if (moveSelectedToPosition.HasValue)
            {
                menu.AddDisabledItem(new GUIContent("选中动作帧/移动到鼠标位置"));
            }
        }

        if (CanInsertSmoothFramesAfterSelected())
        {
            menu.AddItem(new GUIContent("选中段/智能补 1 点"), false, () => InsertInterpolatedMotionFramesForSelectedSegment(1));
            menu.AddItem(new GUIContent("选中段/智能补 2 点"), false, () => InsertInterpolatedMotionFramesForSelectedSegment(2));
            menu.AddItem(new GUIContent("选中段/智能补 3 点"), false, () => InsertInterpolatedMotionFramesForSelectedSegment(3));
        }
        else
        {
            menu.AddDisabledItem(new GUIContent("选中段/智能补 1 点"));
            menu.AddDisabledItem(new GUIContent("选中段/智能补 2 点"));
            menu.AddDisabledItem(new GUIContent("选中段/智能补 3 点"));
        }

        if (GetMotionCount() >= 2)
        {
            menu.AddItem(new GUIContent("全部动作帧/每段智能补 1 点"), false, () => InsertInterpolatedMotionFramesForAllSegments(1));
            menu.AddItem(new GUIContent("全部动作帧/每段智能补 2 点"), false, () => InsertInterpolatedMotionFramesForAllSegments(2));
        }
        else
        {
            menu.AddDisabledItem(new GUIContent("全部动作帧/每段智能补 1 点"));
            menu.AddDisabledItem(new GUIContent("全部动作帧/每段智能补 2 点"));
        }
    }

    private void CaptureSelectedMotionFrameFromPreview()
    {
        if (sequenceObject == null || selectedMotionIndex < 0)
        {
            return;
        }

        SerializedProperty frames = sequenceObject.FindProperty("motionKeyframes");
        if (frames == null || selectedMotionIndex >= frames.arraySize)
        {
            return;
        }

        Undo.RecordObject(sequence, "捕捉当前预览姿态");
        PreviewPose currentPreviewPose = SamplePose(previewNormalizedTime);
        ApplyPoseToMotionFrame(frames.GetArrayElementAtIndex(selectedMotionIndex), currentPreviewPose.position, currentPreviewPose.angleZ);
        ApplyModifiedProperties();
        Repaint();
    }

    private void MoveSelectedMotionFrameToPosition(Vector2 position)
    {
        if (sequenceObject == null || selectedMotionIndex < 0)
        {
            return;
        }

        Undo.RecordObject(sequence, "移动选中动作关键帧");
        SetMotionFramePosition(selectedMotionIndex, position);
        ApplyModifiedProperties();
        Repaint();
    }

    private float NormalizeTimelinePosition(Rect track, float mouseX)
    {
        return Mathf.Clamp01((mouseX - track.xMin) / Mathf.Max(1f, track.width));
    }

    private int FindMotionMarkerAtTimeline(Rect track, Vector2 mousePosition)
    {
        if (sequence == null || sequence.MotionKeyframes == null)
        {
            return -1;
        }

        for (int i = 0; i < sequence.MotionKeyframes.Count; i++)
        {
            float x = Mathf.Lerp(track.xMin, track.xMax, Mathf.Clamp01(sequence.MotionKeyframes[i].normalizedTime));
            Rect hitRect = new Rect(x - 6f, track.yMin - 4f, 12f, track.height + 8f);
            if (hitRect.Contains(mousePosition))
            {
                return i;
            }
        }

        return -1;
    }

    private int FindEventMarkerAtTimeline(Rect track, Vector2 mousePosition)
    {
        if (sequence == null || sequence.EventKeyframes == null)
        {
            return -1;
        }

        for (int i = 0; i < sequence.EventKeyframes.Count; i++)
        {
            float x = Mathf.Lerp(track.xMin, track.xMax, Mathf.Clamp01(sequence.EventKeyframes[i].normalizedTime));
            Rect hitRect = new Rect(x - 6f, track.yMin - 14f, 12f, 14f);
            if (hitRect.Contains(mousePosition))
            {
                return i;
            }
        }

        return -1;
    }

    private int FindMotionMarkerAtCanvas(Rect rect, Vector2 mousePosition)
    {
        if (sequence == null || sequence.MotionKeyframes == null)
        {
            return -1;
        }

        for (int i = 0; i < sequence.MotionKeyframes.Count; i++)
        {
            PreviewPose pose = SamplePose(sequence.MotionKeyframes[i].normalizedTime);
            Vector2 marker = WorldToCanvas(pose.position, rect);
            if (Vector2.Distance(marker, mousePosition) <= 8f)
            {
                return i;
            }
        }

        return -1;
    }

    private SmartMotionSample BuildSmartMotionSample(float normalizedTime, bool preferLinearEase)
    {
        if (sequence == null || sequence.MotionKeyframes == null || sequence.MotionKeyframes.Count == 0)
        {
            return new SmartMotionSample(Vector2.zero, 0f, WeaponMotionEase.Linear);
        }

        IReadOnlyList<WeaponMotionKeyframe> frames = sequence.MotionKeyframes;
        if (frames.Count == 1)
        {
            PreviewPose singlePose = GetResolvedMotionFramePose(0);
            return new SmartMotionSample(singlePose.position, singlePose.angleZ, WeaponMotionEase.Linear);
        }

        FindMotionSegment(normalizedTime, out int fromIndex, out int toIndex);
        WeaponMotionKeyframe from = frames[fromIndex];
        WeaponMotionKeyframe to = frames[toIndex];
        float segmentLength = Mathf.Max(0.0001f, to.normalizedTime - from.normalizedTime);
        float localT = Mathf.Clamp01((normalizedTime - from.normalizedTime) / segmentLength);

        int prevIndex = Mathf.Max(0, fromIndex - 1);
        int nextIndex = Mathf.Min(frames.Count - 1, toIndex + 1);
        PreviewPose prevPose = GetResolvedMotionFramePose(prevIndex);
        PreviewPose fromPose = GetResolvedMotionFramePose(fromIndex);
        PreviewPose toPose = GetResolvedMotionFramePose(toIndex);
        PreviewPose nextPose = GetResolvedMotionFramePose(nextIndex);

        float prevTime = frames[prevIndex].normalizedTime;
        float fromTime = from.normalizedTime;
        float toTime = to.normalizedTime;
        float nextTime = frames[nextIndex].normalizedTime;

        Vector2 fromTangent = EstimatePositionTangent(prevPose.position, toPose.position, prevTime, toTime, fromTime, toTime);
        Vector2 toTangent = EstimatePositionTangent(fromPose.position, nextPose.position, fromTime, nextTime, fromTime, toTime);
        Vector2 position = EvaluateHermite(fromPose.position, toPose.position, fromTangent, toTangent, localT);
        position = ClampSmartSamplePosition(position, prevPose.position, fromPose.position, toPose.position, nextPose.position);

        float angle = EvaluateSmartAngle(prevPose.angleZ, fromPose.angleZ, toPose.angleZ, nextPose.angleZ, prevTime, fromTime, toTime, nextTime, localT);
        WeaponMotionEase ease = preferLinearEase ? WeaponMotionEase.Linear : ResolveSmartInsertedEase(to.ease);
        return new SmartMotionSample(position, angle, ease);
    }

    private void FindMotionSegment(float normalizedTime, out int fromIndex, out int toIndex)
    {
        IReadOnlyList<WeaponMotionKeyframe> frames = sequence.MotionKeyframes;
        fromIndex = 0;
        toIndex = Mathf.Min(1, frames.Count - 1);

        if (normalizedTime <= frames[0].normalizedTime)
        {
            return;
        }

        for (int i = 0; i < frames.Count - 1; i++)
        {
            if (normalizedTime <= frames[i + 1].normalizedTime)
            {
                fromIndex = i;
                toIndex = i + 1;
                return;
            }
        }

        fromIndex = Mathf.Max(0, frames.Count - 2);
        toIndex = frames.Count - 1;
    }

    private PreviewPose GetResolvedMotionFramePose(int index)
    {
        WeaponMotionKeyframe frame = sequence.MotionKeyframes[index];
        return new PreviewPose(ResolvePreviewPosition(frame, index), frame.localEulerAngles.z);
    }

    private Vector2 EstimatePositionTangent(Vector2 previous, Vector2 next, float previousTime, float nextTime, float segmentStart, float segmentEnd)
    {
        float timeSpan = Mathf.Max(0.0001f, nextTime - previousTime);
        float segmentSpan = Mathf.Max(0.0001f, segmentEnd - segmentStart);
        return (next - previous) * (segmentSpan / timeSpan);
    }

    private static Vector2 EvaluateHermite(Vector2 from, Vector2 to, Vector2 fromTangent, Vector2 toTangent, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        float h00 = 2f * t3 - 3f * t2 + 1f;
        float h10 = t3 - 2f * t2 + t;
        float h01 = -2f * t3 + 3f * t2;
        float h11 = t3 - t2;
        return h00 * from + h10 * fromTangent + h01 * to + h11 * toTangent;
    }

    private static float EvaluateHermite(float from, float to, float fromTangent, float toTangent, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        float h00 = 2f * t3 - 3f * t2 + 1f;
        float h10 = t3 - 2f * t2 + t;
        float h01 = -2f * t3 + 3f * t2;
        float h11 = t3 - t2;
        return h00 * from + h10 * fromTangent + h01 * to + h11 * toTangent;
    }

    private static Vector2 ClampSmartSamplePosition(Vector2 value, Vector2 previous, Vector2 from, Vector2 to, Vector2 next)
    {
        float minX = Mathf.Min(previous.x, from.x, to.x, next.x);
        float maxX = Mathf.Max(previous.x, from.x, to.x, next.x);
        float minY = Mathf.Min(previous.y, from.y, to.y, next.y);
        float maxY = Mathf.Max(previous.y, from.y, to.y, next.y);
        float marginX = Mathf.Max(0.05f, Mathf.Abs(to.x - from.x) * 0.35f);
        float marginY = Mathf.Max(0.05f, Mathf.Abs(to.y - from.y) * 0.35f);
        return new Vector2(
            Mathf.Clamp(value.x, minX - marginX, maxX + marginX),
            Mathf.Clamp(value.y, minY - marginY, maxY + marginY));
    }

    private float EvaluateSmartAngle(
        float previousAngle,
        float fromAngle,
        float toAngle,
        float nextAngle,
        float previousTime,
        float fromTime,
        float toTime,
        float nextTime,
        float localT)
    {
        float previous = UnwrapAngleNear(previousAngle, fromAngle);
        float from = fromAngle;
        float to = UnwrapAngleNear(toAngle, from);
        float next = UnwrapAngleNear(nextAngle, to);
        float fromTangent = EstimateScalarTangent(previous, to, previousTime, toTime, fromTime, toTime);
        float toTangent = EstimateScalarTangent(from, next, fromTime, nextTime, fromTime, toTime);
        return EvaluateHermite(from, to, fromTangent, toTangent, localT);
    }

    private static float EstimateScalarTangent(float previous, float next, float previousTime, float nextTime, float segmentStart, float segmentEnd)
    {
        float timeSpan = Mathf.Max(0.0001f, nextTime - previousTime);
        float segmentSpan = Mathf.Max(0.0001f, segmentEnd - segmentStart);
        return (next - previous) * (segmentSpan / timeSpan);
    }

    private static float UnwrapAngleNear(float angle, float reference)
    {
        return reference + Mathf.DeltaAngle(reference, angle);
    }

    private static WeaponMotionEase ResolveSmartInsertedEase(WeaponMotionEase segmentEase)
    {
        switch (segmentEase)
        {
            case WeaponMotionEase.OutBack:
            case WeaponMotionEase.OutElastic:
            case WeaponMotionEase.CustomCurve:
                return WeaponMotionEase.InOutSine;
            default:
                return WeaponMotionEase.Linear;
        }
    }

    private List<float> BuildSmartSampleTimesBetween(float startTime, float endTime, int sampleCount)
    {
        List<float> result = new List<float>(sampleCount);
        if (sampleCount <= 0 || endTime <= startTime + 0.0001f)
        {
            return result;
        }

        const int resolution = 36;
        float[] cumulativeLengths = new float[resolution + 1];
        SmartMotionSample previous = BuildSmartMotionSample(startTime, true);
        for (int i = 1; i <= resolution; i++)
        {
            float sampleTime = Mathf.Lerp(startTime, endTime, i / (float)resolution);
            SmartMotionSample current = BuildSmartMotionSample(sampleTime, true);
            float distance = Vector2.Distance(previous.position, current.position);
            distance += Mathf.Abs(Mathf.DeltaAngle(previous.angleZ, current.angleZ)) * 0.01f;
            cumulativeLengths[i] = cumulativeLengths[i - 1] + distance;
            previous = current;
        }

        float totalLength = cumulativeLengths[resolution];
        for (int i = 0; i < sampleCount; i++)
        {
            float fraction = (i + 1f) / (sampleCount + 1f);
            if (totalLength <= 0.0001f)
            {
                result.Add(Mathf.Lerp(startTime, endTime, SmootherStep(fraction)));
                continue;
            }

            float targetLength = totalLength * fraction;
            int upperIndex = 1;
            while (upperIndex < cumulativeLengths.Length - 1 && cumulativeLengths[upperIndex] < targetLength)
            {
                upperIndex++;
            }

            float lowerLength = cumulativeLengths[upperIndex - 1];
            float upperLength = cumulativeLengths[upperIndex];
            float segmentT = Mathf.InverseLerp(lowerLength, upperLength, targetLength);
            float lowerTime = Mathf.Lerp(startTime, endTime, (upperIndex - 1) / (float)resolution);
            float upperTime = Mathf.Lerp(startTime, endTime, upperIndex / (float)resolution);
            result.Add(Mathf.Lerp(lowerTime, upperTime, segmentT));
        }

        return result;
    }

    private static float SmootherStep(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * t * (t * (t * 6f - 15f) + 10f);
    }

    private void InsertMotionFrameAtTime(float normalizedTime, Vector2? localPosition, float? angleZ = null, WeaponMotionEase? preferredEase = null)
    {
        if (sequenceObject == null)
        {
            return;
        }

        SmartMotionSample sampledPose = BuildSmartMotionSample(normalizedTime, false);
        int insertIndex = FindMotionInsertIndex(normalizedTime);
        Vector2 posePosition = localPosition ?? sampledPose.position;
        float poseAngle = angleZ ?? sampledPose.angleZ;
        InsertMotionFrameAtIndex(insertIndex, normalizedTime, posePosition, poseAngle, preferredEase ?? sampledPose.ease);
    }

    private void InsertMotionFrameAfterSelection()
    {
        if (sequenceObject == null || selectedMotionIndex < 0)
        {
            return;
        }

        SerializedProperty frames = sequenceObject.FindProperty("motionKeyframes");
        SerializedProperty selectedFrame = frames.GetArrayElementAtIndex(selectedMotionIndex);
        float baseTime = selectedFrame.FindPropertyRelative("normalizedTime").floatValue;
        float insertTime = Mathf.Clamp01(baseTime + 0.05f);
        SmartMotionSample sampledPose = BuildSmartMotionSample(insertTime, false);
        InsertMotionFrameAtIndex(selectedMotionIndex + 1, insertTime, sampledPose.position, sampledPose.angleZ, sampledPose.ease);
    }

    private void InsertMotionFrameAtIndex(int insertIndex, float normalizedTime, Vector2 localPosition, float angleZ, WeaponMotionEase preferredEase)
    {
        Undo.RecordObject(sequence, "插入动作关键帧");
        sequenceObject.Update();
        SerializedProperty frames = sequenceObject.FindProperty("motionKeyframes");
        frames.InsertArrayElementAtIndex(insertIndex);
        SerializedProperty target = frames.GetArrayElementAtIndex(insertIndex);
        SerializedProperty source = insertIndex > 0 ? frames.GetArrayElementAtIndex(insertIndex - 1) : null;
        InitializeMotionKeyframe(target, source);
        target.FindPropertyRelative("normalizedTime").floatValue = Mathf.Clamp01(normalizedTime);
        target.FindPropertyRelative("ease").enumValueIndex = (int)preferredEase;
        if (preferredEase != WeaponMotionEase.CustomCurve)
        {
            target.FindPropertyRelative("customCurve").animationCurveValue = null;
        }

        ApplyPoseToMotionFrame(target, localPosition, angleZ);

        previewNormalizedTime = Mathf.Clamp01(normalizedTime);
        selectedMotionIndex = insertIndex;
        ApplyModifiedProperties();
        RebuildLists();
        if (motionList != null)
        {
            motionList.index = selectedMotionIndex;
        }
    }

    private void InsertInterpolatedMotionFramesForSelectedSegment(int extraFrameCount)
    {
        if (!CanInsertSmoothFramesAfterSelected() || extraFrameCount <= 0)
        {
            return;
        }

        SerializedProperty frames = sequenceObject.FindProperty("motionKeyframes");
        float startTime = frames.GetArrayElementAtIndex(selectedMotionIndex).FindPropertyRelative("normalizedTime").floatValue;
        float endTime = frames.GetArrayElementAtIndex(selectedMotionIndex + 1).FindPropertyRelative("normalizedTime").floatValue;
        InsertInterpolatedMotionFramesBetween(startTime, endTime, extraFrameCount);
    }

    private void InsertInterpolatedMotionFramesForAllSegments(int extraFrameCountPerSegment)
    {
        if (sequenceObject == null || extraFrameCountPerSegment <= 0)
        {
            return;
        }

        SerializedProperty frames = sequenceObject.FindProperty("motionKeyframes");
        if (frames == null || frames.arraySize < 2)
        {
            return;
        }

        List<float> baseTimes = new List<float>(frames.arraySize);
        for (int i = 0; i < frames.arraySize; i++)
        {
            baseTimes.Add(frames.GetArrayElementAtIndex(i).FindPropertyRelative("normalizedTime").floatValue);
        }

        List<float> sampleTimes = new List<float>();
        List<SmartMotionSample> samplePoses = new List<SmartMotionSample>();
        for (int i = 0; i < baseTimes.Count - 1; i++)
        {
            float startTime = baseTimes[i];
            float endTime = baseTimes[i + 1];
            if (endTime <= startTime + 0.0001f)
            {
                continue;
            }

            List<float> segmentTimes = BuildSmartSampleTimesBetween(startTime, endTime, extraFrameCountPerSegment);
            for (int j = 0; j < segmentTimes.Count; j++)
            {
                float sampleTime = segmentTimes[j];
                sampleTimes.Add(sampleTime);
                samplePoses.Add(BuildSmartMotionSample(sampleTime, true));
            }
        }

        for (int i = 0; i < sampleTimes.Count; i++)
        {
            InsertMotionFrameAtTime(sampleTimes[i], samplePoses[i].position, samplePoses[i].angleZ, samplePoses[i].ease);
        }
    }

    private void InsertInterpolatedMotionFramesBetween(float startTime, float endTime, int extraFrameCount)
    {
        if (extraFrameCount <= 0 || endTime <= startTime + 0.0001f)
        {
            return;
        }

        List<float> sampleTimes = BuildSmartSampleTimesBetween(startTime, endTime, extraFrameCount);
        List<SmartMotionSample> samplePoses = new List<SmartMotionSample>(sampleTimes.Count);
        for (int i = 0; i < sampleTimes.Count; i++)
        {
            samplePoses.Add(BuildSmartMotionSample(sampleTimes[i], true));
        }

        for (int i = 0; i < sampleTimes.Count; i++)
        {
            InsertMotionFrameAtTime(sampleTimes[i], samplePoses[i].position, samplePoses[i].angleZ, samplePoses[i].ease);
        }
    }

    private void InsertEventFrameAtTime(float normalizedTime)
    {
        if (sequenceObject == null)
        {
            return;
        }

        int insertIndex = FindEventInsertIndex(normalizedTime);
        InsertEventFrameAtIndex(insertIndex, normalizedTime);
    }

    private void InsertEventFrameAfterSelection()
    {
        if (sequenceObject == null || selectedEventIndex < 0)
        {
            return;
        }

        SerializedProperty frames = sequenceObject.FindProperty("eventKeyframes");
        SerializedProperty selectedFrame = frames.GetArrayElementAtIndex(selectedEventIndex);
        float baseTime = selectedFrame.FindPropertyRelative("normalizedTime").floatValue;
        InsertEventFrameAtIndex(selectedEventIndex + 1, Mathf.Clamp01(baseTime + 0.05f));
    }

    private void InsertEventFrameAtIndex(int insertIndex, float normalizedTime)
    {
        Undo.RecordObject(sequence, "插入事件关键帧");
        sequenceObject.Update();
        SerializedProperty frames = sequenceObject.FindProperty("eventKeyframes");
        frames.InsertArrayElementAtIndex(insertIndex);
        SerializedProperty target = frames.GetArrayElementAtIndex(insertIndex);
        target.FindPropertyRelative("normalizedTime").floatValue = Mathf.Clamp01(normalizedTime);
        target.FindPropertyRelative("eventType").enumValueIndex = (int)WeaponSequenceEventType.PlaySfx;
        target.FindPropertyRelative("eventKey").intValue = 0;
        previewNormalizedTime = Mathf.Clamp01(normalizedTime);
        selectedEventIndex = insertIndex;
        ApplyModifiedProperties();
        RebuildLists();
        if (eventList != null)
        {
            eventList.index = selectedEventIndex;
        }
    }

    private void DeleteSelectedMotionFrame()
    {
        if (sequenceObject == null || selectedMotionIndex < 0)
        {
            return;
        }

        Undo.RecordObject(sequence, "删除动作关键帧");
        sequenceObject.Update();
        SerializedProperty frames = sequenceObject.FindProperty("motionKeyframes");
        frames.DeleteArrayElementAtIndex(selectedMotionIndex);
        selectedMotionIndex = Mathf.Clamp(selectedMotionIndex - 1, 0, Mathf.Max(0, frames.arraySize - 1));
        ApplyModifiedProperties();
        RebuildLists();
    }

    private void DeleteSelectedEventFrame()
    {
        if (sequenceObject == null || selectedEventIndex < 0)
        {
            return;
        }

        Undo.RecordObject(sequence, "删除事件关键帧");
        sequenceObject.Update();
        SerializedProperty frames = sequenceObject.FindProperty("eventKeyframes");
        frames.DeleteArrayElementAtIndex(selectedEventIndex);
        selectedEventIndex = Mathf.Clamp(selectedEventIndex - 1, 0, Mathf.Max(0, frames.arraySize - 1));
        ApplyModifiedProperties();
        RebuildLists();
    }

    private int FindMotionInsertIndex(float normalizedTime)
    {
        SerializedProperty frames = sequenceObject.FindProperty("motionKeyframes");
        for (int i = 0; i < frames.arraySize; i++)
        {
            if (frames.GetArrayElementAtIndex(i).FindPropertyRelative("normalizedTime").floatValue > normalizedTime)
            {
                return i;
            }
        }

        return frames.arraySize;
    }

    private int FindEventInsertIndex(float normalizedTime)
    {
        SerializedProperty frames = sequenceObject.FindProperty("eventKeyframes");
        for (int i = 0; i < frames.arraySize; i++)
        {
            if (frames.GetArrayElementAtIndex(i).FindPropertyRelative("normalizedTime").floatValue > normalizedTime)
            {
                return i;
            }
        }

        return frames.arraySize;
    }

    private void SetMotionFrameTime(int index, float normalizedTime)
    {
        if (sequenceObject == null || index < 0)
        {
            return;
        }

        SerializedProperty frames = sequenceObject.FindProperty("motionKeyframes");
        if (index >= frames.arraySize)
        {
            return;
        }

        float minTime = index > 0
            ? frames.GetArrayElementAtIndex(index - 1).FindPropertyRelative("normalizedTime").floatValue + 0.001f
            : 0f;
        float maxTime = index < frames.arraySize - 1
            ? frames.GetArrayElementAtIndex(index + 1).FindPropertyRelative("normalizedTime").floatValue - 0.001f
            : 1f;
        frames.GetArrayElementAtIndex(index).FindPropertyRelative("normalizedTime").floatValue = Mathf.Clamp(normalizedTime, minTime, maxTime);
    }

    private void SetEventFrameTime(int index, float normalizedTime)
    {
        if (sequenceObject == null || index < 0)
        {
            return;
        }

        SerializedProperty frames = sequenceObject.FindProperty("eventKeyframes");
        if (index >= frames.arraySize)
        {
            return;
        }

        float minTime = index > 0
            ? frames.GetArrayElementAtIndex(index - 1).FindPropertyRelative("normalizedTime").floatValue + 0.001f
            : 0f;
        float maxTime = index < frames.arraySize - 1
            ? frames.GetArrayElementAtIndex(index + 1).FindPropertyRelative("normalizedTime").floatValue - 0.001f
            : 1f;
        frames.GetArrayElementAtIndex(index).FindPropertyRelative("normalizedTime").floatValue = Mathf.Clamp(normalizedTime, minTime, maxTime);
    }

    private Vector2 GetMotionFrameEditablePosition(int index)
    {
        SerializedProperty frames = sequenceObject.FindProperty("motionKeyframes");
        if (frames == null || index < 0 || index >= frames.arraySize)
        {
            return Vector2.zero;
        }

        SerializedProperty frame = frames.GetArrayElementAtIndex(index);
        return ResolveSerializedMotionFramePosition(frame);
    }

    private void SetMotionFramePosition(int index, Vector2 position)
    {
        if (sequenceObject == null)
        {
            return;
        }

        SerializedProperty frames = sequenceObject.FindProperty("motionKeyframes");
        if (frames == null || index < 0 || index >= frames.arraySize)
        {
            return;
        }

        SerializedProperty frame = frames.GetArrayElementAtIndex(index);
        SetSerializedMotionPosition(frame, position);
    }

    private Vector2 CanvasToWorld(Vector2 canvasPosition, Rect rect)
    {
        Vector2 origin = new Vector2(rect.center.x, rect.yMax - rect.height * 0.35f);
        return new Vector2(
            (canvasPosition.x - origin.x) / Mathf.Max(1f, previewPixelsPerUnit),
            (origin.y - canvasPosition.y) / Mathf.Max(1f, previewPixelsPerUnit));
    }

    private void DrawCurveGrid(Rect rect)
    {
        Handles.BeginGUI();
        Handles.color = new Color(1f, 1f, 1f, 0.06f);
        for (int i = 0; i <= 10; i++)
        {
            float x = Mathf.Lerp(rect.xMin, rect.xMax, i / 10f);
            Handles.DrawLine(new Vector2(x, rect.yMin), new Vector2(x, rect.yMax));
        }

        for (int i = 0; i <= 4; i++)
        {
            float y = Mathf.Lerp(rect.yMin, rect.yMax, i / 4f);
            Handles.DrawLine(new Vector2(rect.xMin, y), new Vector2(rect.xMax, y));
        }
        Handles.EndGUI();
    }

    private void DrawCurveLine(Rect rect)
    {
        if (sequence == null || sequence.MotionKeyframes == null || sequence.MotionKeyframes.Count == 0)
        {
            return;
        }

        float minValue;
        float maxValue;
        GetCurveValueRange(out minValue, out maxValue);

        const int sampleCount = 96;
        Vector3[] points = new Vector3[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (sampleCount - 1f);
            float value = EvaluateCurveChannelAtTime(t);
            points[i] = CurveToCanvas(rect, t, value, minValue, maxValue);
        }

        Handles.BeginGUI();
        Handles.color = new Color(0.35f, 1f, 0.65f, 0.95f);
        Handles.DrawAAPolyLine(2f, points);
        Handles.EndGUI();
    }

    private void DrawCurveMarkers(Rect rect)
    {
        if (sequence == null || sequence.MotionKeyframes == null)
        {
            return;
        }

        float minValue;
        float maxValue;
        GetCurveValueRange(out minValue, out maxValue);

        Handles.BeginGUI();
        for (int i = 0; i < sequence.MotionKeyframes.Count; i++)
        {
            WeaponMotionKeyframe frame = sequence.MotionKeyframes[i];
            float value = EvaluateCurveChannelValue(frame);
            Vector3 point = CurveToCanvas(rect, frame.normalizedTime, value, minValue, maxValue);
            bool isActive = i == selectedMotionIndex || i == curveDragMotionIndex;
            Handles.color = isActive ? Color.yellow : new Color(0.75f, 1f, 0.9f, 1f);
            Handles.DrawSolidDisc(point, Vector3.forward, isActive ? 4.5f : 3f);
        }
        Handles.EndGUI();
    }

    private void HandleCurvePanelInput(Rect rect)
    {
        if (sequenceObject == null || sequence == null || sequence.MotionKeyframes == null)
        {
            return;
        }

        Event current = Event.current;
        int controlId = GUIUtility.GetControlID(FocusType.Passive, rect);

        GetCurveValueRange(out float minValue, out float maxValue);
        int hitIndex = FindMotionMarkerAtCurve(rect, current.mousePosition, minValue, maxValue);
        if (hitIndex >= 0)
        {
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.MoveArrow);
        }

        if (current.type == EventType.ContextClick && rect.Contains(current.mousePosition))
        {
            ShowCurveContextMenu(rect, current.mousePosition);
            current.Use();
            return;
        }

        if (current.type == EventType.MouseDown && current.button == 0 && rect.Contains(current.mousePosition) && hitIndex >= 0)
        {
            previewPlaying = false;
            Undo.RecordObject(sequence, "拖拽曲线关键帧");
            selectedMotionIndex = hitIndex;
            curveDragMotionIndex = hitIndex;
            float padding = Mathf.Max(0.1f, (maxValue - minValue) * 0.15f);
            curveDragMinValue = minValue - padding;
            curveDragMaxValue = maxValue + padding;
            if (motionList != null)
            {
                motionList.index = hitIndex;
            }

            GUIUtility.hotControl = controlId;
            current.Use();
            return;
        }

        if (current.type == EventType.MouseDrag && GUIUtility.hotControl == controlId && curveDragMotionIndex >= 0)
        {
            float normalizedTime = NormalizeTimelinePosition(rect, current.mousePosition.x);
            float value = CurveCanvasToValue(rect, current.mousePosition.y, curveDragMinValue, curveDragMaxValue);
            SetMotionFrameTime(curveDragMotionIndex, normalizedTime);
            SetMotionFrameCurveValue(curveDragMotionIndex, value);
            ApplyModifiedProperties();
            previewNormalizedTime = normalizedTime;
            Repaint();
            current.Use();
            return;
        }

        if (current.type == EventType.MouseUp && GUIUtility.hotControl == controlId)
        {
            GUIUtility.hotControl = 0;
            curveDragMotionIndex = -1;
            current.Use();
        }
    }

    private int FindMotionMarkerAtCurve(Rect rect, Vector2 mousePosition, float minValue, float maxValue)
    {
        if (sequence == null || sequence.MotionKeyframes == null)
        {
            return -1;
        }

        for (int i = 0; i < sequence.MotionKeyframes.Count; i++)
        {
            WeaponMotionKeyframe frame = sequence.MotionKeyframes[i];
            float value = EvaluateCurveChannelValue(frame);
            Vector3 point = CurveToCanvas(rect, frame.normalizedTime, value, minValue, maxValue);
            if (Vector2.Distance(point, mousePosition) <= 9f)
            {
                return i;
            }
        }

        return -1;
    }

    private void ShowCurveContextMenu(Rect rect, Vector2 mousePosition)
    {
        float normalizedTime = NormalizeTimelinePosition(rect, mousePosition.x);
        GenericMenu menu = new GenericMenu();
        menu.AddItem(new GUIContent("在此时间插入/动作帧"), false, () => InsertMotionFrameAtTime(normalizedTime, null));
        AddMotionGraphEditMenuItems(menu, null);

        menu.ShowAsContext();
    }

    private void GetCurveValueRange(out float minValue, out float maxValue)
    {
        minValue = float.MaxValue;
        maxValue = float.MinValue;

        for (int i = 0; i < sequence.MotionKeyframes.Count; i++)
        {
            float value = EvaluateCurveChannelValue(sequence.MotionKeyframes[i]);
            minValue = Mathf.Min(minValue, value);
            maxValue = Mathf.Max(maxValue, value);
        }

        if (Mathf.Approximately(minValue, maxValue))
        {
            minValue -= 1f;
            maxValue += 1f;
        }
    }

    private Vector3 CurveToCanvas(Rect rect, float normalizedTime, float value, float minValue, float maxValue)
    {
        float x = Mathf.Lerp(rect.xMin, rect.xMax, normalizedTime);
        float y = Mathf.Lerp(rect.yMax, rect.yMin, Mathf.InverseLerp(minValue, maxValue, value));
        return new Vector3(x, y, 0f);
    }

    private float CurveCanvasToValue(Rect rect, float mouseY, float minValue, float maxValue)
    {
        float normalized = Mathf.InverseLerp(rect.yMax, rect.yMin, Mathf.Clamp(mouseY, rect.yMin, rect.yMax));
        return Mathf.Lerp(minValue, maxValue, normalized);
    }

    private float EvaluateCurveChannelAtTime(float normalizedTime)
    {
        PreviewPose pose = SamplePose(normalizedTime);
        switch (curveChannel)
        {
            case PreviewCurveChannel.PositionX:
                return pose.position.x;
            case PreviewCurveChannel.RotationZ:
                return pose.angleZ;
            default:
                return pose.position.y;
        }
    }

    private float EvaluateCurveChannelValue(WeaponMotionKeyframe frame)
    {
        switch (curveChannel)
        {
            case PreviewCurveChannel.PositionX:
                return ResolveCurveAxisValue(frame, MotionAxis.X);
            case PreviewCurveChannel.RotationZ:
                return frame.localEulerAngles.z;
            default:
                return ResolveCurveAxisValue(frame, MotionAxis.Y);
        }
    }

    private void DrawMotionListElement(Rect rect, int index, bool isActive, bool isFocused)
    {
        SerializedProperty element = motionList.serializedProperty.GetArrayElementAtIndex(index);
        SerializedProperty time = element.FindPropertyRelative("normalizedTime");
        SerializedProperty xMode = element.FindPropertyRelative("xPositionMode");
        SerializedProperty yMode = element.FindPropertyRelative("yPositionMode");
        SerializedProperty x = element.FindPropertyRelative("localPositionX");
        SerializedProperty y = element.FindPropertyRelative("localPositionY");
        SerializedProperty euler = element.FindPropertyRelative("localEulerAngles");
        SerializedProperty ease = element.FindPropertyRelative("ease");

        rect.y += 2f;
        rect.height = EditorGUIUtility.singleLineHeight;

        Rect indexRect = new Rect(rect.x, rect.y, 28f, rect.height);
        Rect timeRect = new Rect(indexRect.xMax + 4f, rect.y, 48f, rect.height);
        Rect summaryRect = new Rect(timeRect.xMax + 8f, rect.y, rect.width - 88f, rect.height);

        EditorGUI.LabelField(indexRect, index.ToString("00"));
        time.floatValue = EditorGUI.FloatField(timeRect, Mathf.Clamp01(time.floatValue));

        string xText = BuildMotionAxisSummary(element, MotionAxis.X, xMode, x);
        string yText = BuildMotionAxisSummary(element, MotionAxis.Y, yMode, y);
        string summary = "X " + xText + "  Y " + yText + "  Z " + euler.vector3Value.z.ToString("0.#") + "  " + MotionEaseLabels[ease.enumValueIndex];
        EditorGUI.LabelField(summaryRect, summary);
    }

    private void DrawEventListElement(Rect rect, int index, bool isActive, bool isFocused)
    {
        SerializedProperty element = eventList.serializedProperty.GetArrayElementAtIndex(index);
        SerializedProperty time = element.FindPropertyRelative("normalizedTime");
        SerializedProperty type = element.FindPropertyRelative("eventType");
        SerializedProperty key = element.FindPropertyRelative("eventKey");

        rect.y += 2f;
        rect.height = EditorGUIUtility.singleLineHeight;

        Rect indexRect = new Rect(rect.x, rect.y, 28f, rect.height);
        Rect timeRect = new Rect(indexRect.xMax + 4f, rect.y, 48f, rect.height);
        Rect typeRect = new Rect(timeRect.xMax + 8f, rect.y, rect.width - 150f, rect.height);
        Rect keyRect = new Rect(typeRect.xMax + 8f, rect.y, 54f, rect.height);

        EditorGUI.LabelField(indexRect, index.ToString("00"));
        time.floatValue = EditorGUI.FloatField(timeRect, Mathf.Clamp01(time.floatValue));
        type.enumValueIndex = EditorGUI.Popup(typeRect, type.enumValueIndex, EventTypeLabels);
        key.intValue = Mathf.Max(0, EditorGUI.IntField(keyRect, key.intValue));
    }

    private void OnAddMotionFrame(ReorderableList list)
    {
        SerializedProperty frames = list.serializedProperty;
        int newIndex = frames.arraySize;
        frames.InsertArrayElementAtIndex(newIndex);
        SerializedProperty newElement = frames.GetArrayElementAtIndex(newIndex);
        SerializedProperty source = newIndex > 0 ? frames.GetArrayElementAtIndex(newIndex - 1) : null;
        InitializeMotionKeyframe(newElement, source);
        newElement.FindPropertyRelative("normalizedTime").floatValue = previewNormalizedTime;
        PreviewPose sampledPose = SamplePose(previewNormalizedTime);
        ApplyPoseToMotionFrame(newElement, sampledPose.position, sampledPose.angleZ);
        selectedMotionIndex = newIndex;
        list.index = newIndex;
    }

    private void OnAddEventFrame(ReorderableList list)
    {
        SerializedProperty frames = list.serializedProperty;
        int newIndex = frames.arraySize;
        frames.InsertArrayElementAtIndex(newIndex);
        SerializedProperty newElement = frames.GetArrayElementAtIndex(newIndex);
        newElement.FindPropertyRelative("normalizedTime").floatValue = previewNormalizedTime;
        newElement.FindPropertyRelative("eventType").enumValueIndex = (int)WeaponSequenceEventType.PlaySfx;
        newElement.FindPropertyRelative("eventKey").intValue = 0;
        selectedEventIndex = newIndex;
        list.index = newIndex;
    }

    private void DuplicateMotionFrame(int index)
    {
        Undo.RecordObject(sequence, "复制动作关键帧");
        SerializedProperty frames = sequenceObject.FindProperty("motionKeyframes");
        int newIndex = Mathf.Clamp(index + 1, 0, frames.arraySize);
        frames.InsertArrayElementAtIndex(newIndex);
        SerializedProperty source = frames.GetArrayElementAtIndex(index);
        SerializedProperty destination = frames.GetArrayElementAtIndex(newIndex);
        CopyMotionKeyframe(source, destination);
        destination.FindPropertyRelative("normalizedTime").floatValue = Mathf.Clamp01(source.FindPropertyRelative("normalizedTime").floatValue + 0.025f);
        selectedMotionIndex = newIndex;
        ApplyModifiedProperties();
        RebuildLists();
        if (motionList != null)
        {
            motionList.index = newIndex;
        }
    }

    private void DuplicateEventFrame(int index)
    {
        Undo.RecordObject(sequence, "复制事件关键帧");
        SerializedProperty frames = sequenceObject.FindProperty("eventKeyframes");
        int newIndex = Mathf.Clamp(index + 1, 0, frames.arraySize);
        frames.InsertArrayElementAtIndex(newIndex);
        SerializedProperty source = frames.GetArrayElementAtIndex(index);
        SerializedProperty destination = frames.GetArrayElementAtIndex(newIndex);
        destination.FindPropertyRelative("normalizedTime").floatValue = Mathf.Clamp01(source.FindPropertyRelative("normalizedTime").floatValue + 0.025f);
        destination.FindPropertyRelative("eventType").enumValueIndex = source.FindPropertyRelative("eventType").enumValueIndex;
        destination.FindPropertyRelative("eventKey").intValue = source.FindPropertyRelative("eventKey").intValue;
        selectedEventIndex = newIndex;
        ApplyModifiedProperties();
        RebuildLists();
        if (eventList != null)
        {
            eventList.index = newIndex;
        }
    }

    private void DrawAxisValues(string label, SerializedProperty axisMode, SerializedProperty localPosition,
        SerializedProperty minReach, SerializedProperty maxReach, SerializedProperty dynamicPositionStrategy)
    {
        EditorGUILayout.Space(3f);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

        WeaponMotionPositionMode mode = (WeaponMotionPositionMode)axisMode.enumValueIndex;
        if (mode == WeaponMotionPositionMode.Fixed)
        {
            EditorGUILayout.PropertyField(localPosition, new GUIContent("局部坐标"));
        }
        else
        {
            EditorGUILayout.Slider(minReach, 0f, 1f, new GUIContent("最小归一化距离"));
            EditorGUILayout.Slider(maxReach, 0f, 1f, new GUIContent("最大归一化距离"));
            float previewValue = ResolveDynamicAxis(
                localPosition.floatValue,
                minReach.floatValue,
                maxReach.floatValue,
                (WeaponMotionDynamicPositionStrategy)dynamicPositionStrategy.enumValueIndex);
            EditorGUILayout.LabelField("当前预览值", previewValue.ToString("0.###"));
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawEventWiringHint(SerializedProperty element)
    {
        if (weaponObject == null)
        {
            EditorGUILayout.HelpBox("载入 WeaponDataSO 后可以在这里检查事件键值和武器配置的映射。", MessageType.None);
            return;
        }

        WeaponSequenceEventType eventType = (WeaponSequenceEventType)element.FindPropertyRelative("eventType").enumValueIndex;
        int key = Mathf.Max(0, element.FindPropertyRelative("eventKey").intValue);
        string propertyName = null;

        if (eventType == WeaponSequenceEventType.SpawnProjectile)
        {
            propertyName = "sequenceProjectileList";
        }
        else if (eventType == WeaponSequenceEventType.PlaySfx)
        {
            propertyName = "sequenceSfxList";
        }
        else if (eventType == WeaponSequenceEventType.PlayVfx)
        {
            propertyName = "sequenceVfxList";
        }

        if (propertyName == null)
        {
            EditorGUILayout.HelpBox("这个事件类型不需要额外的 WeaponDataSO 数组映射。", MessageType.None);
            return;
        }

        SerializedProperty list = weaponObject.FindProperty(propertyName);
        int count = list != null ? list.arraySize : 0;
        string displayName = GetLocalizedPropertyLabel(propertyName);
        if (key >= count)
        {
            EditorGUILayout.HelpBox("事件键值 " + key + " 超出了“" + displayName + "”的长度 " + count + "。", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox("事件键值 " + key + " 对应“" + displayName + "”的第 " + key + " 项。", MessageType.None);
        }
    }

    private void DrawSelectedMotionInspectorStatus(SerializedProperty element)
    {
        SerializedProperty normalizedTime = element.FindPropertyRelative("normalizedTime");
        Vector2 resolvedPosition = ResolveSerializedMotionFramePosition(element);
        Vector3 localEulerAngles = element.FindPropertyRelative("localEulerAngles").vector3Value;

        EditorGUILayout.Space(4f);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("当前解算", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("实际时间", (sequence.Duration * normalizedTime.floatValue).ToString("0.###") + " 秒");
        EditorGUILayout.LabelField("预览位置", "X " + resolvedPosition.x.ToString("0.###") + " / Y " + resolvedPosition.y.ToString("0.###"));
        EditorGUILayout.LabelField("预览旋转", "Z " + localEulerAngles.z.ToString("0.###") + "°");
        EditorGUILayout.LabelField("目标距离", previewTargetDistance.ToString("0.###") + " / 范围 " + previewAttackRange.ToString("0.###"));
        EditorGUILayout.EndVertical();
    }

    private bool CanInsertSmoothFramesAfterSelected()
    {
        SerializedProperty frames = sequenceObject != null ? sequenceObject.FindProperty("motionKeyframes") : null;
        return frames != null && selectedMotionIndex >= 0 && selectedMotionIndex < frames.arraySize - 1;
    }

    private static float GetAxisPreferredSign(float configuredValue)
    {
        return configuredValue < 0f ? -1f : 1f;
    }

    private static void GetMotionAxisProperties(
        SerializedProperty frame,
        MotionAxis axis,
        out SerializedProperty modeProperty,
        out SerializedProperty localPositionProperty,
        out SerializedProperty minReachProperty,
        out SerializedProperty maxReachProperty)
    {
        if (axis == MotionAxis.X)
        {
            modeProperty = frame.FindPropertyRelative("xPositionMode");
            localPositionProperty = frame.FindPropertyRelative("localPositionX");
            minReachProperty = frame.FindPropertyRelative("xDynamicMinNormalizedReach");
            maxReachProperty = frame.FindPropertyRelative("xDynamicMaxNormalizedReach");
            return;
        }

        modeProperty = frame.FindPropertyRelative("yPositionMode");
        localPositionProperty = frame.FindPropertyRelative("localPositionY");
        minReachProperty = frame.FindPropertyRelative("yDynamicMinNormalizedReach");
        maxReachProperty = frame.FindPropertyRelative("yDynamicMaxNormalizedReach");
    }

    private float ResolveSerializedMotionAxisValue(SerializedProperty frame, MotionAxis axis)
    {
        GetMotionAxisProperties(frame, axis, out SerializedProperty modeProperty, out SerializedProperty localPositionProperty,
            out SerializedProperty minReachProperty, out SerializedProperty maxReachProperty);

        WeaponMotionPositionMode mode = (WeaponMotionPositionMode)modeProperty.enumValueIndex;
        if (mode != WeaponMotionPositionMode.DynamicFromTarget)
        {
            return localPositionProperty.floatValue;
        }

        SerializedProperty strategyProperty = frame.FindPropertyRelative("dynamicPositionStrategy");
        return ResolveDynamicAxis(
            localPositionProperty.floatValue,
            minReachProperty.floatValue,
            maxReachProperty.floatValue,
            (WeaponMotionDynamicPositionStrategy)strategyProperty.enumValueIndex);
    }

    private Vector2 ResolveSerializedMotionFramePosition(SerializedProperty frame)
    {
        return new Vector2(
            ResolveSerializedMotionAxisValue(frame, MotionAxis.X),
            ResolveSerializedMotionAxisValue(frame, MotionAxis.Y));
    }

    private void ConvertMotionAxisModePreservingPose(SerializedProperty frame, MotionAxis axis, float resolvedValue)
    {
        GetMotionAxisProperties(frame, axis, out SerializedProperty modeProperty, out _, out _, out _);
        if ((WeaponMotionPositionMode)modeProperty.enumValueIndex == WeaponMotionPositionMode.DynamicFromTarget)
        {
            SerializedProperty strategyProperty = frame.FindPropertyRelative("dynamicPositionStrategy");
            if ((WeaponMotionDynamicPositionStrategy)strategyProperty.enumValueIndex == WeaponMotionDynamicPositionStrategy.None)
            {
                strategyProperty.enumValueIndex = (int)WeaponMotionDynamicPositionStrategy.TowardTargetClampedRadius;
            }
        }

        SetSerializedMotionAxisResolvedValue(frame, axis, resolvedValue);
    }

    private void SetSerializedMotionPosition(SerializedProperty frame, Vector2 position)
    {
        SetSerializedMotionAxisResolvedValue(frame, MotionAxis.X, position.x);
        SetSerializedMotionAxisResolvedValue(frame, MotionAxis.Y, position.y);
    }

    private void SetSerializedMotionAxisResolvedValue(SerializedProperty frame, MotionAxis axis, float resolvedValue)
    {
        GetMotionAxisProperties(frame, axis, out SerializedProperty modeProperty, out SerializedProperty localPositionProperty,
            out SerializedProperty minReachProperty, out SerializedProperty maxReachProperty);

        WeaponMotionPositionMode mode = (WeaponMotionPositionMode)modeProperty.enumValueIndex;
        if (mode == WeaponMotionPositionMode.Fixed)
        {
            localPositionProperty.floatValue = resolvedValue;
            return;
        }

        float range = Mathf.Max(0.1f, previewAttackRange);
        float normalizedDesired = Mathf.Clamp01(Mathf.Abs(resolvedValue) / range);
        float sign = Mathf.Approximately(resolvedValue, 0f) ? GetAxisPreferredSign(localPositionProperty.floatValue) : Mathf.Sign(resolvedValue);
        localPositionProperty.floatValue = sign * normalizedDesired;

        SerializedProperty strategyProperty = frame.FindPropertyRelative("dynamicPositionStrategy");
        if ((WeaponMotionDynamicPositionStrategy)strategyProperty.enumValueIndex != WeaponMotionDynamicPositionStrategy.TowardTargetClampedRadius)
        {
            return;
        }

        float normalizedTargetDistance = Mathf.Clamp01(previewTargetDistance / range);
        float existingMin = Mathf.Clamp01(minReachProperty.floatValue);
        float existingMax = Mathf.Clamp(maxReachProperty.floatValue, existingMin, 1f);

        if (Mathf.Approximately(normalizedDesired, normalizedTargetDistance))
        {
            if (existingMin > normalizedDesired || existingMax < normalizedDesired)
            {
                minReachProperty.floatValue = 0f;
                maxReachProperty.floatValue = 1f;
            }
        }
        else if (normalizedDesired < normalizedTargetDistance)
        {
            minReachProperty.floatValue = Mathf.Min(existingMin, normalizedDesired);
            maxReachProperty.floatValue = normalizedDesired;
        }
        else
        {
            minReachProperty.floatValue = normalizedDesired;
            maxReachProperty.floatValue = Mathf.Max(existingMax, normalizedDesired);
        }

        minReachProperty.floatValue = Mathf.Clamp01(minReachProperty.floatValue);
        maxReachProperty.floatValue = Mathf.Clamp(maxReachProperty.floatValue, minReachProperty.floatValue, 1f);
    }

    private void ApplyPoseToMotionFrame(SerializedProperty frame, Vector2 position, float angleZ)
    {
        SetSerializedMotionPosition(frame, position);
        SerializedProperty localEulerAngles = frame.FindPropertyRelative("localEulerAngles");
        Vector3 value = localEulerAngles.vector3Value;
        value.z = angleZ;
        localEulerAngles.vector3Value = value;
    }

    private float ResolveCurveAxisValue(WeaponMotionKeyframe frame, MotionAxis axis)
    {
        if (axis == MotionAxis.X)
        {
            return frame.xPositionMode == WeaponMotionPositionMode.DynamicFromTarget
                ? ResolveDynamicAxis(frame.localPositionX, frame.xDynamicMinNormalizedReach, frame.xDynamicMaxNormalizedReach, frame.dynamicPositionStrategy)
                : frame.localPositionX;
        }

        return frame.yPositionMode == WeaponMotionPositionMode.DynamicFromTarget
            ? ResolveDynamicAxis(frame.localPositionY, frame.yDynamicMinNormalizedReach, frame.yDynamicMaxNormalizedReach, frame.dynamicPositionStrategy)
            : frame.localPositionY;
    }

    private void SetMotionFrameCurveValue(int index, float value)
    {
        if (sequenceObject == null)
        {
            return;
        }

        SerializedProperty frames = sequenceObject.FindProperty("motionKeyframes");
        if (frames == null || index < 0 || index >= frames.arraySize)
        {
            return;
        }

        SerializedProperty frame = frames.GetArrayElementAtIndex(index);
        switch (curveChannel)
        {
            case PreviewCurveChannel.PositionX:
                SetSerializedMotionAxisResolvedValue(frame, MotionAxis.X, value);
                break;
            case PreviewCurveChannel.PositionY:
                SetSerializedMotionAxisResolvedValue(frame, MotionAxis.Y, value);
                break;
            case PreviewCurveChannel.RotationZ:
                SerializedProperty localEulerAngles = frame.FindPropertyRelative("localEulerAngles");
                Vector3 euler = localEulerAngles.vector3Value;
                euler.z = value;
                localEulerAngles.vector3Value = euler;
                break;
        }
    }

    private string BuildMotionAxisSummary(SerializedProperty element, MotionAxis axis, SerializedProperty modeProperty, SerializedProperty localPositionProperty)
    {
        if ((WeaponMotionPositionMode)modeProperty.enumValueIndex == WeaponMotionPositionMode.Fixed)
        {
            return localPositionProperty.floatValue.ToString("0.##");
        }

        return "动 " + ResolveSerializedMotionAxisValue(element, axis).ToString("0.##");
    }

    private void AssignTargets(AttackSequenceDefinitionSO targetSequence, WeaponDataSO targetWeapon)
    {
        sequence = targetSequence;
        weaponData = targetWeapon;
        if (sequence == null && weaponData != null)
        {
            sequence = weaponData.AttackSequence;
        }

        RebuildSerializedObjects();
        previewAttackRange = ResolvePreviewAttackRange();
        previewTargetDistance = Mathf.Clamp(previewTargetDistance, 0f, Mathf.Max(0.1f, previewAttackRange));
        Repaint();
    }

    private void TryLoadTargetsFromSelection(bool replaceExisting)
    {
        Object activeObject = Selection.activeObject;
        AttackSequenceDefinitionSO foundSequence = null;
        WeaponDataSO foundWeapon = null;

        if (activeObject is AttackSequenceDefinitionSO activeSequence)
        {
            foundSequence = activeSequence;
        }
        else if (activeObject is WeaponDataSO activeWeapon)
        {
            foundWeapon = activeWeapon;
            foundSequence = activeWeapon.AttackSequence;
        }
        else if (activeObject is GameObject gameObject)
        {
            Weapon weapon = gameObject.GetComponentInParent<Weapon>();
            if (weapon != null)
            {
                foundWeapon = weapon.WeaponData;
                foundSequence = foundWeapon != null ? foundWeapon.AttackSequence : null;
            }
        }

        if (!replaceExisting)
        {
            foundSequence = foundSequence != null ? foundSequence : sequence;
            foundWeapon = foundWeapon != null ? foundWeapon : weaponData;
        }

        AssignTargets(foundSequence, foundWeapon);
    }

    private void RebuildSerializedObjects()
    {
        sequenceObject = sequence != null ? new SerializedObject(sequence) : null;
        weaponObject = weaponData != null ? new SerializedObject(weaponData) : null;
        RebuildLists();
    }

    private void EnsureSerializedObjects()
    {
        bool needsRebuild = false;
        if (sequence != null && (sequenceObject == null || sequenceObject.targetObject != sequence))
        {
            needsRebuild = true;
        }

        if (sequence == null && sequenceObject != null)
        {
            needsRebuild = true;
        }

        if (weaponData != null && (weaponObject == null || weaponObject.targetObject != weaponData))
        {
            needsRebuild = true;
        }

        if (weaponData == null && weaponObject != null)
        {
            needsRebuild = true;
        }

        if (needsRebuild)
        {
            RebuildSerializedObjects();
        }
    }

    private void EnsureListsReady()
    {
        if (sequenceObject != null && (motionList == null || eventList == null))
        {
            RebuildLists();
        }
    }

    private void RebuildLists()
    {
        motionList = null;
        eventList = null;

        if (sequenceObject == null)
        {
            return;
        }

        SerializedProperty motionFrames = sequenceObject.FindProperty("motionKeyframes");
        if (motionFrames != null)
        {
            motionList = new ReorderableList(sequenceObject, motionFrames, true, true, true, true);
            motionList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "动作关键帧列表");
            motionList.drawElementCallback = DrawMotionListElement;
            motionList.elementHeightCallback = _ => EditorGUIUtility.singleLineHeight + 6f;
            motionList.onAddCallback = OnAddMotionFrame;
            motionList.onSelectCallback = list => selectedMotionIndex = list.index;
            motionList.onReorderCallback = list => selectedMotionIndex = list.index;
        }

        SerializedProperty eventFrames = sequenceObject.FindProperty("eventKeyframes");
        if (eventFrames != null)
        {
            eventList = new ReorderableList(sequenceObject, eventFrames, true, true, true, true);
            eventList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "事件关键帧列表");
            eventList.drawElementCallback = DrawEventListElement;
            eventList.elementHeightCallback = _ => EditorGUIUtility.singleLineHeight + 6f;
            eventList.onAddCallback = OnAddEventFrame;
            eventList.onSelectCallback = list => selectedEventIndex = list.index;
            eventList.onReorderCallback = list => selectedEventIndex = list.index;
        }
    }

    private void ApplyModifiedProperties()
    {
        if (sequenceObject != null && sequenceObject.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(sequence);
        }

        if (weaponObject != null && weaponObject.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(weaponData);
        }
    }

    private void AssignSequenceToWeapon()
    {
        if (weaponData == null || sequence == null)
        {
            return;
        }

        SerializedObject serializedWeapon = new SerializedObject(weaponData);
        SerializedProperty attackSequence = serializedWeapon.FindProperty("attackSequence");
        if (attackSequence == null)
        {
            return;
        }

        Undo.RecordObject(weaponData, "Assign Attack Sequence To Weapon");
        attackSequence.objectReferenceValue = sequence;
        serializedWeapon.ApplyModifiedProperties();
        EditorUtility.SetDirty(weaponData);
        RebuildSerializedObjects();
    }

    private void ApplySelectedPreset()
    {
        if (sequence == null)
        {
            return;
        }

        ApplyModifiedProperties();
        Undo.RecordObject(sequence, "Apply Attack Sequence Preset");
        WeaponAnimationSequencePresets.ApplyPreset(sequence, selectedPreset);
        EditorUtility.SetDirty(sequence);
        RebuildSerializedObjects();
    }

    private void SortSequenceKeyframes()
    {
        if (sequence == null)
        {
            return;
        }

        ApplyModifiedProperties();
        Undo.RecordObject(sequence, "Sort Attack Sequence Keyframes");
        List<WeaponMotionKeyframe> motions = CopyMotions(sequence.MotionKeyframes);
        List<WeaponSequenceEventKeyframe> events = CopyEvents(sequence.EventKeyframes);
        motions.Sort((left, right) => left.normalizedTime.CompareTo(right.normalizedTime));
        events.Sort((left, right) => left.normalizedTime.CompareTo(right.normalizedTime));
        sequence.Overwrite(sequence.Duration, sequence.RestoreDefaultPoseOnComplete, motions, events);
        EditorUtility.SetDirty(sequence);
        RebuildSerializedObjects();
    }

    private void ClampAndRepairSequence()
    {
        if (sequence == null)
        {
            return;
        }

        ApplyModifiedProperties();
        Undo.RecordObject(sequence, "Clamp And Repair Attack Sequence");

        List<WeaponMotionKeyframe> motions = CopyMotions(sequence.MotionKeyframes);
        List<WeaponSequenceEventKeyframe> events = CopyEvents(sequence.EventKeyframes);

        for (int i = 0; i < motions.Count; i++)
        {
            WeaponMotionKeyframe frame = motions[i];
            frame.normalizedTime = Mathf.Clamp01(frame.normalizedTime);
            frame.xDynamicMinNormalizedReach = Mathf.Clamp01(frame.xDynamicMinNormalizedReach);
            frame.xDynamicMaxNormalizedReach = Mathf.Clamp(frame.xDynamicMaxNormalizedReach, frame.xDynamicMinNormalizedReach, 1f);
            frame.yDynamicMinNormalizedReach = Mathf.Clamp01(frame.yDynamicMinNormalizedReach);
            frame.yDynamicMaxNormalizedReach = Mathf.Clamp(frame.yDynamicMaxNormalizedReach, frame.yDynamicMinNormalizedReach, 1f);
            if (frame.ease == WeaponMotionEase.CustomCurve && frame.customCurve == null)
            {
                frame.customCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            }
            motions[i] = frame;
        }

        for (int i = 0; i < events.Count; i++)
        {
            WeaponSequenceEventKeyframe frame = events[i];
            frame.normalizedTime = Mathf.Clamp01(frame.normalizedTime);
            frame.eventKey = Mathf.Max(0, frame.eventKey);
            events[i] = frame;
        }

        motions.Sort((left, right) => left.normalizedTime.CompareTo(right.normalizedTime));
        events.Sort((left, right) => left.normalizedTime.CompareTo(right.normalizedTime));

        if (motions.Count == 0)
        {
            motions.Add(new WeaponMotionKeyframe(0f, Vector3.zero, Vector3.zero));
            motions.Add(new WeaponMotionKeyframe(1f, Vector3.zero, Vector3.zero));
        }
        else
        {
            if (motions[0].normalizedTime > 0.0001f)
            {
                WeaponMotionKeyframe start = motions[0];
                start.normalizedTime = 0f;
                motions.Insert(0, start);
            }

            if (motions[motions.Count - 1].normalizedTime < 0.9999f)
            {
                WeaponMotionKeyframe end = motions[motions.Count - 1];
                end.normalizedTime = 1f;
                motions.Add(end);
            }
        }

        sequence.Overwrite(sequence.Duration, sequence.RestoreDefaultPoseOnComplete, motions, events);
        EditorUtility.SetDirty(sequence);
        RebuildSerializedObjects();
    }

    private void AddMotionFrameAtPreviewTime()
    {
        InsertMotionFrameAtTime(previewNormalizedTime, null);
    }

    private void AddEventFrameAtPreviewTime()
    {
        InsertEventFrameAtTime(previewNormalizedTime);
    }

    private void CreateNewSequenceAsset()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "创建攻击序列",
            "武器攻击序列",
            "asset",
            "请选择新攻击序列资源的保存位置。");
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        AttackSequenceDefinitionSO asset = CreateInstance<AttackSequenceDefinitionSO>();
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = asset;
        AssignTargets(asset, weaponData);
    }

    private void DuplicateSequenceAsset()
    {
        if (sequence == null)
        {
            return;
        }

        string sourcePath = AssetDatabase.GetAssetPath(sequence);
        if (string.IsNullOrEmpty(sourcePath))
        {
            return;
        }

        string duplicatePath = AssetDatabase.GenerateUniqueAssetPath(sourcePath.Replace(".asset", " Copy.asset"));
        AttackSequenceDefinitionSO duplicate = Instantiate(sequence);
        duplicate.name = System.IO.Path.GetFileNameWithoutExtension(duplicatePath);
        AssetDatabase.CreateAsset(duplicate, duplicatePath);
        AssetDatabase.SaveAssets();
        Selection.activeObject = duplicate;
        AssignTargets(duplicate, weaponData);
    }

    private List<string> BuildDiagnostics()
    {
        List<string> warnings = new List<string>();
        if (sequence == null)
        {
            warnings.Add("当前没有载入 AttackSequenceDefinitionSO。");
            return warnings;
        }

        IReadOnlyList<WeaponMotionKeyframe> motions = sequence.MotionKeyframes;
        if (motions == null || motions.Count == 0)
        {
            warnings.Add("当前序列没有动作关键帧。");
        }
        else
        {
            if (motions[0].normalizedTime > 0.0001f)
            {
                warnings.Add("第一个动作帧没有从时间 0 开始。");
            }

            if (motions[motions.Count - 1].normalizedTime < 0.9999f)
            {
                warnings.Add("最后一个动作帧没有落到时间 1。");
            }

            for (int i = 1; i < motions.Count; i++)
            {
                if (motions[i].normalizedTime < motions[i - 1].normalizedTime)
                {
                    warnings.Add("动作关键帧没有按时间升序排列。");
                    break;
                }
            }
        }

        IReadOnlyList<WeaponSequenceEventKeyframe> events = sequence.EventKeyframes;
        if (events != null)
        {
            for (int i = 1; i < events.Count; i++)
            {
                if (events[i].normalizedTime < events[i - 1].normalizedTime)
                {
                    warnings.Add("事件关键帧没有按时间升序排列。");
                    break;
                }
            }

            AddWeaponEventWarnings(warnings, events);
        }

        if (weaponData != null && weaponData.AttackSequence != sequence)
        {
            warnings.Add("当前载入的 WeaponDataSO 没有挂到正在编辑的这份攻击序列上。");
        }

        return warnings;
    }

    private void AddWeaponEventWarnings(List<string> warnings, IReadOnlyList<WeaponSequenceEventKeyframe> events)
    {
        if (weaponData == null)
        {
            return;
        }

        int projectileCount = weaponData.SequenceProjectileList != null ? weaponData.SequenceProjectileList.Count : 0;
        int sfxCount = weaponData.SequenceSfxList != null ? weaponData.SequenceSfxList.Count : 0;
        int vfxCount = weaponData.SequenceVfxList != null ? weaponData.SequenceVfxList.Count : 0;

        for (int i = 0; i < events.Count; i++)
        {
            WeaponSequenceEventKeyframe frame = events[i];
            if (frame.eventType == WeaponSequenceEventType.SpawnProjectile && frame.eventKey >= projectileCount)
            {
                warnings.Add("投射物事件键值 " + frame.eventKey + " 没有对应的弹道配置。");
            }
            else if (frame.eventType == WeaponSequenceEventType.PlaySfx && frame.eventKey >= sfxCount)
            {
                warnings.Add("音效事件键值 " + frame.eventKey + " 没有对应的音效配置。");
            }
            else if (frame.eventType == WeaponSequenceEventType.PlayVfx && frame.eventKey >= vfxCount)
            {
                warnings.Add("特效事件键值 " + frame.eventKey + " 没有对应的特效配置。");
            }
        }
    }

    private void DrawLocalizedEnumPopup(SerializedProperty property, string label, string[] options)
    {
        property.enumValueIndex = EditorGUILayout.Popup(label, property.enumValueIndex, options);
    }

    private static string GetLocalizedPropertyLabel(string propertyName)
    {
        switch (propertyName)
        {
            case "duration":
                return "总时长";
            case "restoreDefaultPoseOnComplete":
                return "结束后恢复默认姿态";
            case "weaponPrefab":
                return "武器预制体";
            case "constructionScheme":
                return "构造方案";
            case "visualForwardAngle":
                return "美术朝向角";
            case "stopAimingWhenAttackReady":
                return "攻击前停止继续转向";
            case "attackSequenceOccupancy":
                return "攻击动作占比";
            case "sequenceProjectileList":
                return "序列投射物配置";
            case "sequenceSfxList":
                return "序列音效配置";
            case "sequenceVfxList":
                return "序列特效配置";
            case "hitSfxKey":
                return "命中音效";
            case "meleeHitVfxPrefab":
                return "近战命中特效";
            case "meleeHitBoxSize":
                return "近战命中框尺寸";
            case "meleeHitOffset":
                return "近战命中框偏移";
            case "attack":
                return "攻击力";
            case "attackSpeed":
                return "攻速";
            case "criticalChance":
                return "暴击率";
            case "criticalPercent":
                return "暴击倍率";
            case "range":
                return "攻击范围";
            default:
                return propertyName;
        }
    }

    private static string GetEventTypeLabel(WeaponSequenceEventType eventType)
    {
        return EventTypeLabels[(int)eventType];
    }

    private void DrawSequenceProperty(string propertyName)
    {
        SerializedProperty property = sequenceObject.FindProperty(propertyName);
        if (property != null)
        {
            EditorGUILayout.PropertyField(property, new GUIContent(GetLocalizedPropertyLabel(propertyName)));
        }
    }

    private void DrawWeaponProperty(string propertyName, bool includeChildren = false)
    {
        SerializedProperty property = weaponObject.FindProperty(propertyName);
        if (property != null)
        {
            EditorGUILayout.PropertyField(property, new GUIContent(GetLocalizedPropertyLabel(propertyName)), includeChildren);
        }
    }

    private int GetMotionCount()
    {
        return sequence != null && sequence.MotionKeyframes != null ? sequence.MotionKeyframes.Count : 0;
    }

    private int GetEventCount()
    {
        return sequence != null && sequence.EventKeyframes != null ? sequence.EventKeyframes.Count : 0;
    }

    private float ResolvePreviewAttackRange()
    {
        if (weaponObject != null)
        {
            SerializedProperty range = weaponObject.FindProperty("range");
            if (range != null)
            {
                return Mathf.Max(0.1f, range.floatValue);
            }
        }

        if (weaponData != null)
        {
            Dictionary<PropType, float> props = weaponData.GetPropsByLevel(1);
            if (props != null && props.TryGetValue(PropType.Range, out float value))
            {
                return Mathf.Max(0.1f, value);
            }
        }

        return Mathf.Max(0.1f, previewAttackRange > 0f ? previewAttackRange : DEFAULT_PREVIEW_RANGE);
    }

    private float ResolveVisualForwardAngle()
    {
        if (weaponObject != null)
        {
            SerializedProperty visualForwardAngle = weaponObject.FindProperty("visualForwardAngle");
            if (visualForwardAngle != null)
            {
                return visualForwardAngle.floatValue;
            }
        }

        return weaponData != null ? weaponData.VisualForwardAngle : 0f;
    }

    private void FitPreviewScaleToCurrentData()
    {
        float visibleWorldWidth = Mathf.Max(previewAttackRange * 2.2f, previewTargetDistance * 1.4f, 1f);
        Sprite sprite = ResolveWeaponPreviewSprite();
        if (sprite != null)
        {
            Vector2 spriteSize = sprite.bounds.size;
            visibleWorldWidth = Mathf.Max(visibleWorldWidth, Mathf.Max(spriteSize.x, spriteSize.y) * 2.4f);
        }

        float availableWidth = Mathf.Max(120f, PREVIEW_PANEL_WIDTH - PANEL_PADDING * 6f);
        previewPixelsPerUnit = Mathf.Clamp(availableWidth / visibleWorldWidth, MIN_PREVIEW_SCALE, MAX_PREVIEW_SCALE);
        Repaint();
    }

    private bool TryGetPresetDefinition(WeaponAnimationSequencePresetId id, out WeaponAnimationSequencePresetDefinition definition)
    {
        IReadOnlyList<WeaponAnimationSequencePresetDefinition> presets = WeaponAnimationSequencePresets.GetAllPresets();
        for (int i = 0; i < presets.Count; i++)
        {
            if (presets[i].Id == id)
            {
                definition = presets[i];
                return true;
            }
        }

        definition = default;
        return false;
    }

    private static void InitializeMotionKeyframe(SerializedProperty target, SerializedProperty source)
    {
        if (source == null)
        {
            target.FindPropertyRelative("normalizedTime").floatValue = 0f;
            target.FindPropertyRelative("xPositionMode").enumValueIndex = (int)WeaponMotionPositionMode.Fixed;
            target.FindPropertyRelative("yPositionMode").enumValueIndex = (int)WeaponMotionPositionMode.Fixed;
            target.FindPropertyRelative("dynamicPositionStrategy").enumValueIndex = (int)WeaponMotionDynamicPositionStrategy.None;
            target.FindPropertyRelative("localPositionX").floatValue = 0f;
            target.FindPropertyRelative("localPositionY").floatValue = 0f;
            target.FindPropertyRelative("xDynamicMinNormalizedReach").floatValue = 0f;
            target.FindPropertyRelative("xDynamicMaxNormalizedReach").floatValue = 0f;
            target.FindPropertyRelative("yDynamicMinNormalizedReach").floatValue = 0f;
            target.FindPropertyRelative("yDynamicMaxNormalizedReach").floatValue = 0f;
            target.FindPropertyRelative("localEulerAngles").vector3Value = Vector3.zero;
            target.FindPropertyRelative("ease").enumValueIndex = (int)WeaponMotionEase.Linear;
            target.FindPropertyRelative("customCurve").animationCurveValue = null;
            return;
        }

        CopyMotionKeyframe(source, target);
    }

    private static void CopyMotionKeyframe(SerializedProperty source, SerializedProperty target)
    {
        target.FindPropertyRelative("normalizedTime").floatValue = source.FindPropertyRelative("normalizedTime").floatValue;
        target.FindPropertyRelative("xPositionMode").enumValueIndex = source.FindPropertyRelative("xPositionMode").enumValueIndex;
        target.FindPropertyRelative("yPositionMode").enumValueIndex = source.FindPropertyRelative("yPositionMode").enumValueIndex;
        target.FindPropertyRelative("dynamicPositionStrategy").enumValueIndex = source.FindPropertyRelative("dynamicPositionStrategy").enumValueIndex;
        target.FindPropertyRelative("localPositionX").floatValue = source.FindPropertyRelative("localPositionX").floatValue;
        target.FindPropertyRelative("localPositionY").floatValue = source.FindPropertyRelative("localPositionY").floatValue;
        target.FindPropertyRelative("xDynamicMinNormalizedReach").floatValue = source.FindPropertyRelative("xDynamicMinNormalizedReach").floatValue;
        target.FindPropertyRelative("xDynamicMaxNormalizedReach").floatValue = source.FindPropertyRelative("xDynamicMaxNormalizedReach").floatValue;
        target.FindPropertyRelative("yDynamicMinNormalizedReach").floatValue = source.FindPropertyRelative("yDynamicMinNormalizedReach").floatValue;
        target.FindPropertyRelative("yDynamicMaxNormalizedReach").floatValue = source.FindPropertyRelative("yDynamicMaxNormalizedReach").floatValue;
        target.FindPropertyRelative("localEulerAngles").vector3Value = source.FindPropertyRelative("localEulerAngles").vector3Value;
        target.FindPropertyRelative("ease").enumValueIndex = source.FindPropertyRelative("ease").enumValueIndex;
        target.FindPropertyRelative("customCurve").animationCurveValue = source.FindPropertyRelative("customCurve").animationCurveValue;
    }

    private static List<WeaponMotionKeyframe> CopyMotions(IReadOnlyList<WeaponMotionKeyframe> source)
    {
        List<WeaponMotionKeyframe> result = new List<WeaponMotionKeyframe>();
        if (source == null)
        {
            return result;
        }

        for (int i = 0; i < source.Count; i++)
        {
            result.Add(source[i]);
        }

        return result;
    }

    private static List<WeaponSequenceEventKeyframe> CopyEvents(IReadOnlyList<WeaponSequenceEventKeyframe> source)
    {
        List<WeaponSequenceEventKeyframe> result = new List<WeaponSequenceEventKeyframe>();
        if (source == null)
        {
            return result;
        }

        for (int i = 0; i < source.Count; i++)
        {
            result.Add(source[i]);
        }

        return result;
    }

    private Vector2 WorldToCanvas(Vector2 world, Rect rect)
    {
        Vector2 origin = new Vector2(rect.center.x, rect.yMax - rect.height * 0.35f);
        return new Vector2(origin.x + world.x * previewPixelsPerUnit, origin.y - world.y * previewPixelsPerUnit);
    }

    private static void DrawGuiLine(Vector2 start, Vector2 end, Color color, float width)
    {
        Handles.color = color;
        Handles.DrawAAPolyLine(width, new Vector3(start.x, start.y, 0f), new Vector3(end.x, end.y, 0f));
    }

    private void DrawRotatedRect(Rect canvas, Vector2 center, Vector2 size, float angle, Color color)
    {
        Vector2 halfSize = size * 0.5f;
        Vector2 a = center + Rotate(new Vector2(-halfSize.x, -halfSize.y), angle);
        Vector2 b = center + Rotate(new Vector2(-halfSize.x, halfSize.y), angle);
        Vector2 c = center + Rotate(new Vector2(halfSize.x, halfSize.y), angle);
        Vector2 d = center + Rotate(new Vector2(halfSize.x, -halfSize.y), angle);

        Vector2 ca = WorldToCanvas(a, canvas);
        Vector2 cb = WorldToCanvas(b, canvas);
        Vector2 cc = WorldToCanvas(c, canvas);
        Vector2 cd = WorldToCanvas(d, canvas);

        DrawGuiLine(ca, cb, color, 2f);
        DrawGuiLine(cb, cc, color, 2f);
        DrawGuiLine(cc, cd, color, 2f);
        DrawGuiLine(cd, ca, color, 2f);
    }

    private static Vector2 Rotate(Vector2 value, float angleDegrees)
    {
        float radians = angleDegrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);
        return new Vector2(value.x * cos - value.y * sin, value.x * sin + value.y * cos);
    }

    private static Color GetEventColor(WeaponSequenceEventType eventType)
    {
        switch (eventType)
        {
            case WeaponSequenceEventType.OpenHitWindow:
                return new Color(1f, 0.28f, 0.18f, 1f);
            case WeaponSequenceEventType.CloseHitWindow:
                return new Color(0.9f, 0.55f, 0.25f, 1f);
            case WeaponSequenceEventType.SpawnProjectile:
                return new Color(0.25f, 0.85f, 1f, 1f);
            case WeaponSequenceEventType.PlaySfx:
                return new Color(0.65f, 0.45f, 1f, 1f);
            case WeaponSequenceEventType.PlayVfx:
                return new Color(0.25f, 1f, 0.55f, 1f);
            default:
                return Color.white;
        }
    }

    private static float EvaluateEase(float t, WeaponMotionEase ease, AnimationCurve customCurve)
    {
        switch (ease)
        {
            case WeaponMotionEase.InSine:
                return 1f - Mathf.Cos((t * Mathf.PI) * 0.5f);
            case WeaponMotionEase.OutSine:
                return Mathf.Sin((t * Mathf.PI) * 0.5f);
            case WeaponMotionEase.InOutSine:
                return -(Mathf.Cos(Mathf.PI * t) - 1f) * 0.5f;
            case WeaponMotionEase.InQuad:
                return t * t;
            case WeaponMotionEase.OutQuad:
                return 1f - ((1f - t) * (1f - t));
            case WeaponMotionEase.InOutQuad:
                return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f;
            case WeaponMotionEase.InCubic:
                return t * t * t;
            case WeaponMotionEase.OutCubic:
                return 1f - Mathf.Pow(1f - t, 3f);
            case WeaponMotionEase.InOutCubic:
                return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;
            case WeaponMotionEase.InExpo:
                return t <= 0f ? 0f : Mathf.Pow(2f, 10f * t - 10f);
            case WeaponMotionEase.OutExpo:
                return t >= 1f ? 1f : 1f - Mathf.Pow(2f, -10f * t);
            case WeaponMotionEase.InOutExpo:
                if (t <= 0f)
                {
                    return 0f;
                }

                if (t >= 1f)
                {
                    return 1f;
                }

                return t < 0.5f
                    ? Mathf.Pow(2f, 20f * t - 10f) * 0.5f
                    : (2f - Mathf.Pow(2f, -20f * t + 10f)) * 0.5f;
            case WeaponMotionEase.OutBack:
                const float c1 = 1.70158f;
                const float c3 = c1 + 1f;
                float p = t - 1f;
                return 1f + c3 * p * p * p + c1 * p * p;
            case WeaponMotionEase.OutElastic:
                if (t <= 0f)
                {
                    return 0f;
                }

                if (t >= 1f)
                {
                    return 1f;
                }

                const float c4 = (2f * Mathf.PI) / 3f;
                return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
            case WeaponMotionEase.CustomCurve:
                return customCurve != null ? customCurve.Evaluate(t) : t;
            default:
                return t;
        }
    }

    private readonly struct PreviewPose
    {
        public readonly Vector2 position;
        public readonly float angleZ;

        public PreviewPose(Vector2 position, float angleZ)
        {
            this.position = position;
            this.angleZ = angleZ;
        }
    }

    private readonly struct SmartMotionSample
    {
        public readonly Vector2 position;
        public readonly float angleZ;
        public readonly WeaponMotionEase ease;

        public SmartMotionSample(Vector2 position, float angleZ, WeaponMotionEase ease)
        {
            this.position = position;
            this.angleZ = angleZ;
            this.ease = ease;
        }
    }

    private enum TimelineDragMode
    {
        None,
        Scrub,
        Motion,
        Event
    }

    private enum MotionAxis
    {
        X,
        Y
    }

    private enum PreviewCurveChannel
    {
        PositionX,
        PositionY,
        RotationZ
    }
}

[CustomEditor(typeof(WeaponDataSO))]
internal sealed class WeaponDataSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(8f);
        if (GUILayout.Button("打开攻击序列工作台"))
        {
            AttackSequenceStudioWindow.Open((WeaponDataSO)target);
        }
    }
}
#endif
