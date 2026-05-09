using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 批量把属性图标写入 PropPresentationCatalogSO。
/// 图标文件默认按 PropType 命名，允许带尺寸后缀，例如 ShopPriceDiscount_64x64.png。
/// </summary>
public static class PropPresentationCatalogIconBinder
{
    private const string CATALOG_PATH = "Assets/ScriptableObjects/Content/Prop Presentation Catalog.asset";
    private const string ICON_FOLDER = "Assets/Resources/Sprites/Icons";

    [MenuItem("Survivors/Presentation/Bind Prop Presentation Icons")]
    public static void BindDefaultCatalogIcons()
    {
        PropPresentationCatalogSO catalog = AssetDatabase.LoadAssetAtPath<PropPresentationCatalogSO>(CATALOG_PATH);
        if (catalog == null)
        {
            Debug.LogError($"{nameof(PropPresentationCatalogIconBinder)} could not find catalog at {CATALOG_PATH}.");
            return;
        }

        int changedCount = BindIcons(catalog, ICON_FOLDER);
        Debug.Log($"{nameof(PropPresentationCatalogIconBinder)} bound {changedCount} prop icons from {ICON_FOLDER}.");
    }

    private static int BindIcons(PropPresentationCatalogSO catalog, string iconFolder)
    {
        Dictionary<string, Sprite> iconsByName = BuildIconMap(iconFolder);
        SerializedObject serializedCatalog = new SerializedObject(catalog);
        SerializedProperty entries = serializedCatalog.FindProperty("entries");
        int changedCount = 0;

        for (int i = 0; i < entries.arraySize; i++)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(i);
            SerializedProperty propTypeProperty = entry.FindPropertyRelative("propType");
            SerializedProperty iconProperty = entry.FindPropertyRelative("icon");

            string propTypeName = ((PropType)propTypeProperty.enumValueIndex).ToString();
            if (!iconsByName.TryGetValue(propTypeName, out Sprite icon))
            {
                Debug.LogWarning($"No prop icon named {propTypeName} was found under {iconFolder}.");
                continue;
            }

            if (iconProperty.objectReferenceValue == icon)
            {
                continue;
            }

            iconProperty.objectReferenceValue = icon;
            changedCount++;
        }

        serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return changedCount;
    }

    private static Dictionary<string, Sprite> BuildIconMap(string iconFolder)
    {
        Dictionary<string, Sprite> iconsByName = new Dictionary<string, Sprite>(StringComparer.Ordinal);
        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { iconFolder });

        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (icon == null)
            {
                continue;
            }

            string normalizedName = NormalizeIconName(Path.GetFileNameWithoutExtension(assetPath));
            if (iconsByName.ContainsKey(normalizedName))
            {
                Debug.LogWarning($"Duplicate prop icon name {normalizedName} found at {assetPath}. Keeping the first one.");
                continue;
            }

            iconsByName.Add(normalizedName, icon);
        }

        return iconsByName;
    }

    private static string NormalizeIconName(string iconName)
    {
        int suffixIndex = iconName.LastIndexOf('_');
        if (suffixIndex < 0)
        {
            return iconName;
        }

        string suffix = iconName.Substring(suffixIndex + 1);
        return IsSizeSuffix(suffix) ? iconName.Substring(0, suffixIndex) : iconName;
    }

    private static bool IsSizeSuffix(string suffix)
    {
        string[] parts = suffix.Split('x');
        return parts.Length == 2 &&
               int.TryParse(parts[0], out _) &&
               int.TryParse(parts[1], out _);
    }
}
