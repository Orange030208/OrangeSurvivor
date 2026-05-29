using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// 批量把属性图标写入 PropPresentationCatalogSO。
/// 图标按 prop_icons.png 图集内的序号顺序绑定到 catalog 条目。
/// </summary>
public static class PropPresentationCatalogIconBinder
{
    private const string CATALOG_PATH = GameContentAssetPaths.PropPresentationCatalog;
    private const string ICON_ATLAS_PATH = GameContentAssetPaths.PropertyShowPropIconsAtlas;

    [MenuItem("Survivors/Presentation/Bind Prop Presentation Icons")]
    public static void BindDefaultCatalogIcons()
    {
        PropPresentationCatalogSO catalog = AssetDatabase.LoadAssetAtPath<PropPresentationCatalogSO>(CATALOG_PATH);
        if (catalog == null)
        {
            Debug.LogError($"{nameof(PropPresentationCatalogIconBinder)} could not find catalog at {CATALOG_PATH}.");
            return;
        }

        int changedCount = BindIcons(catalog, ICON_ATLAS_PATH);
        Debug.Log($"{nameof(PropPresentationCatalogIconBinder)} bound {changedCount} prop icons from {ICON_ATLAS_PATH}.");
    }

    private static int BindIcons(PropPresentationCatalogSO catalog, string iconAtlasPath)
    {
        Dictionary<int, Sprite> iconsByIndex = BuildIconMap(iconAtlasPath);
        SerializedObject serializedCatalog = new SerializedObject(catalog);
        SerializedProperty entries = serializedCatalog.FindProperty("entries");
        int changedCount = 0;

        for (int i = 0; i < entries.arraySize; i++)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(i);
            SerializedProperty iconProperty = entry.FindPropertyRelative("icon");

            if (!iconsByIndex.TryGetValue(i, out Sprite icon))
            {
                Debug.LogWarning($"No prop icon index {i} was found under {iconAtlasPath}.");
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

    private static Dictionary<int, Sprite> BuildIconMap(string iconAtlasPath)
    {
        Dictionary<int, Sprite> iconsByIndex = new Dictionary<int, Sprite>();
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(iconAtlasPath);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is not Sprite icon)
            {
                continue;
            }

            if (!TryParseAtlasIndex(icon.name, out int index))
            {
                continue;
            }

            if (iconsByIndex.ContainsKey(index))
            {
                Debug.LogWarning($"Duplicate prop icon index {index} found in {iconAtlasPath}. Keeping the first one.");
                continue;
            }

            iconsByIndex.Add(index, icon);
        }

        return iconsByIndex;
    }

    private static bool TryParseAtlasIndex(string iconName, out int index)
    {
        int suffixIndex = iconName.LastIndexOf('_');
        if (suffixIndex < 0)
        {
            index = -1;
            return false;
        }

        string suffix = iconName.Substring(suffixIndex + 1);
        return int.TryParse(suffix, out index);
    }
}
