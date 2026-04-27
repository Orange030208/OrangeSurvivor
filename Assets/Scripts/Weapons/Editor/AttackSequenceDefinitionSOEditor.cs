#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AttackSequenceDefinitionSO))]
public class AttackSequenceDefinitionSOEditor : Editor
{
    private WeaponAnimationSequencePresetId selectedPreset;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        if (GUILayout.Button("Open Attack Sequence Studio"))
        {
            AttackSequenceStudioWindow.Open((AttackSequenceDefinitionSO)target);
        }

        EditorGUILayout.Space(8f);
        DrawPresetToolbar();
        EditorGUILayout.Space(8f);
        DrawCoreProperties();
        EditorGUILayout.Space(8f);
        DrawMotionKeyframes();
        EditorGUILayout.Space(8f);
        DrawEventKeyframes();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawCoreProperties()
    {
        EditorGUILayout.LabelField("Sequence", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("duration"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("restoreDefaultPoseOnComplete"));

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Retarget", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("referenceTargetOffset"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("retargetScaleWeight"));
        EditorGUILayout.HelpBox(
            "When the current target local offset equals Reference Target Offset, the sampled animation plays unchanged. X/Y Scale Weight controls how strongly each axis scales toward the current target offset.",
            MessageType.Info);
    }

    private void DrawMotionKeyframes()
    {
        SerializedProperty motionKeyframes = serializedObject.FindProperty("motionKeyframes");
        EditorGUILayout.LabelField("Motion Samples", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        motionKeyframes.isExpanded = EditorGUILayout.Foldout(motionKeyframes.isExpanded, $"Samples ({motionKeyframes.arraySize})", true);
        if (GUILayout.Button("Add Sample", GUILayout.Width(120f)))
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
            EditorGUILayout.PropertyField(element.FindPropertyRelative("normalizedTime"));
            EditorGUILayout.PropertyField(element.FindPropertyRelative("localPositionX"), new GUIContent("Local Position X"));
            EditorGUILayout.PropertyField(element.FindPropertyRelative("localPositionY"), new GUIContent("Local Position Y"));
            EditorGUILayout.PropertyField(element.FindPropertyRelative("localEulerAngles"));

            SerializedProperty ease = element.FindPropertyRelative("ease");
            EditorGUILayout.PropertyField(ease);
            if ((WeaponMotionEase)ease.enumValueIndex == WeaponMotionEase.CustomCurve)
            {
                EditorGUILayout.PropertyField(element.FindPropertyRelative("customCurve"));
            }
        }

        EditorGUILayout.EndVertical();
    }

    private static string BuildMotionFrameHeader(int index, SerializedProperty element)
    {
        float normalizedTime = element.FindPropertyRelative("normalizedTime").floatValue;
        float x = element.FindPropertyRelative("localPositionX").floatValue;
        float y = element.FindPropertyRelative("localPositionY").floatValue;
        return $"Sample {index}  [t={normalizedTime:0.00}]  X:{x:0.##}  Y:{y:0.##}";
    }

    private static void InitializeMotionKeyframe(SerializedProperty newElement, SerializedProperty sourceElement)
    {
        newElement.isExpanded = true;
        if (sourceElement == null)
        {
            newElement.FindPropertyRelative("normalizedTime").floatValue = 1f;
            newElement.FindPropertyRelative("localPositionX").floatValue = 0f;
            newElement.FindPropertyRelative("localPositionY").floatValue = 0f;
            newElement.FindPropertyRelative("localEulerAngles").vector3Value = Vector3.zero;
            newElement.FindPropertyRelative("ease").enumValueIndex = (int)WeaponMotionEase.Linear;
            newElement.FindPropertyRelative("customCurve").animationCurveValue = null;
            return;
        }

        newElement.FindPropertyRelative("normalizedTime").floatValue = Mathf.Clamp01(sourceElement.FindPropertyRelative("normalizedTime").floatValue);
        newElement.FindPropertyRelative("localPositionX").floatValue = sourceElement.FindPropertyRelative("localPositionX").floatValue;
        newElement.FindPropertyRelative("localPositionY").floatValue = sourceElement.FindPropertyRelative("localPositionY").floatValue;
        newElement.FindPropertyRelative("localEulerAngles").vector3Value = sourceElement.FindPropertyRelative("localEulerAngles").vector3Value;
        newElement.FindPropertyRelative("ease").enumValueIndex = sourceElement.FindPropertyRelative("ease").enumValueIndex;
        newElement.FindPropertyRelative("customCurve").animationCurveValue = sourceElement.FindPropertyRelative("customCurve").animationCurveValue;
    }

    private void DrawEventKeyframes()
    {
        EditorGUILayout.LabelField("Sequence Events", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("eventKeyframes"), true);
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
                $"Apply preset: {definition.Value.DisplayName}\nMotion samples: {previewData.MotionFrameCount}\nEvents: {previewData.EventCount}\nSummary: {definition.Value.TendencySummary}",
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
