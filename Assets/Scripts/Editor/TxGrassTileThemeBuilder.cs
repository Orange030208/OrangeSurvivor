#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class TxGrassTileThemeBuilder
{
    private const string TileFolderPath = "Assets/Resources/Tiles";
    private const string ThemeAssetPath = "Assets/Resources/Tiles/TX Grass Ground Theme.asset";
    private const int DefaultGroundTileIndex = 0;
    private const int DefaultWallTileIndex = 128;

    private static readonly int[] WhiteFlowerTileIndices =
    {
        27, 28, 29, 43, 56, 57, 74, 90, 91, 93, 120, 121, 124, 127
    };

    private static readonly int[] YellowFlowerTileIndices =
    {
        45, 79, 88, 106, 107, 122, 123
    };

    private static readonly int[] BaseExcludedTileIndices =
    {
        8, 9, 12, 24, 25, 27, 28, 29, 41, 43, 45, 56, 57, 59,
        74, 79, 88, 90, 91, 93, 106, 107, 120, 121, 122, 123, 124, 127
    };

    private static readonly int[] TuftTileIndices =
    {
        2, 4, 10, 20, 22, 23, 30, 31, 35, 37, 38, 40, 46, 50, 55, 58,
        60, 63, 65, 67, 69, 71, 73, 75, 77, 82, 84, 92, 98, 99, 101,
        103, 109, 111, 112, 113, 114, 115, 116, 118, 119, 126
    };

    [MenuItem("Tools/Tilemap/Create Or Update TX Grass Theme")]
    public static void CreateOrUpdateTheme()
    {
        MapGroundThemeSO theme = CreateOrUpdateThemeAsset();
        Selection.activeObject = theme;
        Debug.Log($"TX Grass ground theme is ready at {ThemeAssetPath}");
    }

    [MenuItem("Tools/Tilemap/Apply TX Grass Theme To Active Scene Map")]
    public static void ApplyThemeToActiveSceneMap()
    {
        MapGroundThemeSO theme = CreateOrUpdateThemeAsset();
        MapGenerator mapGenerator = Object.FindFirstObjectByType<MapGenerator>();
        if (mapGenerator == null)
        {
            Selection.activeObject = theme;
            Debug.LogWarning("No MapGenerator was found in the active scene. The theme asset was created but not applied.");
            return;
        }

        SerializedObject serializedObject = new(mapGenerator);
        serializedObject.FindProperty("groundTheme").objectReferenceValue = theme;
        serializedObject.FindProperty("groundTile").objectReferenceValue = LoadTile(DefaultGroundTileIndex);
        serializedObject.FindProperty("wallTile").objectReferenceValue = LoadTile(DefaultWallTileIndex);
        serializedObject.ApplyModifiedPropertiesWithoutUndo();

        mapGenerator.Regenerate();
        EditorUtility.SetDirty(mapGenerator);
        EditorSceneManager.MarkSceneDirty(mapGenerator.gameObject.scene);
        Selection.activeObject = mapGenerator.gameObject;

        Debug.Log("Applied TX Grass theme to the active scene MapGenerator and regenerated the tilemap.");
    }

    private static MapGroundThemeSO CreateOrUpdateThemeAsset()
    {
        MapGroundThemeSO theme = AssetDatabase.LoadAssetAtPath<MapGroundThemeSO>(ThemeAssetPath);
        if (theme == null)
        {
            theme = ScriptableObject.CreateInstance<MapGroundThemeSO>();
            AssetDatabase.CreateAsset(theme, ThemeAssetPath);
        }

        HashSet<int> excludedBaseIndices = new(BaseExcludedTileIndices);

        List<int> baseIndices = new();
        for (int index = 0; index < 128; index++)
        {
            if (!excludedBaseIndices.Contains(index))
            {
                baseIndices.Add(index);
            }
        }

        SerializedObject serializedObject = new(theme);
        serializedObject.FindProperty("fallbackGroundTile").objectReferenceValue = LoadTile(DefaultGroundTileIndex);
        serializedObject.FindProperty("fallbackWallTile").objectReferenceValue = LoadTile(DefaultWallTileIndex);
        serializedObject.FindProperty("tuftRegionWeight").floatValue = 0.4f;
        serializedObject.FindProperty("whiteFlowerRegionWeight").floatValue = 0.08f;
        serializedObject.FindProperty("yellowFlowerRegionWeight").floatValue = 0.03f;
        serializedObject.FindProperty("macroNoiseScale").floatValue = 0.055f;
        serializedObject.FindProperty("microNoiseScale").floatValue = 0.16f;
        serializedObject.FindProperty("tuftBlendChance").floatValue = 0.72f;
        serializedObject.FindProperty("whiteFlowerThreshold").floatValue = 0.82f;
        serializedObject.FindProperty("whiteFlowerMinSpacing").intValue = 3;
        serializedObject.FindProperty("yellowFlowerThreshold").floatValue = 0.9f;
        serializedObject.FindProperty("yellowFlowerMinSpacing").intValue = 4;
        SetTileArray(serializedObject.FindProperty("baseTiles"), baseIndices);
        SetTileArray(serializedObject.FindProperty("tuftTiles"), TuftTileIndices);
        SetTileArray(serializedObject.FindProperty("whiteFlowerTiles"), WhiteFlowerTileIndices);
        SetTileArray(serializedObject.FindProperty("yellowFlowerTiles"), YellowFlowerTileIndices);
        serializedObject.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(theme);
        AssetDatabase.SaveAssets();
        return theme;
    }

    private static void SetTileArray(SerializedProperty property, IReadOnlyList<int> tileIndices)
    {
        property.arraySize = tileIndices.Count;
        for (int i = 0; i < tileIndices.Count; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = LoadTile(tileIndices[i]);
        }
    }

    private static TileBase LoadTile(int tileIndex)
    {
        string assetPath = $"{TileFolderPath}/TX Tileset Grass_{tileIndex}.asset";
        return AssetDatabase.LoadAssetAtPath<TileBase>(assetPath);
    }
}
#endif
