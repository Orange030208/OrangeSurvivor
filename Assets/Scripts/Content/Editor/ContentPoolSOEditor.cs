#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(ContentPoolSO))]
public sealed class ContentPoolSOEditor : Editor
{
    private SerializedProperty entriesProperty;
    private readonly Dictionary<string, ReorderableList> managedReferenceLists = new();

    private void OnEnable()
    {
        entriesProperty = serializedObject.FindProperty("entries");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPropertiesExcluding(serializedObject, "m_Script", "entries");
        EditorGUILayout.Space(4f);
        DrawEntries();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawEntries()
    {
        if (entriesProperty == null)
        {
            return;
        }

        EditorGUILayout.PropertyField(entriesProperty, false);
        if (!entriesProperty.isExpanded)
        {
            return;
        }

        EditorGUI.indentLevel++;
        entriesProperty.arraySize = Mathf.Max(
            0,
            EditorGUILayout.IntField("Size", entriesProperty.arraySize));

        for (int i = 0; i < entriesProperty.arraySize; i++)
        {
            SerializedProperty entry = entriesProperty.GetArrayElementAtIndex(i);
            DrawEntry(entry, i);
        }

        EditorGUI.indentLevel--;
    }

    private void DrawEntry(SerializedProperty entry, int index)
    {
        string label = ResolveEntryLabel(entry, index);
        entry.isExpanded = EditorGUILayout.Foldout(entry.isExpanded, label, true);
        if (!entry.isExpanded)
        {
            return;
        }

        EditorGUI.indentLevel++;
        DrawEntryField(entry, "entryId");
        DrawEntryField(entry, "content");
        DrawEntryField(entry, "baseWeight");
        DrawEntryField(entry, "maxRollCount");
        DrawEntryField(entry, "maxPickCount");
        DrawEntryField(entry, "mutuallyExclusiveEntryIds");
        DrawManagedReferenceList(entry.FindPropertyRelative("metadata"), typeof(ContentEntryMetadata), "Metadata");
        DrawManagedReferenceList(entry.FindPropertyRelative("conditions"), typeof(ContentCondition), "Conditions");
        DrawManagedReferenceList(entry.FindPropertyRelative("weightRules"), typeof(ContentWeightRule), "Weight Rules");
        EditorGUI.indentLevel--;
    }

    private static void DrawEntryField(SerializedProperty entry, string relativePath)
    {
        SerializedProperty property = entry.FindPropertyRelative(relativePath);
        if (property != null)
        {
            EditorGUILayout.PropertyField(property, true);
        }
    }

    private void DrawManagedReferenceList(SerializedProperty property, Type baseType, string header)
    {
        if (property == null)
        {
            return;
        }

        ReorderableList list = GetOrCreateManagedReferenceList(property, baseType, header);
        list.DoLayoutList();
    }

    private ReorderableList GetOrCreateManagedReferenceList(SerializedProperty property, Type baseType, string header)
    {
        string key = property.propertyPath;
        if (managedReferenceLists.TryGetValue(key, out ReorderableList list))
        {
            return list;
        }

        list = new ReorderableList(serializedObject, property, true, true, true, true);
        list.drawHeaderCallback = rect => EditorGUI.LabelField(rect, header);
        list.elementHeightCallback = index => GetManagedReferenceElementHeight(
            property.GetArrayElementAtIndex(index));
        list.drawElementCallback = (rect, index, _, _) => DrawManagedReferenceElement(
            rect,
            property.GetArrayElementAtIndex(index),
            index);
        list.onAddDropdownCallback = (buttonRect, _) => ShowAddManagedReferenceMenu(
            buttonRect,
            property,
            baseType);
        managedReferenceLists.Add(key, list);
        return list;
    }

    private void ShowAddManagedReferenceMenu(Rect buttonRect, SerializedProperty property, Type baseType)
    {
        GenericMenu menu = new();
        List<Type> types = GetConcreteManagedReferenceTypes(baseType);
        foreach (Type type in types)
        {
            menu.AddItem(new GUIContent(ObjectNames.NicifyVariableName(type.Name)), false, () =>
            {
                serializedObject.Update();
                int index = property.arraySize;
                property.InsertArrayElementAtIndex(index);
                property.GetArrayElementAtIndex(index).managedReferenceValue = Activator.CreateInstance(type);
                serializedObject.ApplyModifiedProperties();
            });
        }

        if (types.Count == 0)
        {
            menu.AddDisabledItem(new GUIContent($"No selectable {baseType.Name} types found"));
        }

        menu.DropDown(buttonRect);
    }

    private static float GetManagedReferenceElementHeight(SerializedProperty element)
    {
        float height = EditorGUIUtility.singleLineHeight + 6f;
        if (!IsManagedReferenceWithType(element))
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
            label = element != null && element.propertyType == SerializedPropertyType.ManagedReference
                ? "Empty Rule"
                : $"Element {index}";
        }

        Rect headerRect = new(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.LabelField(headerRect, label, EditorStyles.boldLabel);
        if (!IsManagedReferenceWithType(element))
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

            float height = EditorGUI.GetPropertyHeight(child, true);
            EditorGUI.PropertyField(new Rect(rect.x, y, rect.width, height), child, true);
            y += height + 2f;
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

        FieldInfo field = parentType.GetField(
            property.name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return field != null && Attribute.IsDefined(field, typeof(HideInInspector), true);
    }

    private static Type GetManagedReferenceType(SerializedObject serializedObject, string propertyPath)
    {
        int markerIndex = propertyPath.LastIndexOf(".Array.data[", StringComparison.Ordinal);
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
        if (!IsManagedReferenceWithType(element))
        {
            return null;
        }

        string[] parts = element.managedReferenceFullTypename.Split(' ');
        return parts.Length == 2 ? Type.GetType($"{parts[1]}, {parts[0]}") : null;
    }

    private static List<Type> GetConcreteManagedReferenceTypes(Type baseType)
    {
        List<Type> types = new();
        foreach (Type type in TypeCache.GetTypesDerivedFrom(baseType))
        {
            if (type.IsAbstract || type.IsGenericType || type.GetConstructor(Type.EmptyTypes) == null)
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
        if (!IsManagedReferenceWithType(property))
        {
            return null;
        }

        string[] parts = property.managedReferenceFullTypename.Split(' ');
        return parts.Length == 2 ? ObjectNames.NicifyVariableName(parts[1].Split('.').Last()) : "Missing Type";
    }

    private static bool IsManagedReferenceWithType(SerializedProperty property)
    {
        return property != null &&
               property.propertyType == SerializedPropertyType.ManagedReference &&
               !string.IsNullOrEmpty(property.managedReferenceFullTypename);
    }

    private static string ResolveEntryLabel(SerializedProperty entry, int index)
    {
        SerializedProperty entryId = entry.FindPropertyRelative("entryId");
        if (entryId != null && !string.IsNullOrWhiteSpace(entryId.stringValue))
        {
            return entryId.stringValue;
        }

        SerializedProperty content = entry.FindPropertyRelative("content");
        if (content?.objectReferenceValue != null)
        {
            return content.objectReferenceValue.name;
        }

        return $"Entry {index}";
    }
}
#endif
