using UnityEditor;
using UnityEngine;

namespace Orange.GameServices.Editor
{
    [CustomEditor(typeof(GameServiceProfile))]
    public sealed class GameServiceProfileEditor : UnityEditor.Editor
    {
        private SerializedProperty servicesProperty;

        private void OnEnable()
        {
            servicesProperty = serializedObject.FindProperty("services");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            GameServiceListEditor.Draw(serializedObject, servicesProperty, new GUIContent("Services"));
            serializedObject.ApplyModifiedProperties();
        }
    }
}
