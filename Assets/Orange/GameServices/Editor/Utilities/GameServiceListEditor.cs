using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Orange.GameServices.Editor
{
    internal static class GameServiceListEditor
    {
        public static void Draw(SerializedObject serializedObject, SerializedProperty servicesProperty, GUIContent label)
        {
            if (servicesProperty == null || !servicesProperty.isArray)
            {
                EditorGUILayout.HelpBox("Service list property is missing or not an array.", MessageType.Error);
                return;
            }

            servicesProperty.isExpanded = EditorGUILayout.Foldout(servicesProperty.isExpanded, label, true);
            if (!servicesProperty.isExpanded)
            {
                return;
            }

            EditorGUI.indentLevel++;
            for (int i = 0; i < servicesProperty.arraySize; i++)
            {
                SerializedProperty element = servicesProperty.GetArrayElementAtIndex(i);
                DrawServiceElement(servicesProperty, element, i);
            }

            DrawAddButton(serializedObject, servicesProperty);
            EditorGUI.indentLevel--;
        }

        private static void DrawServiceElement(SerializedProperty servicesProperty, SerializedProperty element, int index)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            element.isExpanded = EditorGUILayout.Foldout(
                element.isExpanded,
                $"{index}: {GameServiceEditorTypeUtility.GetDisplayName(element)}",
                true,
                EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(index <= 0))
            {
                if (GUILayout.Button("Up", GUILayout.Width(36f)))
                {
                    servicesProperty.MoveArrayElement(index, index - 1);
                }
            }

            using (new EditorGUI.DisabledScope(index >= servicesProperty.arraySize - 1))
            {
                if (GUILayout.Button("Down", GUILayout.Width(50f)))
                {
                    servicesProperty.MoveArrayElement(index, index + 1);
                }
            }

            if (GUILayout.Button("Remove", GUILayout.Width(64f)))
            {
                servicesProperty.DeleteArrayElementAtIndex(index);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.EndHorizontal();

            if (element.isExpanded)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(element, GUIContent.none, true);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private static void DrawAddButton(SerializedObject serializedObject, SerializedProperty servicesProperty)
        {
            if (!GUILayout.Button("Add Service"))
            {
                return;
            }

            GenericMenu menu = new GenericMenu();
            IReadOnlyList<Type> serviceTypes = GameServiceEditorTypeUtility.GetConcreteServiceTypes();
            if (serviceTypes.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("No concrete GameService types found"));
            }

            for (int i = 0; i < serviceTypes.Count; i++)
            {
                Type serviceType = serviceTypes[i];
                menu.AddItem(
                    new GUIContent(GameServiceEditorTypeUtility.GetMenuName(serviceType)),
                    false,
                    () => AddService(serializedObject, servicesProperty.propertyPath, serviceType));
            }

            menu.ShowAsContext();
        }

        private static void AddService(SerializedObject serializedObject, string propertyPath, Type serviceType)
        {
            serializedObject.Update();
            SerializedProperty servicesProperty = serializedObject.FindProperty(propertyPath);
            if (servicesProperty == null)
            {
                return;
            }

            object serviceInstance;
            try
            {
                serviceInstance = Activator.CreateInstance(serviceType, true);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return;
            }

            int index = servicesProperty.arraySize;
            servicesProperty.arraySize++;
            SerializedProperty element = servicesProperty.GetArrayElementAtIndex(index);
            element.managedReferenceValue = serviceInstance;
            element.isExpanded = true;
            serializedObject.ApplyModifiedProperties();
        }
    }
}
