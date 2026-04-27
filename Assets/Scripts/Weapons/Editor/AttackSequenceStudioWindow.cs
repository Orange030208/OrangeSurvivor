#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public sealed class AttackSequenceStudioWindow : EditorWindow
{
    private const float MIN_LEFT_PANEL_WIDTH = 260f;
    private const float MAX_LEFT_PANEL_WIDTH = 340f;
    private const float MIN_MIDDLE_PANEL_WIDTH = 430f;
    private const float MAX_MIDDLE_PANEL_WIDTH = 640f;
    private const float MIN_RIGHT_PANEL_WIDTH = 380f;
    private const float MIN_PREVIEW_HEIGHT = 300f;
    private const float PANEL_PADDING = 8f;
    private const float COLUMN_GAP = 6f;
    private const float MIN_PREVIEW_SCALE = 35f;
    private const float MAX_PREVIEW_SCALE = 140f;
    private const float TARGET_MARKER_HIT_RADIUS = 15f;
    private const float MOTION_MARKER_HIT_RADIUS = 12f;
    private const float TIMELINE_HEIGHT = 38f;
    private const float TIMELINE_MARGIN = 10f;

    private static readonly Color CanvasBorderColor = new(0.07f, 0.07f, 0.07f, 1f);
    private static readonly Color CanvasBackgroundColor = new(0.15f, 0.15f, 0.16f, 1f);
    private static readonly Color GridColor = new(1f, 1f, 1f, 0.07f);
    private static readonly Color PoseColor = new(0.2f, 0.8f, 1f, 1f);
    private static readonly Color TargetColor = new(1f, 0.75f, 0.2f, 1f);
    private static readonly Color ReferenceColor = new(0.55f, 1f, 0.55f, 0.9f);

    [SerializeField] private AttackSequenceDefinitionSO sequence;
    [SerializeField] private WeaponDataSO weaponData;
    [SerializeField] private WeaponAnimationSequencePresetId selectedPreset;
    [SerializeField] private float previewNormalizedTime;
    [SerializeField] private bool previewPlaying;
    [SerializeField] private bool loopPreview = true;
    [SerializeField] private Vector2 previewTargetOffset = new(0f, 1f);
    [SerializeField] private float previewPixelsPerUnit = 70f;
    [SerializeField] private Vector2 previewCanvasPan;
    [SerializeField] private bool showMotionPath = true;
    [SerializeField] private bool showHitBox = true;
    [SerializeField] private bool showReferenceOffset = true;
    [SerializeField] private int selectedMotionIndex = -1;
    [SerializeField] private int selectedEventIndex = -1;

    private SerializedObject sequenceObject;
    private ReorderableList motionList;
    private ReorderableList eventList;
    private Vector2 leftScroll;
    private Vector2 middleScroll;
    private PreviewDragMode previewDragMode;
    private int previewDragMotionIndex = -1;
    private Vector2 previewDragStartMouse;
    private Vector2 previewDragStartPan;
    private double lastEditorTime;

    [MenuItem("Tools/Weapons/Attack Sequence Studio")]
    public static void OpenFromMenu()
    {
        AttackSequenceStudioWindow window = GetWindow<AttackSequenceStudioWindow>("Attack Sequence Studio");
        window.TryLoadTargetsFromSelection(false);
        window.Show();
    }

    [MenuItem("Assets/Open In Attack Sequence Studio", true)]
    private static bool ValidateOpenFromAssetMenu()
    {
        Object activeObject = Selection.activeObject;
        return activeObject is AttackSequenceDefinitionSO || activeObject is WeaponDataSO;
    }

    [MenuItem("Assets/Open In Attack Sequence Studio")]
    private static void OpenFromAssetMenu()
    {
        AttackSequenceStudioWindow window = GetWindow<AttackSequenceStudioWindow>("Attack Sequence Studio");
        window.TryLoadTargetsFromSelection(true);
        window.Show();
    }

    public static void Open(AttackSequenceDefinitionSO targetSequence, WeaponDataSO targetWeapon = null)
    {
        AttackSequenceStudioWindow window = GetWindow<AttackSequenceStudioWindow>("Attack Sequence Studio");
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
        minSize = new Vector2(1120f, 560f);
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

    private void OnEditorUpdate()
    {
        double time = EditorApplication.timeSinceStartup;
        float deltaTime = Mathf.Max(0f, (float)(time - lastEditorTime));
        lastEditorTime = time;

        if (!previewPlaying || sequence == null)
        {
            return;
        }

        previewNormalizedTime += deltaTime / Mathf.Max(0.01f, sequence.Duration);
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

    private void OnGUI()
    {
        DrawToolbar();
        EnsureSerializedObject();

        if (sequenceObject == null)
        {
            DrawEmptyState();
            return;
        }

        sequenceObject.Update();
        EnsureListsReady();
        EnsureSelectedIndices();

        EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        DrawLeftColumn();
        GUILayout.Space(COLUMN_GAP);
        DrawMiddleColumn();
        GUILayout.Space(COLUMN_GAP);
        sequenceObject.ApplyModifiedProperties();
        DrawPreviewColumn();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        EditorGUILayout.LabelField("Attack Sequence Studio", EditorStyles.boldLabel, GUILayout.Width(180f));

        EditorGUI.BeginChangeCheck();
        AttackSequenceDefinitionSO nextSequence = (AttackSequenceDefinitionSO)EditorGUILayout.ObjectField(sequence, typeof(AttackSequenceDefinitionSO), false, GUILayout.MinWidth(180f));
        WeaponDataSO nextWeapon = (WeaponDataSO)EditorGUILayout.ObjectField(weaponData, typeof(WeaponDataSO), false, GUILayout.MinWidth(180f));
        if (EditorGUI.EndChangeCheck())
        {
            AssignTargets(nextSequence, nextWeapon);
        }

        if (GUILayout.Button("Load Selection", EditorStyles.toolbarButton, GUILayout.Width(110f)))
        {
            TryLoadTargetsFromSelection(true);
            GUIUtility.ExitGUI();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawEmptyState()
    {
        EditorGUILayout.Space(12f);
        EditorGUILayout.HelpBox("Load an AttackSequenceDefinitionSO or WeaponDataSO to edit weapon motion samples.", MessageType.Info);
        if (GUILayout.Button("Load Selection", GUILayout.Width(160f)))
        {
            TryLoadTargetsFromSelection(true);
            GUIUtility.ExitGUI();
        }
    }

    private void DrawLeftColumn()
    {
        float width = ResolveLeftColumnWidth();
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(width), GUILayout.MinWidth(MIN_LEFT_PANEL_WIDTH), GUILayout.ExpandHeight(true));
        leftScroll = EditorGUILayout.BeginScrollView(leftScroll, GUILayout.ExpandHeight(true));

        DrawSequenceSettings();
        EditorGUILayout.Space(6f);
        DrawPresetPanel();
        EditorGUILayout.Space(6f);
        DrawQuickActionsPanel();
        EditorGUILayout.Space(6f);
        DrawDiagnostics();

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawMiddleColumn()
    {
        float width = ResolveMiddleColumnWidth();
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(width), GUILayout.MinWidth(MIN_MIDDLE_PANEL_WIDTH), GUILayout.ExpandHeight(true));
        middleScroll = EditorGUILayout.BeginScrollView(middleScroll, GUILayout.ExpandHeight(true));

        DrawMotionSection();
        EditorGUILayout.Space(6f);
        DrawEventSection();

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawSequenceSettings()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Sequence", EditorStyles.boldLabel);
        DrawSequenceProperty("duration", "Duration");
        DrawSequenceProperty("restoreDefaultPoseOnComplete", "Restore Default Pose");

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Retarget", EditorStyles.boldLabel);
        DrawSequenceProperty("referenceTargetOffset", "Reference Target Offset");
        DrawSequenceProperty("retargetScaleWeight", "Scale Weight");
        EditorGUILayout.EndVertical();
    }

    private void DrawPresetPanel()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Preset", EditorStyles.boldLabel);
        selectedPreset = (WeaponAnimationSequencePresetId)EditorGUILayout.EnumPopup("Preset", selectedPreset);

        if (GUILayout.Button("Apply Preset"))
        {
            ApplySelectedPreset();
            GUIUtility.ExitGUI();
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawQuickActionsPanel()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

        if (GUILayout.Button("Sort Motion By Time"))
        {
            SortMotionFramesByTime();
            GUIUtility.ExitGUI();
        }

        if (GUILayout.Button("Sort Events By Time"))
        {
            SortEventFramesByTime();
            GUIUtility.ExitGUI();
        }

        if (GUILayout.Button("Reset Preview View"))
        {
            previewCanvasPan = Vector2.zero;
            previewTargetOffset = sequence != null ? sequence.ReferenceTargetOffset : new Vector2(0f, 1f);
            Repaint();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawMotionSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Motion Samples", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Add At Time", GUILayout.Width(95f)))
        {
            AddMotionFrameAtPreviewTime();
            GUIUtility.ExitGUI();
        }
        EditorGUILayout.EndHorizontal();

        motionList?.DoLayoutList();
        DrawSelectedMotionInspector();
        EditorGUILayout.EndVertical();
    }

    private void DrawSelectedMotionInspector()
    {
        SerializedProperty frames = sequenceObject.FindProperty("motionKeyframes");
        if (frames == null || frames.arraySize == 0)
        {
            EditorGUILayout.HelpBox("No motion samples yet.", MessageType.None);
            return;
        }

        selectedMotionIndex = Mathf.Clamp(selectedMotionIndex, 0, frames.arraySize - 1);
        if (motionList != null)
        {
            motionList.index = selectedMotionIndex;
        }

        SerializedProperty element = frames.GetArrayElementAtIndex(selectedMotionIndex);
        SerializedProperty normalizedTime = element.FindPropertyRelative("normalizedTime");
        SerializedProperty localPositionX = element.FindPropertyRelative("localPositionX");
        SerializedProperty localPositionY = element.FindPropertyRelative("localPositionY");
        SerializedProperty localEulerAngles = element.FindPropertyRelative("localEulerAngles");
        SerializedProperty ease = element.FindPropertyRelative("ease");

        EditorGUILayout.Space(4f);
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Selected Sample " + selectedMotionIndex, EditorStyles.boldLabel);
        if (GUILayout.Button("Use Preview Time", GUILayout.Width(118f)))
        {
            normalizedTime.floatValue = previewNormalizedTime;
        }
        if (GUILayout.Button("Go", GUILayout.Width(42f)))
        {
            previewNormalizedTime = normalizedTime.floatValue;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.PropertyField(normalizedTime, new GUIContent("Time"));

        EditorGUI.BeginChangeCheck();
        Vector2 localPosition = new(localPositionX.floatValue, localPositionY.floatValue);
        localPosition = EditorGUILayout.Vector2Field("Sample Position", localPosition);
        if (EditorGUI.EndChangeCheck())
        {
            localPositionX.floatValue = localPosition.x;
            localPositionY.floatValue = localPosition.y;
        }

        EditorGUILayout.PropertyField(localEulerAngles, new GUIContent("Local Euler"));
        EditorGUILayout.PropertyField(ease, new GUIContent("Ease"));
        if ((WeaponMotionEase)ease.enumValueIndex == WeaponMotionEase.CustomCurve)
        {
            EditorGUILayout.PropertyField(element.FindPropertyRelative("customCurve"), new GUIContent("Custom Curve"));
        }

        Vector2 previewPosition = RetargetPosition(localPosition);
        EditorGUILayout.LabelField("Preview Position", $"X {previewPosition.x:0.###} / Y {previewPosition.y:0.###}", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();
    }

    private void DrawEventSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Sequence Events", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Add At Time", GUILayout.Width(95f)))
        {
            AddEventFrameAtPreviewTime();
            GUIUtility.ExitGUI();
        }
        EditorGUILayout.EndHorizontal();

        eventList?.DoLayoutList();
        DrawSelectedEventInspector();
        EditorGUILayout.EndVertical();
    }

    private void DrawSelectedEventInspector()
    {
        SerializedProperty frames = sequenceObject.FindProperty("eventKeyframes");
        if (frames == null || frames.arraySize == 0)
        {
            EditorGUILayout.HelpBox("No sequence events yet.", MessageType.None);
            return;
        }

        selectedEventIndex = Mathf.Clamp(selectedEventIndex, 0, frames.arraySize - 1);
        if (eventList != null)
        {
            eventList.index = selectedEventIndex;
        }

        SerializedProperty element = frames.GetArrayElementAtIndex(selectedEventIndex);
        EditorGUILayout.Space(4f);
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Selected Event " + selectedEventIndex, EditorStyles.boldLabel);
        if (GUILayout.Button("Use Preview Time", GUILayout.Width(118f)))
        {
            element.FindPropertyRelative("normalizedTime").floatValue = previewNormalizedTime;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.PropertyField(element.FindPropertyRelative("normalizedTime"), new GUIContent("Time"));
        EditorGUILayout.PropertyField(element.FindPropertyRelative("eventType"), new GUIContent("Type"));
        EditorGUILayout.PropertyField(element.FindPropertyRelative("eventKey"), new GUIContent("Key"));
        EditorGUILayout.EndVertical();
    }

    private void DrawPreviewColumn()
    {
        float width = ResolveRightColumnWidth();
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(width), GUILayout.MinWidth(MIN_RIGHT_PANEL_WIDTH), GUILayout.ExpandHeight(true));
        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
        DrawPreviewControls();

        Rect previewRect = GUILayoutUtility.GetRect(1f, MIN_PREVIEW_HEIGHT, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        DrawPreview(previewRect);
        EditorGUILayout.EndVertical();
    }

    private void DrawPreviewControls()
    {
        EditorGUILayout.BeginHorizontal();
        previewTargetOffset = EditorGUILayout.Vector2Field("Target Offset", previewTargetOffset);
        if (GUILayout.Button("Ref", GUILayout.Width(44f)))
        {
            previewTargetOffset = sequence.ReferenceTargetOffset;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(previewPlaying ? "Pause" : "Play", GUILayout.Width(72f)))
        {
            previewPlaying = !previewPlaying;
            lastEditorTime = EditorApplication.timeSinceStartup;
        }

        if (GUILayout.Button("Stop", GUILayout.Width(54f)))
        {
            previewPlaying = false;
            previewNormalizedTime = 0f;
        }

        loopPreview = EditorGUILayout.Toggle("Loop", loopPreview, GUILayout.Width(86f));
        showMotionPath = EditorGUILayout.Toggle("Path", showMotionPath);
        showHitBox = EditorGUILayout.Toggle("Hit Box", showHitBox);
        showReferenceOffset = EditorGUILayout.Toggle("Reference", showReferenceOffset);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawPreview(Rect rect)
    {
        EditorGUI.DrawRect(rect, CanvasBorderColor);
        Rect canvas = new(rect.x + PANEL_PADDING, rect.y + PANEL_PADDING, rect.width - PANEL_PADDING * 2f, rect.height - PANEL_PADDING * 2f);
        EditorGUI.DrawRect(canvas, CanvasBackgroundColor);

        HandlePreviewInput(canvas);

        GUI.BeginGroup(canvas);
        Rect localCanvas = new(0f, 0f, canvas.width, canvas.height);
        Handles.BeginGUI();
        DrawGrid(localCanvas);

        if (showReferenceOffset)
        {
            DrawReferenceMarker(localCanvas);
        }

        DrawTargetMarker(localCanvas);

        if (sequence != null)
        {
            if (showMotionPath)
            {
                DrawMotionPath(localCanvas);
            }

            DrawMotionKeyframeMarkers(localCanvas);
            DrawCurrentPose(localCanvas);
        }

        Handles.EndGUI();
        DrawPreviewTimeline(localCanvas);
        GUI.Label(
            new Rect(8f, localCanvas.yMax - TIMELINE_HEIGHT - 22f, localCanvas.width - 16f, 18f),
            "Drag target/samples. Drag empty space to pan, wheel to zoom.",
            EditorStyles.miniLabel);
        GUI.EndGroup();

    }

    private void HandlePreviewInput(Rect rect)
    {
        Event current = Event.current;
        int controlId = GUIUtility.GetControlID(FocusType.Passive, rect);
        Vector2 mouse = current.mousePosition;
        bool ownsMouse = GUIUtility.hotControl == controlId;
        bool isInside = rect.Contains(mouse);
        Rect timelineRect = GetAbsoluteTimelineRect(rect);
        bool hitTimeline = timelineRect.Contains(mouse);
        int hitMotionIndex = isInside && !hitTimeline ? FindMotionMarkerAtCanvas(rect, mouse) : -1;
        bool hitTarget = isInside && Vector2.Distance(WorldToCanvas(previewTargetOffset, rect), mouse) <= TARGET_MARKER_HIT_RADIUS;

        if (isInside)
        {
            if (hitTimeline)
            {
                EditorGUIUtility.AddCursorRect(rect, MouseCursor.SlideArrow);
            }
            else if (hitTarget || hitMotionIndex >= 0)
            {
                EditorGUIUtility.AddCursorRect(rect, MouseCursor.MoveArrow);
            }
            else
            {
                EditorGUIUtility.AddCursorRect(rect, MouseCursor.Pan);
            }
        }

        if (current.type == EventType.ScrollWheel && isInside)
        {
            previewPixelsPerUnit = Mathf.Clamp(previewPixelsPerUnit - current.delta.y * 4f, MIN_PREVIEW_SCALE, MAX_PREVIEW_SCALE);
            current.Use();
            Repaint();
            return;
        }

        if (current.type == EventType.MouseDown && isInside)
        {
            if (current.button == 0 && hitTimeline)
            {
                previewPlaying = false;
                previewDragMode = PreviewDragMode.Timeline;
                previewNormalizedTime = NormalizeTimelinePosition(timelineRect, mouse.x);
                GUIUtility.hotControl = controlId;
                current.Use();
                Repaint();
                return;
            }

            if (current.button == 0 && hitTarget)
            {
                previewDragMode = PreviewDragMode.Target;
                GUIUtility.hotControl = controlId;
                current.Use();
                return;
            }

            if (current.button == 0 && hitMotionIndex >= 0)
            {
                Undo.RecordObject(sequence, "Move Attack Motion Sample");
                previewDragMode = PreviewDragMode.MotionSample;
                previewDragMotionIndex = hitMotionIndex;
                selectedMotionIndex = hitMotionIndex;
                if (motionList != null)
                {
                    motionList.index = hitMotionIndex;
                }

                IReadOnlyList<WeaponMotionKeyframe> frames = sequence.MotionKeyframes;
                previewNormalizedTime = frames[hitMotionIndex].normalizedTime;
                GUIUtility.hotControl = controlId;
                current.Use();
                return;
            }

            if (current.button == 0 || current.button == 1 || current.button == 2)
            {
                previewDragMode = PreviewDragMode.Pan;
                previewDragStartMouse = mouse;
                previewDragStartPan = previewCanvasPan;
                GUIUtility.hotControl = controlId;
                current.Use();
                return;
            }
        }

        if (current.type == EventType.MouseDrag && ownsMouse)
        {
            if (previewDragMode == PreviewDragMode.Timeline)
            {
                previewNormalizedTime = NormalizeTimelinePosition(timelineRect, mouse.x);
                current.Use();
                Repaint();
                return;
            }

            if (previewDragMode == PreviewDragMode.Target)
            {
                previewTargetOffset = CanvasToWorld(mouse, rect);
                current.Use();
                Repaint();
                return;
            }

            if (previewDragMode == PreviewDragMode.MotionSample && previewDragMotionIndex >= 0)
            {
                SetMotionFramePreviewPosition(previewDragMotionIndex, CanvasToWorld(mouse, rect));
                current.Use();
                Repaint();
                return;
            }

            if (previewDragMode == PreviewDragMode.Pan)
            {
                previewCanvasPan = previewDragStartPan + mouse - previewDragStartMouse;
                current.Use();
                Repaint();
                return;
            }
        }

        if ((current.type == EventType.MouseUp || current.type == EventType.Ignore) && ownsMouse)
        {
            GUIUtility.hotControl = 0;
            previewDragMode = PreviewDragMode.None;
            previewDragMotionIndex = -1;
            current.Use();
        }
    }

    private void DrawGrid(Rect rect)
    {
        Vector2 origin = WorldToCanvas(Vector2.zero, rect);
        float scale = Mathf.Max(1f, previewPixelsPerUnit);
        Handles.color = GridColor;

        for (float x = origin.x % scale; x < rect.xMax; x += scale)
        {
            if (x >= rect.xMin)
            {
                Handles.DrawLine(new Vector3(x, rect.yMin), new Vector3(x, rect.yMax));
            }
        }

        for (float y = origin.y % scale; y < rect.yMax; y += scale)
        {
            if (y >= rect.yMin)
            {
                Handles.DrawLine(new Vector3(rect.xMin, y), new Vector3(rect.xMax, y));
            }
        }

        DrawGuiLine(new Vector2(rect.xMin, origin.y), new Vector2(rect.xMax, origin.y), new Color(1f, 0.35f, 0.35f, 0.65f), 1.5f);
        DrawGuiLine(new Vector2(origin.x, rect.yMin), new Vector2(origin.x, rect.yMax), new Color(0.45f, 1f, 0.45f, 0.65f), 1.5f);
    }

    private void DrawReferenceMarker(Rect rect)
    {
        Vector2 point = WorldToCanvas(sequence.ReferenceTargetOffset, rect);
        Handles.color = ReferenceColor;
        Handles.DrawWireDisc(point, Vector3.forward, 6f);
        GUI.Label(new Rect(point.x + 7f, point.y - 9f, 72f, 18f), "ref", EditorStyles.miniLabel);
    }

    private void DrawTargetMarker(Rect rect)
    {
        Vector2 target = WorldToCanvas(previewTargetOffset, rect);
        Handles.color = TargetColor;
        Handles.DrawSolidDisc(target, Vector3.forward, 3f);
        Handles.DrawWireDisc(target, Vector3.forward, 9f);
        Handles.DrawLine(target + new Vector2(-13f, 0f), target + new Vector2(13f, 0f));
        Handles.DrawLine(target + new Vector2(0f, -13f), target + new Vector2(0f, 13f));
        GUI.Label(new Rect(target.x + 10f, target.y - 10f, 80f, 18f), "target", EditorStyles.miniLabel);
    }

    private void DrawMotionPath(Rect rect)
    {
        IReadOnlyList<WeaponMotionKeyframe> frames = sequence.MotionKeyframes;
        if (frames == null || frames.Count == 0)
        {
            return;
        }

        const int SAMPLE_COUNT = 96;
        Vector3[] points = new Vector3[SAMPLE_COUNT];
        for (int i = 0; i < SAMPLE_COUNT; i++)
        {
            float t = i / (SAMPLE_COUNT - 1f);
            points[i] = WorldToCanvas(SamplePose(t).Position, rect);
        }

        Handles.color = new Color(0.2f, 0.8f, 1f, 0.82f);
        Handles.DrawAAPolyLine(2f, points);
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
            Vector2 localPosition = new(frames[i].localPositionX, frames[i].localPositionY);
            Vector2 point = WorldToCanvas(RetargetPosition(localPosition), rect);
            bool selected = i == selectedMotionIndex || i == previewDragMotionIndex;
            Handles.color = selected ? Color.yellow : new Color(0.78f, 0.94f, 1f, 1f);
            Handles.DrawSolidDisc(point, Vector3.forward, selected ? 5.5f : 4f);
            GUI.Label(new Rect(point.x + 6f, point.y - 9f, 30f, 18f), i.ToString(), EditorStyles.miniLabel);
        }
    }

    private void DrawPreviewTimeline(Rect rect)
    {
        Rect timelineRect = GetTimelineRect(rect);
        Rect trackRect = new(timelineRect.x + TIMELINE_MARGIN, timelineRect.center.y - 3f, timelineRect.width - TIMELINE_MARGIN * 2f, 6f);

        EditorGUI.DrawRect(timelineRect, new Color(0f, 0f, 0f, 0.32f));
        EditorGUI.DrawRect(trackRect, new Color(0.45f, 0.45f, 0.45f, 0.85f));
        EditorGUI.DrawRect(new Rect(trackRect.x, trackRect.y, trackRect.width * Mathf.Clamp01(previewNormalizedTime), trackRect.height), PoseColor);

        DrawTimelineMotionMarkers(trackRect);
        DrawTimelineEventMarkers(trackRect);

        float playheadX = Mathf.Lerp(trackRect.xMin, trackRect.xMax, Mathf.Clamp01(previewNormalizedTime));
        EditorGUI.DrawRect(new Rect(playheadX - 1f, timelineRect.y + 6f, 2f, timelineRect.height - 12f), Color.white);
        GUI.Label(new Rect(playheadX - 22f, timelineRect.yMax - 18f, 44f, 16f), previewNormalizedTime.ToString("0.00"), EditorStyles.centeredGreyMiniLabel);
    }

    private void DrawTimelineMotionMarkers(Rect trackRect)
    {
        IReadOnlyList<WeaponMotionKeyframe> frames = sequence != null ? sequence.MotionKeyframes : null;
        if (frames == null)
        {
            return;
        }

        Handles.BeginGUI();
        for (int i = 0; i < frames.Count; i++)
        {
            float x = Mathf.Lerp(trackRect.xMin, trackRect.xMax, Mathf.Clamp01(frames[i].normalizedTime));
            Handles.color = i == selectedMotionIndex ? Color.yellow : new Color(0.65f, 0.95f, 1f, 1f);
            Handles.DrawSolidDisc(new Vector2(x, trackRect.center.y), Vector3.forward, i == selectedMotionIndex ? 4f : 3f);
        }
        Handles.EndGUI();
    }

    private void DrawTimelineEventMarkers(Rect trackRect)
    {
        IReadOnlyList<WeaponSequenceEventKeyframe> frames = sequence != null ? sequence.EventKeyframes : null;
        if (frames == null)
        {
            return;
        }

        Handles.BeginGUI();
        for (int i = 0; i < frames.Count; i++)
        {
            float x = Mathf.Lerp(trackRect.xMin, trackRect.xMax, Mathf.Clamp01(frames[i].normalizedTime));
            Vector3 top = new(x, trackRect.yMin - 7f, 0f);
            Vector3 bottom = new(x, trackRect.yMin - 1f, 0f);
            Handles.color = new Color(1f, 0.72f, 0.25f, 1f);
            Handles.DrawAAPolyLine(2f, top, bottom);
        }
        Handles.EndGUI();
    }

    private void DrawCurrentPose(Rect rect)
    {
        PreviewPose pose = SamplePose(previewNormalizedTime);
        Vector2 center = WorldToCanvas(pose.Position, rect);

        if (showHitBox && weaponData != null)
        {
            DrawRotatedRect(rect, pose.Position + Rotate(weaponData.MeleeHitOffset, pose.AngleZ), weaponData.MeleeHitBoxSize, pose.AngleZ, new Color(1f, 0.25f, 0.2f, 0.88f));
        }

        Sprite sprite = ResolveWeaponPreviewSprite();
        if (sprite != null)
        {
            DrawSpritePreview(sprite, center, pose.AngleZ);
        }
        else
        {
            Handles.color = PoseColor;
            Handles.DrawSolidDisc(center, Vector3.forward, 5f);
            DrawArrow(center, pose.AngleZ, 34f, PoseColor);
        }
    }

    private Rect GetTimelineRect(Rect rect)
    {
        return new Rect(0f, rect.height - TIMELINE_HEIGHT, rect.width, TIMELINE_HEIGHT);
    }

    private Rect GetAbsoluteTimelineRect(Rect rect)
    {
        return new Rect(rect.x, rect.yMax - TIMELINE_HEIGHT, rect.width, TIMELINE_HEIGHT);
    }

    private void DrawSpritePreview(Sprite sprite, Vector2 center, float angleDegrees)
    {
        Rect textureRect = sprite.textureRect;
        Rect uv = new(
            textureRect.x / sprite.texture.width,
            textureRect.y / sprite.texture.height,
            textureRect.width / sprite.texture.width,
            textureRect.height / sprite.texture.height);

        float pixelsPerUnit = Mathf.Max(1f, sprite.pixelsPerUnit);
        Vector2 spriteWorldSize = new(sprite.rect.width / pixelsPerUnit, sprite.rect.height / pixelsPerUnit);
        Vector2 spriteCanvasSize = spriteWorldSize * previewPixelsPerUnit;
        Rect drawRect = new(center.x - spriteCanvasSize.x * 0.5f, center.y - spriteCanvasSize.y * 0.5f, spriteCanvasSize.x, spriteCanvasSize.y);

        Matrix4x4 previousMatrix = GUI.matrix;
        GUIUtility.RotateAroundPivot(-angleDegrees, center);
        GUI.DrawTextureWithTexCoords(drawRect, sprite.texture, uv, true);
        GUI.matrix = previousMatrix;

        Handles.color = PoseColor;
        Handles.DrawSolidDisc(center, Vector3.forward, 3f);
        DrawArrow(center, angleDegrees, Mathf.Max(22f, Mathf.Min(spriteCanvasSize.x, spriteCanvasSize.y) * 0.45f), PoseColor);
    }

    private Sprite ResolveWeaponPreviewSprite()
    {
        if (weaponData == null)
        {
            return null;
        }

        Weapon weaponPrefab = weaponData.WeaponPrefab;
        if (weaponPrefab != null)
        {
            SpriteRenderer renderer = weaponPrefab.GetComponentInChildren<SpriteRenderer>();
            if (renderer != null && renderer.sprite != null)
            {
                return renderer.sprite;
            }
        }

        return weaponData.ItemIcon;
    }

    private static float NormalizeTimelinePosition(Rect timelineRect, float mouseX)
    {
        Rect trackRect = new(timelineRect.x + TIMELINE_MARGIN, timelineRect.center.y - 3f, timelineRect.width - TIMELINE_MARGIN * 2f, 6f);
        return Mathf.Clamp01(Mathf.InverseLerp(trackRect.xMin, trackRect.xMax, mouseX));
    }

    private PreviewPose SamplePose(float normalizedTime)
    {
        IReadOnlyList<WeaponMotionKeyframe> frames = sequence.MotionKeyframes;
        if (frames == null || frames.Count == 0)
        {
            return new PreviewPose(Vector2.zero, 0f);
        }

        if (normalizedTime <= frames[0].normalizedTime)
        {
            return new PreviewPose(RetargetPosition(frames[0]), frames[0].localEulerAngles.z);
        }

        if (normalizedTime >= frames[frames.Count - 1].normalizedTime)
        {
            WeaponMotionKeyframe last = frames[frames.Count - 1];
            return new PreviewPose(RetargetPosition(last), last.localEulerAngles.z);
        }

        WeaponMotionKeyframe from = frames[0];
        WeaponMotionKeyframe to = frames[frames.Count - 1];
        for (int i = 0; i < frames.Count - 1; i++)
        {
            WeaponMotionKeyframe current = frames[i];
            WeaponMotionKeyframe next = frames[i + 1];
            if (normalizedTime >= current.normalizedTime && normalizedTime <= next.normalizedTime)
            {
                from = current;
                to = next;
                break;
            }
        }

        float segmentLength = Mathf.Max(0.0001f, to.normalizedTime - from.normalizedTime);
        float linearT = Mathf.Clamp01((normalizedTime - from.normalizedTime) / segmentLength);
        float easedT = EvaluateEase(linearT, to.ease, to.customCurve);
        Vector2 position = Vector2.LerpUnclamped(RetargetPosition(from), RetargetPosition(to), easedT);
        float angle = Mathf.LerpAngle(from.localEulerAngles.z, to.localEulerAngles.z, easedT);
        return new PreviewPose(position, angle);
    }

    private Vector2 RetargetPosition(WeaponMotionKeyframe frame)
    {
        return RetargetPosition(new Vector2(frame.localPositionX, frame.localPositionY));
    }

    private Vector2 RetargetPosition(Vector2 localPosition)
    {
        Vector2 reference = sequence.ReferenceTargetOffset;
        Vector2 weight = sequence.RetargetScaleWeight;
        return new Vector2(
            localPosition.x * ResolveRetargetScale(reference.x, previewTargetOffset.x, weight.x),
            localPosition.y * ResolveRetargetScale(reference.y, previewTargetOffset.y, weight.y));
    }

    private Vector2 InverseRetargetPosition(Vector2 previewPosition)
    {
        Vector2 reference = sequence.ReferenceTargetOffset;
        Vector2 weight = sequence.RetargetScaleWeight;
        float xScale = ResolveRetargetScale(reference.x, previewTargetOffset.x, weight.x);
        float yScale = ResolveRetargetScale(reference.y, previewTargetOffset.y, weight.y);
        return new Vector2(
            Mathf.Abs(xScale) > 0.0001f ? previewPosition.x / xScale : previewPosition.x,
            Mathf.Abs(yScale) > 0.0001f ? previewPosition.y / yScale : previewPosition.y);
    }

    private static float ResolveRetargetScale(float referenceValue, float targetValue, float weight)
    {
        float clampedWeight = Mathf.Clamp01(weight);
        if (clampedWeight <= 0f || Mathf.Abs(referenceValue) < 0.0001f)
        {
            return 1f;
        }

        return Mathf.Lerp(1f, targetValue / referenceValue, clampedWeight);
    }

    private void DrawDiagnostics()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Diagnostics", EditorStyles.boldLabel);

        List<string> warnings = BuildDiagnostics();
        if (warnings.Count == 0)
        {
            EditorGUILayout.HelpBox("Sequence structure looks valid.", MessageType.None);
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

    private List<string> BuildDiagnostics()
    {
        List<string> warnings = new();
        IReadOnlyList<WeaponMotionKeyframe> motions = sequence != null ? sequence.MotionKeyframes : null;
        if (motions == null || motions.Count == 0)
        {
            warnings.Add("The sequence has no motion samples.");
        }
        else
        {
            if (motions[0].normalizedTime > 0.0001f)
            {
                warnings.Add("The first motion sample does not start at time 0.");
            }

            if (motions[motions.Count - 1].normalizedTime < 0.9999f)
            {
                warnings.Add("The last motion sample does not reach time 1.");
            }

            for (int i = 1; i < motions.Count; i++)
            {
                if (motions[i].normalizedTime < motions[i - 1].normalizedTime)
                {
                    warnings.Add("Motion samples are not sorted by time.");
                    break;
                }
            }
        }

        IReadOnlyList<WeaponSequenceEventKeyframe> events = sequence != null ? sequence.EventKeyframes : null;
        if (events != null)
        {
            for (int i = 1; i < events.Count; i++)
            {
                if (events[i].normalizedTime < events[i - 1].normalizedTime)
                {
                    warnings.Add("Event keyframes are not sorted by time.");
                    break;
                }
            }
        }

        if (weaponData != null && weaponData.AttackSequence != sequence)
        {
            warnings.Add("The loaded WeaponDataSO does not reference this sequence.");
        }

        return warnings;
    }

    private void EnsureSerializedObject()
    {
        if (sequence == null)
        {
            sequenceObject = null;
            motionList = null;
            eventList = null;
            return;
        }

        if (sequenceObject == null || sequenceObject.targetObject != sequence)
        {
            RebuildSerializedObjects();
        }
    }

    private void RebuildSerializedObjects()
    {
        sequenceObject = sequence != null ? new SerializedObject(sequence) : null;
        motionList = null;
        eventList = null;
        EnsureListsReady();
    }

    private void EnsureListsReady()
    {
        if (sequenceObject == null)
        {
            return;
        }

        if (motionList == null)
        {
            SerializedProperty motionFrames = sequenceObject.FindProperty("motionKeyframes");
            if (motionFrames != null)
            {
                motionList = new ReorderableList(sequenceObject, motionFrames, true, true, true, true);
                motionList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Index   Time        Position        Rotation       Ease");
                motionList.elementHeightCallback = _ => EditorGUIUtility.singleLineHeight + 6f;
                motionList.drawElementCallback = DrawMotionElement;
                motionList.onAddCallback = OnAddMotionFrame;
                motionList.onSelectCallback = list => selectedMotionIndex = list.index;
                motionList.onReorderCallback = list => selectedMotionIndex = list.index;
                motionList.onRemoveCallback = list =>
                {
                    ReorderableList.defaultBehaviours.DoRemoveButton(list);
                    selectedMotionIndex = Mathf.Clamp(list.index, 0, Mathf.Max(0, list.serializedProperty.arraySize - 1));
                };
            }
        }

        if (eventList == null)
        {
            SerializedProperty eventFrames = sequenceObject.FindProperty("eventKeyframes");
            if (eventFrames != null)
            {
                eventList = new ReorderableList(sequenceObject, eventFrames, true, true, true, true);
                eventList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Index   Time        Type        Key");
                eventList.elementHeightCallback = _ => EditorGUIUtility.singleLineHeight + 6f;
                eventList.drawElementCallback = DrawEventElement;
                eventList.onAddCallback = OnAddEventFrame;
                eventList.onSelectCallback = list => selectedEventIndex = list.index;
                eventList.onReorderCallback = list => selectedEventIndex = list.index;
                eventList.onRemoveCallback = list =>
                {
                    ReorderableList.defaultBehaviours.DoRemoveButton(list);
                    selectedEventIndex = Mathf.Clamp(list.index, 0, Mathf.Max(0, list.serializedProperty.arraySize - 1));
                };
            }
        }
    }

    private void EnsureSelectedIndices()
    {
        SerializedProperty motions = sequenceObject.FindProperty("motionKeyframes");
        if (motions != null && motions.arraySize > 0)
        {
            selectedMotionIndex = Mathf.Clamp(selectedMotionIndex < 0 ? 0 : selectedMotionIndex, 0, motions.arraySize - 1);
        }
        else
        {
            selectedMotionIndex = -1;
        }

        SerializedProperty events = sequenceObject.FindProperty("eventKeyframes");
        if (events != null && events.arraySize > 0)
        {
            selectedEventIndex = Mathf.Clamp(selectedEventIndex < 0 ? 0 : selectedEventIndex, 0, events.arraySize - 1);
        }
        else
        {
            selectedEventIndex = -1;
        }
    }

    private void DrawMotionElement(Rect rect, int index, bool isActive, bool isFocused)
    {
        SerializedProperty element = motionList.serializedProperty.GetArrayElementAtIndex(index);
        Rect line = new(rect.x, rect.y + 3f, rect.width, EditorGUIUtility.singleLineHeight);
        float indexWidth = 34f;
        float timeWidth = 62f;
        float rotationWidth = 72f;
        float easeWidth = 86f;
        float positionWidth = Mathf.Max(92f, line.width - indexWidth - timeWidth - rotationWidth - easeWidth - 18f);

        float time = element.FindPropertyRelative("normalizedTime").floatValue;
        float x = element.FindPropertyRelative("localPositionX").floatValue;
        float y = element.FindPropertyRelative("localPositionY").floatValue;
        float angle = element.FindPropertyRelative("localEulerAngles").vector3Value.z;
        string easeName = element.FindPropertyRelative("ease").enumDisplayNames[element.FindPropertyRelative("ease").enumValueIndex];

        EditorGUI.LabelField(new Rect(line.x, line.y, indexWidth, line.height), index.ToString(), EditorStyles.miniBoldLabel);
        EditorGUI.LabelField(new Rect(line.x + indexWidth, line.y, timeWidth, line.height), time.ToString("0.00"));
        EditorGUI.LabelField(new Rect(line.x + indexWidth + timeWidth, line.y, positionWidth, line.height), $"({x:0.##}, {y:0.##})");
        EditorGUI.LabelField(new Rect(line.xMax - rotationWidth - easeWidth, line.y, rotationWidth, line.height), angle.ToString("0.#") + " deg");
        EditorGUI.LabelField(new Rect(line.xMax - easeWidth, line.y, easeWidth, line.height), easeName);
    }

    private void DrawEventElement(Rect rect, int index, bool isActive, bool isFocused)
    {
        SerializedProperty element = eventList.serializedProperty.GetArrayElementAtIndex(index);
        Rect line = new(rect.x, rect.y + 3f, rect.width, EditorGUIUtility.singleLineHeight);
        float indexWidth = 34f;
        float timeWidth = 62f;
        float keyWidth = 42f;
        float typeWidth = Mathf.Max(120f, line.width - indexWidth - timeWidth - keyWidth - 12f);

        float time = element.FindPropertyRelative("normalizedTime").floatValue;
        SerializedProperty type = element.FindPropertyRelative("eventType");
        int key = element.FindPropertyRelative("eventKey").intValue;

        EditorGUI.LabelField(new Rect(line.x, line.y, indexWidth, line.height), index.ToString(), EditorStyles.miniBoldLabel);
        EditorGUI.LabelField(new Rect(line.x + indexWidth, line.y, timeWidth, line.height), time.ToString("0.00"));
        EditorGUI.LabelField(new Rect(line.x + indexWidth + timeWidth, line.y, typeWidth, line.height), type.enumDisplayNames[type.enumValueIndex]);
        EditorGUI.LabelField(new Rect(line.xMax - keyWidth, line.y, keyWidth, line.height), key.ToString());
    }

    private void OnAddMotionFrame(ReorderableList list)
    {
        SerializedProperty frames = list.serializedProperty;
        int newIndex = frames.arraySize;
        frames.InsertArrayElementAtIndex(newIndex);
        SerializedProperty element = frames.GetArrayElementAtIndex(newIndex);
        element.FindPropertyRelative("normalizedTime").floatValue = previewNormalizedTime;
        PreviewPose pose = SamplePose(previewNormalizedTime);
        Vector2 authoredPosition = InverseRetargetPosition(pose.Position);
        element.FindPropertyRelative("localPositionX").floatValue = authoredPosition.x;
        element.FindPropertyRelative("localPositionY").floatValue = authoredPosition.y;
        element.FindPropertyRelative("localEulerAngles").vector3Value = new Vector3(0f, 0f, pose.AngleZ);
        element.FindPropertyRelative("ease").enumValueIndex = (int)WeaponMotionEase.Linear;
        element.FindPropertyRelative("customCurve").animationCurveValue = null;
        selectedMotionIndex = newIndex;
        list.index = newIndex;
    }

    private void OnAddEventFrame(ReorderableList list)
    {
        SerializedProperty frames = list.serializedProperty;
        int newIndex = frames.arraySize;
        frames.InsertArrayElementAtIndex(newIndex);
        SerializedProperty element = frames.GetArrayElementAtIndex(newIndex);
        element.FindPropertyRelative("normalizedTime").floatValue = previewNormalizedTime;
        element.FindPropertyRelative("eventType").enumValueIndex = (int)WeaponSequenceEventType.PlaySfx;
        element.FindPropertyRelative("eventKey").intValue = 0;
        selectedEventIndex = newIndex;
        list.index = newIndex;
    }

    private void AddMotionFrameAtPreviewTime()
    {
        if (motionList == null)
        {
            return;
        }

        OnAddMotionFrame(motionList);
        sequenceObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(sequence);
    }

    private void AddEventFrameAtPreviewTime()
    {
        if (eventList == null)
        {
            return;
        }

        OnAddEventFrame(eventList);
        sequenceObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(sequence);
    }

    private void SetMotionFramePreviewPosition(int index, Vector2 previewPosition)
    {
        if (sequenceObject == null)
        {
            return;
        }

        sequenceObject.Update();
        SerializedProperty frames = sequenceObject.FindProperty("motionKeyframes");
        if (frames == null || index < 0 || index >= frames.arraySize)
        {
            return;
        }

        SerializedProperty element = frames.GetArrayElementAtIndex(index);
        Vector2 authoredPosition = InverseRetargetPosition(previewPosition);
        element.FindPropertyRelative("localPositionX").floatValue = authoredPosition.x;
        element.FindPropertyRelative("localPositionY").floatValue = authoredPosition.y;
        previewNormalizedTime = element.FindPropertyRelative("normalizedTime").floatValue;
        selectedMotionIndex = index;
        sequenceObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(sequence);
    }

    private int FindMotionMarkerAtCanvas(Rect rect, Vector2 mousePosition)
    {
        IReadOnlyList<WeaponMotionKeyframe> frames = sequence != null ? sequence.MotionKeyframes : null;
        if (frames == null || frames.Count == 0)
        {
            return -1;
        }

        if (selectedMotionIndex >= 0 && selectedMotionIndex < frames.Count)
        {
            Vector2 selectedPosition = new(frames[selectedMotionIndex].localPositionX, frames[selectedMotionIndex].localPositionY);
            if (Vector2.Distance(WorldToCanvas(RetargetPosition(selectedPosition), rect), mousePosition) <= MOTION_MARKER_HIT_RADIUS)
            {
                return selectedMotionIndex;
            }
        }

        int hitIndex = -1;
        float closestDistance = MOTION_MARKER_HIT_RADIUS;
        for (int i = 0; i < frames.Count; i++)
        {
            Vector2 localPosition = new(frames[i].localPositionX, frames[i].localPositionY);
            float distance = Vector2.Distance(WorldToCanvas(RetargetPosition(localPosition), rect), mousePosition);
            if (distance <= closestDistance)
            {
                closestDistance = distance;
                hitIndex = i;
            }
        }

        return hitIndex;
    }

    private void AssignTargets(AttackSequenceDefinitionSO targetSequence, WeaponDataSO targetWeapon)
    {
        sequence = targetSequence;
        weaponData = targetWeapon;
        if (sequence == null && weaponData != null)
        {
            sequence = weaponData.AttackSequence;
        }

        if (sequence != null)
        {
            previewTargetOffset = sequence.ReferenceTargetOffset;
        }

        selectedMotionIndex = -1;
        selectedEventIndex = -1;
        RebuildSerializedObjects();
        Repaint();
    }

    private void TryLoadTargetsFromSelection(bool replaceExisting)
    {
        Object activeObject = Selection.activeObject;
        if (activeObject is WeaponDataSO selectedWeapon)
        {
            AssignTargets(selectedWeapon.AttackSequence, selectedWeapon);
            return;
        }

        if (activeObject is AttackSequenceDefinitionSO selectedSequence)
        {
            AssignTargets(selectedSequence, replaceExisting ? null : weaponData);
        }
    }

    private void DrawSequenceProperty(string propertyName, string label)
    {
        SerializedProperty property = sequenceObject.FindProperty(propertyName);
        if (property != null)
        {
            EditorGUILayout.PropertyField(property, new GUIContent(label));
        }
    }

    private void ApplySelectedPreset()
    {
        if (sequence == null)
        {
            return;
        }

        sequenceObject.ApplyModifiedProperties();
        Undo.RecordObject(sequence, "Apply Attack Sequence Preset");
        WeaponAnimationSequencePresets.ApplyPreset(sequence, selectedPreset);
        EditorUtility.SetDirty(sequence);
        selectedMotionIndex = 0;
        selectedEventIndex = 0;
        RebuildSerializedObjects();
    }

    private void SortMotionFramesByTime()
    {
        if (sequenceObject == null || sequence == null)
        {
            return;
        }

        Undo.RecordObject(sequence, "Sort Motion Samples By Time");
        sequenceObject.Update();
        SerializedProperty frames = sequenceObject.FindProperty("motionKeyframes");
        if (frames == null || frames.arraySize <= 1)
        {
            return;
        }

        List<WeaponMotionKeyframe> sortedFrames = new();
        for (int i = 0; i < frames.arraySize; i++)
        {
            SerializedProperty frame = frames.GetArrayElementAtIndex(i);
            WeaponMotionKeyframe snapshot = new WeaponMotionKeyframe
            {
                normalizedTime = frame.FindPropertyRelative("normalizedTime").floatValue,
                localPositionX = frame.FindPropertyRelative("localPositionX").floatValue,
                localPositionY = frame.FindPropertyRelative("localPositionY").floatValue,
                localEulerAngles = frame.FindPropertyRelative("localEulerAngles").vector3Value,
                ease = (WeaponMotionEase)frame.FindPropertyRelative("ease").enumValueIndex,
                customCurve = CloneCurve(frame.FindPropertyRelative("customCurve").animationCurveValue)
            };
            sortedFrames.Add(snapshot);
        }

        sortedFrames.Sort((left, right) => left.normalizedTime.CompareTo(right.normalizedTime));
        for (int i = 0; i < sortedFrames.Count; i++)
        {
            SerializedProperty frame = frames.GetArrayElementAtIndex(i);
            WeaponMotionKeyframe snapshot = sortedFrames[i];
            frame.FindPropertyRelative("normalizedTime").floatValue = Mathf.Clamp01(snapshot.normalizedTime);
            frame.FindPropertyRelative("localPositionX").floatValue = snapshot.localPositionX;
            frame.FindPropertyRelative("localPositionY").floatValue = snapshot.localPositionY;
            frame.FindPropertyRelative("localEulerAngles").vector3Value = snapshot.localEulerAngles;
            frame.FindPropertyRelative("ease").enumValueIndex = (int)snapshot.ease;
            frame.FindPropertyRelative("customCurve").animationCurveValue = snapshot.customCurve;
        }

        selectedMotionIndex = Mathf.Clamp(selectedMotionIndex, 0, frames.arraySize - 1);
        sequenceObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(sequence);
        RebuildSerializedObjects();
    }

    private void SortEventFramesByTime()
    {
        if (sequenceObject == null || sequence == null)
        {
            return;
        }

        Undo.RecordObject(sequence, "Sort Sequence Events By Time");
        sequenceObject.Update();
        SerializedProperty frames = sequenceObject.FindProperty("eventKeyframes");
        if (frames == null || frames.arraySize <= 1)
        {
            return;
        }

        List<WeaponSequenceEventKeyframe> sortedFrames = new();
        for (int i = 0; i < frames.arraySize; i++)
        {
            SerializedProperty frame = frames.GetArrayElementAtIndex(i);
            sortedFrames.Add(new WeaponSequenceEventKeyframe
            {
                normalizedTime = frame.FindPropertyRelative("normalizedTime").floatValue,
                eventType = (WeaponSequenceEventType)frame.FindPropertyRelative("eventType").enumValueIndex,
                eventKey = frame.FindPropertyRelative("eventKey").intValue
            });
        }

        sortedFrames.Sort((left, right) => left.normalizedTime.CompareTo(right.normalizedTime));
        for (int i = 0; i < sortedFrames.Count; i++)
        {
            SerializedProperty frame = frames.GetArrayElementAtIndex(i);
            WeaponSequenceEventKeyframe snapshot = sortedFrames[i];
            frame.FindPropertyRelative("normalizedTime").floatValue = Mathf.Clamp01(snapshot.normalizedTime);
            frame.FindPropertyRelative("eventType").enumValueIndex = (int)snapshot.eventType;
            frame.FindPropertyRelative("eventKey").intValue = Mathf.Max(0, snapshot.eventKey);
        }

        selectedEventIndex = Mathf.Clamp(selectedEventIndex, 0, frames.arraySize - 1);
        sequenceObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(sequence);
        RebuildSerializedObjects();
    }

    private static AnimationCurve CloneCurve(AnimationCurve source)
    {
        return source != null ? new AnimationCurve(source.keys) : null;
    }

    private float ResolveLeftColumnWidth()
    {
        return Mathf.Clamp(position.width * 0.18f, MIN_LEFT_PANEL_WIDTH, MAX_LEFT_PANEL_WIDTH);
    }

    private float ResolveMiddleColumnWidth()
    {
        float availableWidth = Mathf.Max(position.width, minSize.x);
        float leftWidth = ResolveLeftColumnWidth();
        float preferredMiddleWidth = Mathf.Clamp(availableWidth * 0.32f, MIN_MIDDLE_PANEL_WIDTH, MAX_MIDDLE_PANEL_WIDTH);
        float maxMiddleWidth = Mathf.Max(MIN_MIDDLE_PANEL_WIDTH, availableWidth - leftWidth - MIN_RIGHT_PANEL_WIDTH - COLUMN_GAP * 2f);
        return Mathf.Min(preferredMiddleWidth, maxMiddleWidth);
    }

    private float ResolveRightColumnWidth()
    {
        float availableWidth = Mathf.Max(position.width, minSize.x);
        float leftWidth = ResolveLeftColumnWidth();
        float middleWidth = ResolveMiddleColumnWidth();
        float toolbarAndBorderAllowance = 18f;
        return Mathf.Max(MIN_RIGHT_PANEL_WIDTH, availableWidth - leftWidth - middleWidth - COLUMN_GAP * 2f - toolbarAndBorderAllowance);
    }

    private Vector2 WorldToCanvas(Vector2 world, Rect rect)
    {
        Vector2 origin = ResolveCanvasOrigin(rect);
        return new Vector2(origin.x + world.x * previewPixelsPerUnit, origin.y - world.y * previewPixelsPerUnit);
    }

    private Vector2 CanvasToWorld(Vector2 canvasPosition, Rect rect)
    {
        Vector2 origin = ResolveCanvasOrigin(rect);
        return new Vector2(
            (canvasPosition.x - origin.x) / Mathf.Max(1f, previewPixelsPerUnit),
            (origin.y - canvasPosition.y) / Mathf.Max(1f, previewPixelsPerUnit));
    }

    private Vector2 ResolveCanvasOrigin(Rect rect)
    {
        return new Vector2(rect.center.x, rect.yMax - rect.height * 0.28f) + previewCanvasPan;
    }

    private static void DrawGuiLine(Vector2 start, Vector2 end, Color color, float width)
    {
        Handles.color = color;
        Handles.DrawAAPolyLine(width, new Vector3(start.x, start.y, 0f), new Vector3(end.x, end.y, 0f));
    }

    private static void DrawArrow(Vector2 origin, float angleDegrees, float length, Color color)
    {
        Vector2 direction = Rotate(Vector2.up, angleDegrees).normalized;
        Vector2 end = origin + direction * length;
        DrawGuiLine(origin, end, color, 2f);
        DrawGuiLine(end, end + Rotate(-direction, 28f) * 10f, color, 2f);
        DrawGuiLine(end, end + Rotate(-direction, -28f) * 10f, color, 2f);
    }

    private void DrawRotatedRect(Rect canvas, Vector2 center, Vector2 size, float angle, Color color)
    {
        Vector2 halfSize = size * 0.5f;
        Vector2 a = center + Rotate(new Vector2(-halfSize.x, -halfSize.y), angle);
        Vector2 b = center + Rotate(new Vector2(-halfSize.x, halfSize.y), angle);
        Vector2 c = center + Rotate(new Vector2(halfSize.x, halfSize.y), angle);
        Vector2 d = center + Rotate(new Vector2(halfSize.x, -halfSize.y), angle);
        DrawGuiLine(WorldToCanvas(a, canvas), WorldToCanvas(b, canvas), color, 2f);
        DrawGuiLine(WorldToCanvas(b, canvas), WorldToCanvas(c, canvas), color, 2f);
        DrawGuiLine(WorldToCanvas(c, canvas), WorldToCanvas(d, canvas), color, 2f);
        DrawGuiLine(WorldToCanvas(d, canvas), WorldToCanvas(a, canvas), color, 2f);
    }

    private static Vector2 Rotate(Vector2 value, float angleDegrees)
    {
        float radians = angleDegrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);
        return new Vector2(value.x * cos - value.y * sin, value.x * sin + value.y * cos);
    }

    private static float EvaluateEase(float t, WeaponMotionEase ease, AnimationCurve customCurve)
    {
        switch (ease)
        {
            case WeaponMotionEase.InSine:
                return 1f - Mathf.Cos(t * Mathf.PI * 0.5f);
            case WeaponMotionEase.OutSine:
                return Mathf.Sin(t * Mathf.PI * 0.5f);
            case WeaponMotionEase.InOutSine:
                return -(Mathf.Cos(Mathf.PI * t) - 1f) * 0.5f;
            case WeaponMotionEase.InQuad:
                return t * t;
            case WeaponMotionEase.OutQuad:
                return 1f - (1f - t) * (1f - t);
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
                if (t <= 0f) return 0f;
                if (t >= 1f) return 1f;
                return t < 0.5f ? Mathf.Pow(2f, 20f * t - 10f) * 0.5f : (2f - Mathf.Pow(2f, -20f * t + 10f)) * 0.5f;
            case WeaponMotionEase.OutBack:
                const float C1 = 1.70158f;
                const float C3 = C1 + 1f;
                float p = t - 1f;
                return 1f + C3 * p * p * p + C1 * p * p;
            case WeaponMotionEase.OutElastic:
                if (t <= 0f) return 0f;
                if (t >= 1f) return 1f;
                const float C4 = (2f * Mathf.PI) / 3f;
                return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * C4) + 1f;
            case WeaponMotionEase.CustomCurve:
                return customCurve != null ? customCurve.Evaluate(t) : t;
            default:
                return t;
        }
    }

    private readonly struct PreviewPose
    {
        public Vector2 Position { get; }
        public float AngleZ { get; }

        public PreviewPose(Vector2 position, float angleZ)
        {
            Position = position;
            AngleZ = angleZ;
        }
    }

    private enum PreviewDragMode
    {
        None,
        Target,
        MotionSample,
        Timeline,
        Pan
    }
}

[CustomEditor(typeof(WeaponDataSO))]
internal sealed class WeaponDataSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space(8f);
        if (GUILayout.Button("Open Attack Sequence Studio"))
        {
            AttackSequenceStudioWindow.Open((WeaponDataSO)target);
        }
    }
}
#endif
