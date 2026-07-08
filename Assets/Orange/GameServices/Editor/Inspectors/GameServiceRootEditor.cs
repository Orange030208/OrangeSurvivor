using UnityEditor;
using UnityEngine;

namespace Orange.GameServices.Editor
{
    [CustomEditor(typeof(GameServiceRoot))]
    public sealed class GameServiceRootEditor : UnityEditor.Editor
    {
        private SerializedProperty scopeIdProperty;
        private SerializedProperty bindAsDefaultProperty;
        private SerializedProperty dontDestroyOnLoadProperty;
        private SerializedProperty profileModeProperty;
        private SerializedProperty profilesProperty;
        private SerializedProperty localServicesProperty;

        private void OnEnable()
        {
            scopeIdProperty = serializedObject.FindProperty("scopeId");
            bindAsDefaultProperty = serializedObject.FindProperty("bindAsDefault");
            dontDestroyOnLoadProperty = serializedObject.FindProperty("dontDestroyOnLoad");
            profileModeProperty = serializedObject.FindProperty("profileMode");
            profilesProperty = serializedObject.FindProperty("profiles");
            localServicesProperty = serializedObject.FindProperty("localServices");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(scopeIdProperty);
            EditorGUILayout.PropertyField(bindAsDefaultProperty);
            EditorGUILayout.PropertyField(dontDestroyOnLoadProperty);
            EditorGUILayout.PropertyField(profileModeProperty);
            EditorGUILayout.PropertyField(profilesProperty, true);
            EditorGUILayout.Space();
            GameServiceListEditor.Draw(serializedObject, localServicesProperty, new GUIContent("Local Services"));

            serializedObject.ApplyModifiedProperties();
            DrawRuntimeSnapshot();
        }

        private void DrawRuntimeSnapshot()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            GameServiceRoot root = (GameServiceRoot)target;
            if (root.Host == null)
            {
                EditorGUILayout.HelpBox("Host is not initialized.", MessageType.Info);
                return;
            }

            GameServiceSnapshot snapshot = root.Host.CaptureSnapshot();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime Snapshot", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Scope", snapshot.ScopeId);
            EditorGUILayout.LabelField("State", snapshot.State.ToString());

            for (int i = 0; i < snapshot.Services.Count; i++)
            {
                GameServiceEntrySnapshot service = snapshot.Services[i];
                EditorGUILayout.LabelField(service.ServiceType.Name, service.State.ToString());
            }

            if (snapshot.ValidationMessages.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
                for (int i = 0; i < snapshot.ValidationMessages.Count; i++)
                {
                    MessageType messageType = ToMessageType(snapshot.ValidationMessages[i].Severity);
                    EditorGUILayout.HelpBox(snapshot.ValidationMessages[i].ToString(), messageType);
                }
            }
        }

        private static MessageType ToMessageType(GameServiceValidationSeverity severity)
        {
            switch (severity)
            {
                case GameServiceValidationSeverity.Error:
                    return MessageType.Error;
                case GameServiceValidationSeverity.Warning:
                    return MessageType.Warning;
                default:
                    return MessageType.Info;
            }
        }
    }
}
