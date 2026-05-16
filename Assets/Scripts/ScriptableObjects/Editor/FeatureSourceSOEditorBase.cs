#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public abstract class FeatureSourceSOEditorBase : Editor
{
    private ReorderableList featureList;

    protected abstract string FeatureListPropertyName { get; }
    protected abstract string FeatureListHeader { get; }

    protected virtual void OnEnable()
    {
        SerializedProperty featureProperty = serializedObject.FindProperty(FeatureListPropertyName);
        if (featureProperty != null)
        {
            featureList = CreateFeatureList(featureProperty, FeatureListHeader);
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPropertiesExcluding(serializedObject, "m_Script", FeatureListPropertyName);
        EditorGUILayout.Space(4f);

        featureList?.DoLayoutList();

        serializedObject.ApplyModifiedProperties();
    }

    private ReorderableList CreateFeatureList(SerializedProperty property, string header)
    {
        ReorderableList list = new(serializedObject, property, true, true, true, true);
        list.drawHeaderCallback = rect => EditorGUI.LabelField(rect, header);
        list.elementHeightCallback = index => GetManagedReferenceElementHeight(property.GetArrayElementAtIndex(index));
        list.drawElementCallback = (rect, index, _, _) => DrawManagedReferenceElement(rect, property.GetArrayElementAtIndex(index), index);
        list.onAddDropdownCallback = (buttonRect, _) =>
        {
            GenericMenu menu = new();
            List<Type> featureTypes = GetConcreteFeatureTypes();
            foreach (Type featureType in featureTypes)
            {
                string menuName = ObjectNames.NicifyVariableName(featureType.Name);
                menu.AddItem(new GUIContent(menuName), false, () =>
                {
                    int index = property.arraySize;
                    property.InsertArrayElementAtIndex(index);
                    property.GetArrayElementAtIndex(index).managedReferenceValue = Activator.CreateInstance(featureType);
                    serializedObject.ApplyModifiedProperties();
                });
            }

            if (featureTypes.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("No selectable FeatureEffectBase types found"));
            }

            menu.DropDown(buttonRect);
        };
        return list;
    }

    private static float GetManagedReferenceElementHeight(SerializedProperty element)
    {
        float height = EditorGUIUtility.singleLineHeight + 6f;
        if (element == null || string.IsNullOrEmpty(element.managedReferenceFullTypename))
        {
            return height;
        }

        foreach (SerializedProperty child in EnumerateVisibleChildren(element))
        {
            if (IsHiddenInInspector(child))
            {
                continue;
            }

            height += EditorGUI.GetPropertyHeight(child, true) + 2f;
        }

        return height;
    }

    private static void DrawManagedReferenceElement(Rect rect, SerializedProperty element, int index)
    {
        rect.y += 2f;
        string label = GetManagedReferenceTypeName(element);
        if (string.IsNullOrEmpty(label))
        {
            label = $"Element {index}";
        }

        Rect headerRect = new(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.LabelField(headerRect, label, EditorStyles.boldLabel);
        if (element == null || string.IsNullOrEmpty(element.managedReferenceFullTypename))
        {
            return;
        }

        float y = headerRect.yMax + 2f;
        foreach (SerializedProperty child in EnumerateVisibleChildren(element))
        {
            if (IsHiddenInInspector(child))
            {
                continue;
            }

            float h = EditorGUI.GetPropertyHeight(child, true);
            EditorGUI.PropertyField(new Rect(rect.x, y, rect.width, h), child, true);
            y += h + 2f;
        }
    }

    private static IEnumerable<SerializedProperty> EnumerateVisibleChildren(SerializedProperty property)
    {
        SerializedProperty iterator = property.Copy();
        SerializedProperty end = iterator.GetEndProperty();
        bool enterChildren = true;
        while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, end))
        {
            enterChildren = false;
            yield return iterator.Copy();
        }
    }

    private static bool IsHiddenInInspector(SerializedProperty property)
    {
        Type parentType = GetManagedReferenceType(property.serializedObject, property.propertyPath);
        if (parentType == null)
        {
            return false;
        }

        FieldInfo field = parentType.GetField(property.name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return field != null && Attribute.IsDefined(field, typeof(HideInInspector), true);
    }

    private static Type GetManagedReferenceType(SerializedObject serializedObject, string propertyPath)
    {
        int markerIndex = propertyPath.IndexOf(".Array.data[", StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            return null;
        }

        string arrayPath = propertyPath.Substring(0, markerIndex);
        SerializedProperty arrayProperty = serializedObject.FindProperty(arrayPath);
        if (arrayProperty == null)
        {
            return null;
        }

        int bracketStart = propertyPath.IndexOf('[', markerIndex);
        int bracketEnd = propertyPath.IndexOf(']', bracketStart + 1);
        if (bracketStart < 0 || bracketEnd < 0)
        {
            return null;
        }

        if (!int.TryParse(propertyPath.Substring(bracketStart + 1, bracketEnd - bracketStart - 1), out int index))
        {
            return null;
        }

        if (index < 0 || index >= arrayProperty.arraySize)
        {
            return null;
        }

        SerializedProperty element = arrayProperty.GetArrayElementAtIndex(index);
        if (string.IsNullOrEmpty(element.managedReferenceFullTypename))
        {
            return null;
        }

        string[] parts = element.managedReferenceFullTypename.Split(' ');
        if (parts.Length != 2)
        {
            return null;
        }

        return Type.GetType($"{parts[1]}, {parts[0]}");
    }

    private static List<Type> GetConcreteFeatureTypes()
    {
        List<Type> types = new();
        foreach (Type type in TypeCache.GetTypesDerivedFrom<FeatureBase>())
        {
            if (type.IsAbstract || type.IsGenericType)
            {
                continue;
            }

            if (Attribute.IsDefined(type, typeof(HideInFeatureMenuAttribute), false))
            {
                continue;
            }

            if (type.GetConstructor(Type.EmptyTypes) == null)
            {
                continue;
            }

            types.Add(type);
        }

        types.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        return types;
    }

    private static string GetManagedReferenceTypeName(SerializedProperty property)
    {
        if (string.IsNullOrEmpty(property.managedReferenceFullTypename))
        {
            return null;
        }

        string[] parts = property.managedReferenceFullTypename.Split(' ');
        return parts.Length == 2 ? ObjectNames.NicifyVariableName(parts[1].Split('.').Last()) : null;
    }
}
#endif
