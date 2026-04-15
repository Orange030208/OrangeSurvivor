#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// AttackSequenceDefinitionSO 的自定义 Inspector：
/// - 保留预设应用面板；
/// - Motion Keyframes 改成按模式显示必要字段，避免 Inspector 过载；
/// - Event Keyframes 继续保留原生列表编辑体验。
/// </summary>
[CustomEditor(typeof(AttackSequenceDefinitionSO))]
public class AttackSequenceDefinitionSOEditor : Editor
{
    private WeaponAnimationSequencePresetId selectedPreset;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPresetToolbar();
        EditorGUILayout.Space(8f);

        DrawCoreProperties();
        EditorGUILayout.Space(8f);
        DrawMotionKeyframes();
        EditorGUILayout.Space(8f);
        DrawEventKeyframes();

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Sequence Guide", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Motion Keyframes 使用归一化攻击空间：\n" +
            "• 当前项目约定武器模型默认是竖着放的，待机时沿 local +Y / transform.up 指向前方。\n" +
            "• 因此 y 表示沿武器朝向的前后伸缩，x 表示横向偏移；写动画时不要把 x 当作前伸轴。\n" +
            "• 现在 x / y 两个轴可以各自独立选择 Fixed 或 DynamicFromTarget。",
            MessageType.Info);

        EditorGUILayout.HelpBox(
            "动态位置策略说明：\n" +
            "• 当前仅保留 TowardTargetClampedRadius。\n" +
            "• 你可以让 x 轴和 y 轴分别选择是否动态。\n" +
            "• 例如：x = Fixed 保留横甩，y = Dynamic 让前伸跟目标远近变化。\n" +
            "• 也可以反过来，或者两个轴都固定。",
            MessageType.None);

        EditorGUILayout.HelpBox(
            "Event Keyframes 用来定义逻辑触发点：\n" +
            "• OpenHitWindow / CloseHitWindow：控制近战命中窗口。\n" +
            "• SpawnProjectile：发射弹射物。\n" +
            "• PlaySfx / PlayVfx：当前主要作为表现层挂钩。",
            MessageType.Info);

        EditorGUILayout.HelpBox(
            "SpawnProjectile 配置说明：\n" +
            "• ProjectileDefinition：直接引用要发射的 ProjectileDefinitionSO。\n" +
            "• SpawnPointIndex：0 通常表示默认发射点，1/2/... 表示额外枪口。\n" +
            "• FiringMode：Default / Spread / Burst / Nova。\n" +
            "• PatternConfig：控制散射数量、连发次数、Nova 数量等。\n" +
            "• 如果 ProjectileDefinition 为空，运行时会回退到 WeaponDataSO > Projectile List 的第一个可用定义。",
            MessageType.None);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawCoreProperties()
    {
        EditorGUILayout.PropertyField(serializedObject.FindProperty("duration"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("restoreDefaultPoseOnComplete"));
    }

    private void DrawMotionKeyframes()
    {
        SerializedProperty motionKeyframes = serializedObject.FindProperty("motionKeyframes");
        EditorGUILayout.LabelField("Motion Keyframes", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        motionKeyframes.isExpanded = EditorGUILayout.Foldout(motionKeyframes.isExpanded, $"Frames ({motionKeyframes.arraySize})", true);
        if (GUILayout.Button("Add Motion Frame", GUILayout.Width(140f)))
        {
            int newIndex = motionKeyframes.arraySize;
            motionKeyframes.InsertArrayElementAtIndex(newIndex);
            SerializedProperty newElement = motionKeyframes.GetArrayElementAtIndex(newIndex);
            InitializeMotionKeyframe(newElement, newIndex > 0 ? motionKeyframes.GetArrayElementAtIndex(newIndex - 1) : null);
        }
        EditorGUILayout.EndHorizontal();

        if (!motionKeyframes.isExpanded)
        {
            return;
        }

        EditorGUI.indentLevel++;
        for (int i = 0; i < motionKeyframes.arraySize; i++)
        {
            DrawMotionKeyframeElement(motionKeyframes, i);
            EditorGUILayout.Space(6f);
        }
        EditorGUI.indentLevel--;
    }

    private void DrawMotionKeyframeElement(SerializedProperty motionKeyframes, int index)
    {
        SerializedProperty element = motionKeyframes.GetArrayElementAtIndex(index);
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

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        element.isExpanded = EditorGUILayout.Foldout(element.isExpanded, BuildMotionFrameHeader(index, element), true);
        if (GUILayout.Button("Delete", GUILayout.Width(64f)))
        {
            motionKeyframes.DeleteArrayElementAtIndex(index);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return;
        }
        EditorGUILayout.EndHorizontal();

        if (element.isExpanded)
        {
            EditorGUILayout.PropertyField(normalizedTime);

            DrawAxisSection(
                "X Axis",
                xPositionMode,
                localPositionX,
                xDynamicMinNormalizedReach,
                xDynamicMaxNormalizedReach,
                "横向基础位移。X 轴如果是 Dynamic，会按 X 轴自己的 Reach 配置解算；只有 Fixed 时才直接使用这个值。");

            DrawAxisSection(
                "Y Axis",
                yPositionMode,
                localPositionY,
                yDynamicMinNormalizedReach,
                yDynamicMaxNormalizedReach,
                "前伸基础位移。Y 轴如果是 Dynamic，会按 Y 轴自己的 Reach 配置解算；只有 Fixed 时才直接使用这个值。");

            bool hasDynamicAxis = (WeaponMotionPositionMode)xPositionMode.enumValueIndex == WeaponMotionPositionMode.DynamicFromTarget ||
                                  (WeaponMotionPositionMode)yPositionMode.enumValueIndex == WeaponMotionPositionMode.DynamicFromTarget;
            if (hasDynamicAxis)
            {
                EditorGUILayout.PropertyField(dynamicPositionStrategy);
            }

            EditorGUILayout.PropertyField(localEulerAngles);
            EditorGUILayout.PropertyField(ease);
            if ((WeaponMotionEase)ease.enumValueIndex == WeaponMotionEase.CustomCurve)
            {
                EditorGUILayout.PropertyField(customCurve);
            }
        }

        EditorGUILayout.EndVertical();
    }

    private static string BuildMotionFrameHeader(int index, SerializedProperty element)
    {
        WeaponMotionPositionMode xMode = (WeaponMotionPositionMode)element.FindPropertyRelative("xPositionMode").enumValueIndex;
        WeaponMotionPositionMode yMode = (WeaponMotionPositionMode)element.FindPropertyRelative("yPositionMode").enumValueIndex;
        float normalizedTime = element.FindPropertyRelative("normalizedTime").floatValue;
        string summary = $"X:{(xMode == WeaponMotionPositionMode.Fixed ? "Fixed" : "Dynamic")}  Y:{(yMode == WeaponMotionPositionMode.Fixed ? "Fixed" : "Dynamic")}";
        return $"Motion Frame {index}  [t={normalizedTime:0.00}]  {summary}";
    }

    private static void DrawAxisSection(string label, SerializedProperty axisMode, SerializedProperty localPosition,
        SerializedProperty minReach, SerializedProperty maxReach, string fixedValueTooltip)
    {
        EditorGUILayout.Space(2f);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(axisMode, new GUIContent("Position Mode"));

        WeaponMotionPositionMode currentMode = (WeaponMotionPositionMode)axisMode.enumValueIndex;
        if (currentMode == WeaponMotionPositionMode.Fixed)
        {
            EditorGUILayout.PropertyField(localPosition, new GUIContent("Local Position", fixedValueTooltip));
        }
        else
        {
            EditorGUILayout.Slider(minReach, 0f, 1f, new GUIContent("Min Normalized Reach"));
            EditorGUILayout.Slider(maxReach, 0f, 1f, new GUIContent("Max Normalized Reach"));
        }

        EditorGUILayout.EndVertical();
    }

    private static void InitializeMotionKeyframe(SerializedProperty newElement, SerializedProperty sourceElement)
    {
        newElement.isExpanded = true;

        if (sourceElement == null)
        {
            newElement.FindPropertyRelative("normalizedTime").floatValue = 1f;
            newElement.FindPropertyRelative("xPositionMode").enumValueIndex = (int)WeaponMotionPositionMode.Fixed;
            newElement.FindPropertyRelative("yPositionMode").enumValueIndex = (int)WeaponMotionPositionMode.Fixed;
            newElement.FindPropertyRelative("localPositionX").floatValue = 0f;
            newElement.FindPropertyRelative("localPositionY").floatValue = 0f;
            newElement.FindPropertyRelative("dynamicPositionStrategy").enumValueIndex = (int)WeaponMotionDynamicPositionStrategy.None;
            newElement.FindPropertyRelative("xDynamicMinNormalizedReach").floatValue = 0f;
            newElement.FindPropertyRelative("xDynamicMaxNormalizedReach").floatValue = 0f;
            newElement.FindPropertyRelative("yDynamicMinNormalizedReach").floatValue = 0f;
            newElement.FindPropertyRelative("yDynamicMaxNormalizedReach").floatValue = 0f;
            newElement.FindPropertyRelative("localEulerAngles").vector3Value = Vector3.zero;
            newElement.FindPropertyRelative("ease").enumValueIndex = (int)WeaponMotionEase.Linear;
            newElement.FindPropertyRelative("customCurve").animationCurveValue = null;
            return;
        }

        newElement.FindPropertyRelative("normalizedTime").floatValue = Mathf.Clamp01(sourceElement.FindPropertyRelative("normalizedTime").floatValue);
        newElement.FindPropertyRelative("xPositionMode").enumValueIndex = sourceElement.FindPropertyRelative("xPositionMode").enumValueIndex;
        newElement.FindPropertyRelative("yPositionMode").enumValueIndex = sourceElement.FindPropertyRelative("yPositionMode").enumValueIndex;
        newElement.FindPropertyRelative("localPositionX").floatValue = sourceElement.FindPropertyRelative("localPositionX").floatValue;
        newElement.FindPropertyRelative("localPositionY").floatValue = sourceElement.FindPropertyRelative("localPositionY").floatValue;
        newElement.FindPropertyRelative("dynamicPositionStrategy").enumValueIndex = sourceElement.FindPropertyRelative("dynamicPositionStrategy").enumValueIndex;
        newElement.FindPropertyRelative("xDynamicMinNormalizedReach").floatValue = sourceElement.FindPropertyRelative("xDynamicMinNormalizedReach").floatValue;
        newElement.FindPropertyRelative("xDynamicMaxNormalizedReach").floatValue = sourceElement.FindPropertyRelative("xDynamicMaxNormalizedReach").floatValue;
        newElement.FindPropertyRelative("yDynamicMinNormalizedReach").floatValue = sourceElement.FindPropertyRelative("yDynamicMinNormalizedReach").floatValue;
        newElement.FindPropertyRelative("yDynamicMaxNormalizedReach").floatValue = sourceElement.FindPropertyRelative("yDynamicMaxNormalizedReach").floatValue;
        newElement.FindPropertyRelative("localEulerAngles").vector3Value = sourceElement.FindPropertyRelative("localEulerAngles").vector3Value;
        newElement.FindPropertyRelative("ease").enumValueIndex = sourceElement.FindPropertyRelative("ease").enumValueIndex;
        newElement.FindPropertyRelative("customCurve").animationCurveValue = sourceElement.FindPropertyRelative("customCurve").animationCurveValue;
    }

    private void DrawEventKeyframes()
    {
        SerializedProperty eventKeyframes = serializedObject.FindProperty("eventKeyframes");
        EditorGUILayout.PropertyField(eventKeyframes, true);
    }

    private void DrawPresetToolbar()
    {
        EditorGUILayout.LabelField("Preset Library", EditorStyles.boldLabel);
        selectedPreset = (WeaponAnimationSequencePresetId)EditorGUILayout.EnumPopup("Preset", selectedPreset);

        WeaponAnimationSequencePresetDefinition? definition = TryGetSelectedPresetDefinition();
        if (definition.HasValue)
        {
            WeaponAnimationSequencePresetData previewData = definition.Value.Builder();
            EditorGUILayout.HelpBox(
                $"将套用预设：{definition.Value.DisplayName}\n" +
                $"动作帧数：{previewData.MotionFrameCount}\n" +
                $"事件数：{previewData.EventCount}\n" +
                $"倾向：{definition.Value.TendencySummary}",
                MessageType.None);
        }

        if (GUILayout.Button("Apply Selected Preset"))
        {
            ApplySelectedPreset();
        }
    }

    private WeaponAnimationSequencePresetDefinition? TryGetSelectedPresetDefinition()
    {
        var presets = WeaponAnimationSequencePresets.GetAllPresets();
        for (int i = 0; i < presets.Count; i++)
        {
            if (presets[i].Id == selectedPreset)
            {
                return presets[i];
            }
        }

        return null;
    }

    private void ApplySelectedPreset()
    {
        AttackSequenceDefinitionSO sequence = (AttackSequenceDefinitionSO)target;
        Undo.RecordObject(sequence, "Apply Attack Sequence Preset");
        WeaponAnimationSequencePresets.ApplyPreset(sequence, selectedPreset);
        EditorUtility.SetDirty(sequence);
        serializedObject.Update();
    }
}
#endif
