#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class TxNeonFloor8x8TilemapBuilder
{
    private const string SpriteAssetPath = GameContentAssetPaths.MapSprites + "/Tiles/TX Tileset Neon Floor 8x8.png";
    private const string TileFolderPath = GameContentAssetPaths.MapTiles;
    private const string TileAssetPrefix = "TX Tileset Neon Floor 8x8";
    private const string PreviewPrefabPath = GameContentAssetPaths.MapPrefabs + "/TX Neon Floor 8x8 Tilemap.prefab";
    private const int GridSize = 8;
    private const int TileSize = 64;
    private const int TileCount = GridSize * GridSize;

    [MenuItem("Tools/Tilemap/Create Or Update TX Neon Floor 8x8 Tilemap")]
    public static void CreateOrUpdateTilemap()
    {
        ConfigureTextureImporter();
        CreateOrUpdateTileAssets();
        CreateOrUpdatePreviewPrefab();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"TX Neon Floor 8x8 tilemap assets are ready at {PreviewPrefabPath}");
    }

    private static void ConfigureTextureImporter()
    {
        TextureImporter importer = AssetImporter.GetAtPath(SpriteAssetPath) as TextureImporter;
        if (importer == null)
        {
            throw new MissingReferenceException($"Texture asset was not found at {SpriteAssetPath}.");
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = TileSize;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 512;
        importer.alphaIsTransparency = false;

        List<SpriteMetaData> sprites = new(TileCount);
        for (int row = 0; row < GridSize; row++)
        {
            for (int col = 0; col < GridSize; col++)
            {
                int index = row * GridSize + col;
                sprites.Add(new SpriteMetaData
                {
                    name = $"{TileAssetPrefix}_{index}",
                    rect = new Rect(col * TileSize, (GridSize - 1 - row) * TileSize, TileSize, TileSize),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    border = Vector4.zero
                });
            }
        }

        importer.spritesheet = sprites.ToArray();
        importer.SaveAndReimport();
    }

    private static void CreateOrUpdateTileAssets()
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(SpriteAssetPath);
        Dictionary<string, Sprite> spritesByName = new();
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite sprite)
            {
                spritesByName[sprite.name] = sprite;
            }
        }

        for (int index = 0; index < TileCount; index++)
        {
            string spriteName = $"{TileAssetPrefix}_{index}";
            if (!spritesByName.TryGetValue(spriteName, out Sprite sprite))
            {
                throw new MissingReferenceException($"Sliced sprite {spriteName} was not found in {SpriteAssetPath}.");
            }

            string tilePath = BuildTilePath(index);
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, tilePath);
            }

            tile.name = spriteName;
            tile.sprite = sprite;
            tile.color = Color.white;
            tile.transform = Matrix4x4.identity;
            tile.gameObject = null;
            tile.flags = TileFlags.LockColor;
            tile.colliderType = Tile.ColliderType.None;
            EditorUtility.SetDirty(tile);
        }
    }

    private static void CreateOrUpdatePreviewPrefab()
    {
        GameObject root = new("TX Neon Floor 8x8 Tilemap Preview");
        Grid grid = root.AddComponent<Grid>();
        grid.cellSize = Vector3.one;

        GameObject ground = new("Ground");
        ground.transform.SetParent(root.transform, false);
        Tilemap tilemap = ground.AddComponent<Tilemap>();
        ground.AddComponent<TilemapRenderer>();

        for (int row = 0; row < GridSize; row++)
        {
            for (int col = 0; col < GridSize; col++)
            {
                int index = row * GridSize + col;
                tilemap.SetTile(new Vector3Int(col, GridSize - 1 - row, 0), LoadTile(index));
            }
        }

        tilemap.CompressBounds();
        PrefabUtility.SaveAsPrefabAsset(root, PreviewPrefabPath);
        Object.DestroyImmediate(root);
    }

    private static TileBase LoadTile(int tileIndex)
    {
        return AssetDatabase.LoadAssetAtPath<TileBase>(BuildTilePath(tileIndex));
    }

    private static string BuildTilePath(int tileIndex)
    {
        return $"{TileFolderPath}/{TileAssetPrefix}_{tileIndex}.asset";
    }
}
#endif
