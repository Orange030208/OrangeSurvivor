#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HealthComponent))]
public class HealthComponentEditor : Editor
{
    private float debugAmount = 10f;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        HealthComponent healthComponent = (HealthComponent)target;

        EditorGUILayout.Space(10f);
        EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Current Health", healthComponent.CurrentHealth.ToString("0.##"));
        EditorGUILayout.LabelField("Max Health", healthComponent.MaxHealth.ToString("0.##"));

        debugAmount = EditorGUILayout.FloatField("Amount", debugAmount);
        debugAmount = Mathf.Max(0f, debugAmount);

        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            if (GUILayout.Button("Apply Debug Hit"))
            {
                Undo.RecordObject(healthComponent, "Debug Apply Hit");
                if (healthComponent.Owner != null)
                {
                    HitService.Apply(new HitRequest(
                        null,
                        healthComponent.Owner,
                        new HitSpec(debugAmount, 0f, 1f),
                        healthComponent.transform.position,
                        HitSourceKind.Direct,
                        sourcePosition: healthComponent.transform.position));
                }
                EditorUtility.SetDirty(healthComponent);
            }

            if (GUILayout.Button("Heal"))
            {
                Undo.RecordObject(healthComponent, "Debug Heal");
                healthComponent.Heal(debugAmount);
                EditorUtility.SetDirty(healthComponent);
            }
        }

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("进入 Play Mode 后可直接用这里的按钮调试命中/回血。", MessageType.Info);
        }
    }
}
#endif
