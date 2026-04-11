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
            if (GUILayout.Button("Take Damage"))
            {
                Undo.RecordObject(healthComponent, "Debug Take Damage");
                healthComponent.TakeDamage(debugAmount);
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
            EditorGUILayout.HelpBox("进入 Play Mode 后可直接用这里的按钮调试扣血/回血。", MessageType.Info);
        }
    }
}
#endif
