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
            "• DynamicFromTarget 也使用 0~1 的归一化半径区间，再在运行时乘当前 Range。",
            MessageType.Info);

        EditorGUILayout.HelpBox(
            "动态位置策略说明：\n" +
            "• 当前仅保留 TowardTargetClampedRadius。\n" +
            "• 它会朝目标方向取距离，并把落点限制在 Min/Max Normalized Reach 对应的真实攻击半径区间内。\n" +
            "• DynamicFromTarget 下不再区分其他长度混合策略；需要不同长短效果时，直接调 Min/Max Reach 与关键帧时序。\n" +
            "• 建议把主攻击方向上的最大前伸控制在 1 以内；x 可以为负表示后拉，侧向摆动请尽量收敛。",
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
        SerializedProperty positionMode = element.FindPropertyRelative("positionMode");
        SerializedProperty localPosition = element.FindPropertyRelative("localPosition");
        SerializedProperty dynamicPositionStrategy = element.FindPropertyRelative("dynamicPositionStrategy");
        SerializedProperty dynamicMinNormalizedReach = element.FindPropertyRelative("dynamicMinNormalizedReach");
        SerializedProperty dynamicMaxNormalizedReach = element.FindPropertyRelative("dynamicMaxNormalizedReach");
        SerializedProperty dynamicWeight = element.FindPropertyRelative("dynamicWeight");
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
            EditorGUILayout.PropertyField(positionMode);

            WeaponMotionPositionMode currentMode = (WeaponMotionPositionMode)positionMode.enumValueIndex;
            if (currentMode == WeaponMotionPositionMode.Fixed)
            {
                float forwardReach = localPosition.vector3Value.y;
                if (forwardReach > 1f)
                {
                    EditorGUILayout.HelpBox($"固定帧前伸较大：当前 y = {forwardReach:0.##}。固定帧现在按本地写死坐标播放，不再受 Range 缩放；请确认这就是你想要的绝对位移。", MessageType.Warning);
                }

                EditorGUILayout.PropertyField(localPosition);
            }
            else
            {
                EditorGUILayout.PropertyField(dynamicPositionStrategy);
                EditorGUILayout.Slider(dynamicMinNormalizedReach, 0f, 1f, new GUIContent("Min Normalized Reach"));
                EditorGUILayout.Slider(dynamicMaxNormalizedReach, 0f, 1f, new GUIContent("Max Normalized Reach"));
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
        WeaponMotionPositionMode positionMode = (WeaponMotionPositionMode)element.FindPropertyRelative("positionMode").enumValueIndex;
        float normalizedTime = element.FindPropertyRelative("normalizedTime").floatValue;
        string summary = positionMode == WeaponMotionPositionMode.Fixed
            ? "Fixed"
            : BuildDynamicSummary(element);
        return $"Motion Frame {index}  [t={normalizedTime:0.00}]  {summary}";
    }

    private static string BuildDynamicSummary(SerializedProperty element)
    {
        WeaponMotionDynamicPositionStrategy strategy = (WeaponMotionDynamicPositionStrategy)element.FindPropertyRelative("dynamicPositionStrategy").enumValueIndex;
        float minReach = element.FindPropertyRelative("dynamicMinNormalizedReach").floatValue;
        float maxReach = element.FindPropertyRelative("dynamicMaxNormalizedReach").floatValue;
        return $"Dynamic/Clamp  Reach[{minReach:0.##}-{maxReach:0.##}]";
    }

    private static void InitializeMotionKeyframe(SerializedProperty newElement, SerializedProperty sourceElement)
    {
        newElement.isExpanded = true;

        if (sourceElement == null)
        {
            newElement.FindPropertyRelative("normalizedTime").floatValue = 1f;
            newElement.FindPropertyRelative("positionMode").enumValueIndex = (int)WeaponMotionPositionMode.Fixed;
            newElement.FindPropertyRelative("localPosition").vector3Value = Vector3.zero;
            newElement.FindPropertyRelative("dynamicPositionStrategy").enumValueIndex = (int)WeaponMotionDynamicPositionStrategy.None;
            newElement.FindPropertyRelative("dynamicMinNormalizedReach").floatValue = 0f;
            newElement.FindPropertyRelative("dynamicMaxNormalizedReach").floatValue = 0f;
            newElement.FindPropertyRelative("localEulerAngles").vector3Value = Vector3.zero;
            newElement.FindPropertyRelative("ease").enumValueIndex = (int)WeaponMotionEase.Linear;
            newElement.FindPropertyRelative("customCurve").animationCurveValue = null;
            return;
        }

        newElement.FindPropertyRelative("normalizedTime").floatValue = Mathf.Clamp01(sourceElement.FindPropertyRelative("normalizedTime").floatValue);
        newElement.FindPropertyRelative("positionMode").enumValueIndex = sourceElement.FindPropertyRelative("positionMode").enumValueIndex;
        newElement.FindPropertyRelative("localPosition").vector3Value = sourceElement.FindPropertyRelative("localPosition").vector3Value;
        newElement.FindPropertyRelative("dynamicPositionStrategy").enumValueIndex = sourceElement.FindPropertyRelative("dynamicPositionStrategy").enumValueIndex;
        newElement.FindPropertyRelative("dynamicMinNormalizedReach").floatValue = sourceElement.FindPropertyRelative("dynamicMinNormalizedReach").floatValue;
        newElement.FindPropertyRelative("dynamicMaxNormalizedReach").floatValue = sourceElement.FindPropertyRelative("dynamicMaxNormalizedReach").floatValue;
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
