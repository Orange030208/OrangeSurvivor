#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BuffController))]
public class BuffControllerEditor : Editor
{
    private BuffDataSO debugBuffData;
    private bool overrideDuration;
    private BuffDurationPolicy durationPolicy = BuffDurationPolicy.Timed;
    private float durationSeconds = 5f;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        BuffController buffController = (BuffController)target;

        EditorGUILayout.Space(10f);
        EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);

        ActiveBuffSnapshot[] snapshots = buffController.BuildSnapshots();
        EditorGUILayout.LabelField("Active Buff Count", snapshots.Length.ToString());
        for (int i = 0; i < snapshots.Length; i++)
        {
            ActiveBuffSnapshot snapshot = snapshots[i];
            string durationText = snapshot.HasDuration ? $"{snapshot.RemainingDurationSeconds:0.0}s" : "常驻";
            EditorGUILayout.LabelField($"- {snapshot.DisplayName}", $"{snapshot.StackCount}/{snapshot.MaxStackCount} 层 · {durationText}");
        }

        EditorGUILayout.Space(6f);
        debugBuffData = (BuffDataSO)EditorGUILayout.ObjectField("Buff Data", debugBuffData, typeof(BuffDataSO), false);
        overrideDuration = EditorGUILayout.Toggle("Override Duration", overrideDuration);
        if (overrideDuration)
        {
            durationPolicy = (BuffDurationPolicy)EditorGUILayout.EnumPopup("Duration Policy", durationPolicy);
            durationSeconds = EditorGUILayout.FloatField("Duration Seconds", durationSeconds);
            durationSeconds = Mathf.Max(0f, durationSeconds);
        }

        using (new EditorGUI.DisabledScope(!Application.isPlaying || debugBuffData == null))
        {
            if (GUILayout.Button("Apply Buff"))
            {
                ApplyDebugBuff(buffController);
                EditorUtility.SetDirty(buffController);
            }

            if (GUILayout.Button("Remove Buff"))
            {
                buffController.RemoveBuff(debugBuffData.BuffId);
                EditorUtility.SetDirty(buffController);
            }

            if (GUILayout.Button("Remove One Stack"))
            {
                buffController.RemoveSingleStack(debugBuffData.BuffId);
                EditorUtility.SetDirty(buffController);
            }

            if (GUILayout.Button("Clear All Buffs"))
            {
                buffController.ClearAllBuffs();
                EditorUtility.SetDirty(buffController);
            }
        }

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("进入 Play Mode 后可直接在这里添加/移除 Buff，方便调试表现与叠层逻辑。", MessageType.Info);
        }
    }

    private void ApplyDebugBuff(BuffController buffController)
    {
        BuffApplyRequest request = overrideDuration
            ? new BuffApplyRequest(debugBuffData, durationPolicy, durationSeconds)
            : new BuffApplyRequest(debugBuffData);

        buffController.ApplyBuff(request);
    }
}
#endif
