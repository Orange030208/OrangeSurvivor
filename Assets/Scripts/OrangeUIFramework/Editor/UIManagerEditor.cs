using UnityEditor;
using UnityEngine;

namespace Orange.UIFramework.Editor
{
    [CustomEditor(typeof(UIManager))]
    public sealed class UIManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Runtime Diagnostics", EditorStyles.boldLabel);
                UIManager manager = (UIManager)target;

                using (new EditorGUI.DisabledScope(!Application.isPlaying))
                {
                    if (GUILayout.Button("Log Runtime Diagnostics"))
                    {
                        manager.LogRuntimeDiagnostics();
                    }
                }

                if (!Application.isPlaying)
                {
                    EditorGUILayout.HelpBox("Enter Play Mode to log the runtime snapshot.", MessageType.Info);
                }
            }
        }
    }
}
