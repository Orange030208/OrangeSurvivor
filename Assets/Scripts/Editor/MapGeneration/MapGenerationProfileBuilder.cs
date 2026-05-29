#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class MapGenerationProfileBuilder
{
    private const string DefaultLayerId = "Ground";
    private const int DefaultGeneratedMapSize = 32;
    private const string NeonFloorSamplePrefabPath = "Assets/GameContent/Map/Prefabs/TX Neon Floor 8x8 Tilemap.prefab";
    private const string NeonFloorSampleBaseName = "TX Neon Floor 8x8";

    [MenuItem("Tools/Tilemap/Map Generation/Create Profile From Selected Tilemap")]
    public static void CreateProfileFromSelectedTilemap()
    {
        if (!TryResolveSelectedTilemap(out Tilemap tilemap))
        {
            Debug.LogWarning("Select a scene GameObject containing a Tilemap before creating a map generation profile.");
            return;
        }

        CreateProfileFromTilemap(tilemap, tilemap.transform.root.name, true);
    }

    [MenuItem("Tools/Tilemap/Map Generation/Create Neon Floor 8x8 Sample Profile")]
    public static void CreateNeonFloor8x8SampleProfile()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(NeonFloorSamplePrefabPath);
        if (prefabRoot == null)
        {
            Debug.LogWarning($"Cannot load sample prefab at '{NeonFloorSamplePrefabPath}'.");
            return;
        }

        try
        {
            Tilemap tilemap = prefabRoot.GetComponentInChildren<Tilemap>();
            if (tilemap == null)
            {
                Debug.LogWarning($"Sample prefab '{NeonFloorSamplePrefabPath}' does not contain a Tilemap.");
                return;
            }

            CreateProfileFromTilemap(tilemap, NeonFloorSampleBaseName, true);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    [MenuItem("Tools/Tilemap/Map Generation/Validate Selected Profile")]
    public static void ValidateSelectedProfile()
    {
        MapGenerationProfileSO profile = Selection.activeObject as MapGenerationProfileSO;
        if (profile == null)
        {
            Debug.LogWarning("Select a MapGenerationProfileSO asset before validating.");
            return;
        }

        MapGenerationValidationResult validation = MapGenerationValidator.ValidateProfile(profile);
        if (validation.IsValid)
        {
            Debug.Log($"Map generation profile '{profile.name}' is valid.");
        }
        else
        {
            Debug.LogError(validation.Format(), profile);
        }
    }

    [MenuItem("Tools/Tilemap/Map Generation/Preview Selected Profile")]
    public static void PreviewSelectedProfile()
    {
        MapGenerationProfileSO profile = Selection.activeObject as MapGenerationProfileSO;
        if (profile == null)
        {
            Debug.LogWarning("Select a MapGenerationProfileSO asset before previewing.");
            return;
        }

        MapGenerationPipeline pipeline = new();
        MapGenerationResult result = pipeline.Generate(profile.CreateRequest());
        if (!result.Success)
        {
            Debug.LogError(result.FailureReason, profile);
            return;
        }

        Debug.Log($"Preview generation succeeded for '{profile.name}' at {result.GridSize.x}x{result.GridSize.y} with {result.Layers.Count} layer(s).");
    }

    [MenuItem("Tools/Tilemap/Map Generation/Apply Selected Profile To Active Scene MapGenerator")]
    public static void ApplySelectedProfileToActiveSceneMapGenerator()
    {
        MapGenerationProfileSO profile = Selection.activeObject as MapGenerationProfileSO;
        if (profile == null)
        {
            Debug.LogWarning("Select a MapGenerationProfileSO asset before applying.");
            return;
        }

        MapGenerator mapGenerator = Object.FindFirstObjectByType<MapGenerator>();
        if (mapGenerator == null)
        {
            Debug.LogWarning("No MapGenerator was found in the active scene.");
            return;
        }

        SerializedObject serializedObject = new(mapGenerator);
        serializedObject.FindProperty("generationProfile").objectReferenceValue = profile;

        SerializedProperty groundTilemapProperty = serializedObject.FindProperty("groundTilemap");
        Tilemap groundTilemap = groundTilemapProperty.objectReferenceValue as Tilemap;
        SerializedProperty bindingsProperty = serializedObject.FindProperty("layerTilemapBindings");
        EnsureLayerBinding(bindingsProperty, DefaultLayerId, groundTilemap);

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(mapGenerator);
        EditorSceneManager.MarkSceneDirty(mapGenerator.gameObject.scene);
        Selection.activeObject = mapGenerator.gameObject;

        Debug.Log($"Applied map generation profile '{profile.name}' to the active scene MapGenerator.");
    }

    private static void CreateProfileFromTilemap(Tilemap tilemap, string baseName, bool selectProfile)
    {
        if (tilemap == null)
        {
            Debug.LogWarning("Cannot create a map generation profile from a null Tilemap.");
            return;
        }

        if (!TryResolveOccupiedBounds(tilemap, out BoundsInt bounds))
        {
            Debug.LogWarning($"Selected Tilemap '{tilemap.name}' contains no TileBase cells.");
            return;
        }

        List<TileBase> orderedTiles = CollectTiles(tilemap, bounds);
        EnsureGenerationFolders();
        string safeBaseName = SanitizeAssetName(baseName);

        MapTileSetSO tileSet = LoadOrCreateAsset<MapTileSetSO>(
            $"{GameContentAssetPaths.MapGenerationTileSets}/{safeBaseName} Tile Set.asset");
        MapAdjacencyRuleSetSO ruleSet = LoadOrCreateAsset<MapAdjacencyRuleSetSO>(
            $"{GameContentAssetPaths.MapGenerationRules}/{safeBaseName} Adjacency Rules.asset");
        MapConstraintProfileSO constraints = LoadOrCreateAsset<MapConstraintProfileSO>(
            $"{GameContentAssetPaths.MapGenerationConstraints}/{safeBaseName} Constraints.asset");
        MapGenerationProfileSO profile = LoadOrCreateAsset<MapGenerationProfileSO>(
            $"{GameContentAssetPaths.MapGenerationProfiles}/{safeBaseName} Generation Profile.asset");

        WriteTileSet(tileSet, orderedTiles);
        WriteRuleSet(ruleSet, tilemap, bounds);
        WriteConstraints(constraints);
        WriteProfile(profile, tileSet, ruleSet, constraints, bounds);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (selectProfile)
        {
            Selection.activeObject = profile;
        }

        MapGenerationValidationResult validation = MapGenerationValidator.ValidateProfile(profile);
        if (validation.IsValid)
        {
            Debug.Log($"Created WFC map generation profile at {AssetDatabase.GetAssetPath(profile)}");
        }
        else
        {
            Debug.LogWarning($"Created WFC map generation profile with validation issues:\n{validation.Format()}", profile);
        }
    }

    private static void EnsureGenerationFolders()
    {
        EnsureFolder(GameContentAssetPaths.MapGeneration);
        EnsureFolder(GameContentAssetPaths.MapGenerationProfiles);
        EnsureFolder(GameContentAssetPaths.MapGenerationTileSets);
        EnsureFolder(GameContentAssetPaths.MapGenerationRules);
        EnsureFolder(GameContentAssetPaths.MapGenerationConstraints);
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string parent = System.IO.Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
        string folderName = System.IO.Path.GetFileName(folderPath);
        if (!string.IsNullOrWhiteSpace(parent))
        {
            EnsureFolder(parent);
        }

        AssetDatabase.CreateFolder(parent, folderName);
    }

    private static T LoadOrCreateAsset<T>(string assetPath)
        where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
        if (asset != null)
        {
            return asset;
        }

        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, assetPath);
        return asset;
    }

    private static void WriteTileSet(MapTileSetSO tileSet, IReadOnlyList<TileBase> orderedTiles)
    {
        tileSet.tiles.Clear();
        for (int i = 0; i < orderedTiles.Count; i++)
        {
            TileBase tile = orderedTiles[i];
            tileSet.tiles.Add(new MapTileDefinition
            {
                tileId = tile.name,
                tile = tile,
                weight = 1f,
                category = MapTileCategory.Floor,
                allowRotation90 = false,
                sockets = new MapSocketSet
                {
                    north = "floor",
                    east = "floor",
                    south = "floor",
                    west = "floor"
                }
            });
        }

        EditorUtility.SetDirty(tileSet);
    }

    private static void WriteRuleSet(MapAdjacencyRuleSetSO ruleSet, Tilemap tilemap, BoundsInt bounds)
    {
        ruleSet.useExplicitRules = true;
        ruleSet.useSocketCompatibility = false;
        ruleSet.allowMissingRules = false;
        ruleSet.rules.Clear();

        HashSet<MapAdjacencyRuleKey> seenRules = new();
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int sourcePosition = new(x, y, 0);
                TileBase sourceTile = tilemap.GetTile(sourcePosition);
                if (sourceTile == null)
                {
                    continue;
                }

                AddObservedRules(tilemap, bounds, sourcePosition, sourceTile, seenRules, ruleSet.rules);
            }
        }

        EditorUtility.SetDirty(ruleSet);
    }

    private static void WriteConstraints(MapConstraintProfileSO constraints)
    {
        constraints.borderPadding = 0;
        constraints.requireConnectedFloor = true;
        constraints.minimumConnectedFloorArea = 0;
        constraints.blockedRegions.Clear();
        constraints.forcedCells.Clear();
        EditorUtility.SetDirty(constraints);
    }

    private static void AddObservedRules(
        Tilemap tilemap,
        BoundsInt bounds,
        Vector3Int sourcePosition,
        TileBase sourceTile,
        ISet<MapAdjacencyRuleKey> seenRules,
        ICollection<MapAdjacencyRule> rules)
    {
        for (int i = 0; i < 4; i++)
        {
            MapDirection direction = (MapDirection)i;
            Vector2Int offset = direction.ToOffset();
            Vector3Int neighborPosition = new(sourcePosition.x + offset.x, sourcePosition.y + offset.y, sourcePosition.z);
            neighborPosition = WrapPosition(neighborPosition, bounds);
            TileBase neighborTile = tilemap.GetTile(neighborPosition);
            if (neighborTile == null)
            {
                continue;
            }

            MapAdjacencyRuleKey key = new(sourceTile.name, direction, neighborTile.name);
            if (!seenRules.Add(key))
            {
                continue;
            }

            rules.Add(new MapAdjacencyRule
            {
                sourceTileId = sourceTile.name,
                direction = direction,
                neighborTileId = neighborTile.name,
                allowed = true
            });
        }
    }

    private static void WriteProfile(
        MapGenerationProfileSO profile,
        MapTileSetSO tileSet,
        MapAdjacencyRuleSetSO ruleSet,
        MapConstraintProfileSO constraints,
        BoundsInt sampleBounds)
    {
        profile.mapWidth = Mathf.Max(DefaultGeneratedMapSize, sampleBounds.size.x * 2);
        profile.mapHeight = Mathf.Max(DefaultGeneratedMapSize, sampleBounds.size.y * 2);
        profile.cellSize = 1f;
        profile.seed = 20260523;
        profile.maxAttempts = 64;
        profile.layers.Clear();
        profile.layers.Add(new MapGenerationLayerConfig
        {
            layerId = DefaultLayerId,
            enabled = true,
            algorithm = MapGenerationAlgorithmType.WaveFunctionCollapse,
            tileSet = tileSet,
            adjacencyRules = ruleSet,
            constraints = constraints,
            seedOffset = 0
        });

        EditorUtility.SetDirty(profile);
    }

    private static List<TileBase> CollectTiles(Tilemap tilemap, BoundsInt bounds)
    {
        Dictionary<TileBase, Vector3Int> firstPositions = new();
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int position = new(x, y, 0);
                TileBase tile = tilemap.GetTile(position);
                if (tile != null && !firstPositions.ContainsKey(tile))
                {
                    firstPositions.Add(tile, position);
                }
            }
        }

        List<TileBase> orderedTiles = new(firstPositions.Keys);
        orderedTiles.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
        return orderedTiles;
    }

    private static bool TryResolveOccupiedBounds(Tilemap tilemap, out BoundsInt occupiedBounds)
    {
        BoundsInt rawBounds = tilemap.cellBounds;
        bool hasTile = false;
        int minX = int.MaxValue;
        int minY = int.MaxValue;
        int maxX = int.MinValue;
        int maxY = int.MinValue;

        for (int x = rawBounds.xMin; x < rawBounds.xMax; x++)
        {
            for (int y = rawBounds.yMin; y < rawBounds.yMax; y++)
            {
                if (tilemap.GetTile(new Vector3Int(x, y, 0)) == null)
                {
                    continue;
                }

                hasTile = true;
                minX = Mathf.Min(minX, x);
                minY = Mathf.Min(minY, y);
                maxX = Mathf.Max(maxX, x);
                maxY = Mathf.Max(maxY, y);
            }
        }

        if (!hasTile)
        {
            occupiedBounds = default;
            return false;
        }

        occupiedBounds = new BoundsInt(minX, minY, 0, maxX - minX + 1, maxY - minY + 1, 1);
        return true;
    }

    private static bool TryResolveSelectedTilemap(out Tilemap tilemap)
    {
        tilemap = Selection.activeGameObject != null
            ? Selection.activeGameObject.GetComponentInChildren<Tilemap>()
            : null;
        return tilemap != null;
    }

    private static Vector3Int WrapPosition(Vector3Int position, BoundsInt bounds)
    {
        int x = Wrap(position.x, bounds.xMin, bounds.xMax);
        int y = Wrap(position.y, bounds.yMin, bounds.yMax);
        return new Vector3Int(x, y, position.z);
    }

    private static int Wrap(int value, int minInclusive, int maxExclusive)
    {
        int size = maxExclusive - minInclusive;
        if (size <= 0)
        {
            return value;
        }

        int localValue = value - minInclusive;
        int wrapped = ((localValue % size) + size) % size;
        return minInclusive + wrapped;
    }

    private static void EnsureLayerBinding(SerializedProperty bindingsProperty, string layerId, Tilemap tilemap)
    {
        if (bindingsProperty == null || tilemap == null)
        {
            return;
        }

        for (int i = 0; i < bindingsProperty.arraySize; i++)
        {
            SerializedProperty element = bindingsProperty.GetArrayElementAtIndex(i);
            SerializedProperty layerIdProperty = element.FindPropertyRelative("layerId");
            if (layerIdProperty.stringValue != layerId)
            {
                continue;
            }

            element.FindPropertyRelative("tilemap").objectReferenceValue = tilemap;
            return;
        }

        int index = bindingsProperty.arraySize;
        bindingsProperty.arraySize++;
        SerializedProperty newElement = bindingsProperty.GetArrayElementAtIndex(index);
        newElement.FindPropertyRelative("layerId").stringValue = layerId;
        newElement.FindPropertyRelative("tilemap").objectReferenceValue = tilemap;
    }

    private static string SanitizeAssetName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
        {
            return "Map Generation";
        }

        string fileName = rawName.Trim();
        foreach (char invalidChar in System.IO.Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(invalidChar, '_');
        }

        return fileName;
    }
}
#endif
