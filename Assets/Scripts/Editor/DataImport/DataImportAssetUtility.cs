#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class DataImportAssetUtility
{
    public static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
        string folderName = Path.GetFileName(folderPath);
        if (string.IsNullOrWhiteSpace(parent))
        {
            return;
        }

        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folderName);
    }

    public static IReadOnlyList<TAsset> LoadAssets<TAsset>(string folder)
        where TAsset : Object
    {
        if (!AssetDatabase.IsValidFolder(folder))
        {
            return System.Array.Empty<TAsset>();
        }

        string[] guids = AssetDatabase.FindAssets($"t:{typeof(TAsset).Name}", new[] { folder });
        List<TAsset> assets = new();
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            TAsset asset = AssetDatabase.LoadAssetAtPath<TAsset>(path);
            if (asset != null)
            {
                assets.Add(asset);
            }
        }

        return assets;
    }

    public static void SetString(SerializedObject serializedObject, string propertyName, string value)
    {
        FindRequiredProperty(serializedObject, propertyName).stringValue = value ?? string.Empty;
    }

    public static void SetEnum<TEnum>(SerializedObject serializedObject, string propertyName, TEnum value)
        where TEnum : System.Enum
    {
        FindRequiredProperty(serializedObject, propertyName).intValue = System.Convert.ToInt32(value);
    }

    public static SerializedProperty FindRequiredProperty(SerializedObject serializedObject, string propertyName)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            throw new DataImportException(
                $"{serializedObject.targetObject.name} is missing serialized property '{propertyName}'.");
        }

        return property;
    }

    public static SerializedProperty FindRequiredProperty(SerializedProperty parentProperty, string relativePropertyName)
    {
        SerializedProperty property = parentProperty.FindPropertyRelative(relativePropertyName);
        if (property == null)
        {
            throw new DataImportException(
                $"{parentProperty.serializedObject.targetObject.name} is missing serialized property '{parentProperty.propertyPath}.{relativePropertyName}'.");
        }

        return property;
    }

    public static string ToSafeAssetFileName(string value)
    {
        string normalized = string.IsNullOrWhiteSpace(value) ? "Imported Asset" : value.Trim();
        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            normalized = normalized.Replace(invalid, '_');
        }

        return normalized;
    }
}
#endif
