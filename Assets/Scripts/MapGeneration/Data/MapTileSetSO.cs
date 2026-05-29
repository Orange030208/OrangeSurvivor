using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "Map Tile Set", menuName = ScriptableObjectMenuPaths.MAP_TILE_SET, order = 0)]
public class MapTileSetSO : ScriptableObject
{
    public List<MapTileDefinition> tiles = new();

    public IReadOnlyList<MapTileDefinition> Tiles => tiles;

    public bool HasTiles
    {
        get
        {
            if (tiles == null)
            {
                return false;
            }

            for (int i = 0; i < tiles.Count; i++)
            {
                if (tiles[i] != null && tiles[i].tile != null)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public MapTileCatalogSnapshot CreateSnapshot()
    {
        MapTileDefinitionSnapshot[] definitions = BuildDefinitionSnapshots();
        MapTileVariantSnapshot[] variants = BuildVariantSnapshots(definitions);
        return new MapTileCatalogSnapshot(definitions, variants);
    }

    public bool TryGetTile(string tileId, out TileBase tile)
    {
        tile = null;
        if (string.IsNullOrWhiteSpace(tileId) || tiles == null)
        {
            return false;
        }

        for (int i = 0; i < tiles.Count; i++)
        {
            MapTileDefinition definition = tiles[i];
            if (definition == null)
            {
                continue;
            }

            if (!string.Equals(definition.tileId, tileId, System.StringComparison.Ordinal))
            {
                continue;
            }

            tile = definition.tile;
            return tile != null;
        }

        return false;
    }

    private MapTileDefinitionSnapshot[] BuildDefinitionSnapshots()
    {
        if (tiles == null || tiles.Count == 0)
        {
            return System.Array.Empty<MapTileDefinitionSnapshot>();
        }

        List<MapTileDefinitionSnapshot> snapshots = new(tiles.Count);
        for (int i = 0; i < tiles.Count; i++)
        {
            MapTileDefinition definition = tiles[i];
            if (definition == null || definition.tile == null)
            {
                continue;
            }

            string tileId = string.IsNullOrWhiteSpace(definition.tileId)
                ? definition.tile.name
                : definition.tileId.Trim();

            snapshots.Add(new MapTileDefinitionSnapshot(
                tileId,
                definition.weight,
                definition.category,
                definition.allowRotation90,
                definition.sockets));
        }

        return snapshots.ToArray();
    }

    private static MapTileVariantSnapshot[] BuildVariantSnapshots(IReadOnlyList<MapTileDefinitionSnapshot> definitions)
    {
        if (definitions == null || definitions.Count == 0)
        {
            return System.Array.Empty<MapTileVariantSnapshot>();
        }

        List<MapTileVariantSnapshot> snapshots = new(definitions.Count * 4);
        for (int i = 0; i < definitions.Count; i++)
        {
            MapTileDefinitionSnapshot definition = definitions[i];
            int rotationCount = definition.AllowRotation90 ? 4 : 1;
            float adjustedWeight = rotationCount > 0
                ? definition.Weight / rotationCount
                : definition.Weight;

            MapSocketSet sockets = definition.Sockets;
            for (int rotation = 0; rotation < rotationCount; rotation++)
            {
                if (rotation > 0)
                {
                    sockets = sockets.RotateClockwise();
                }

                string variantId = rotation == 0
                    ? definition.TileId
                    : $"{definition.TileId}@r{rotation * 90}";

                snapshots.Add(new MapTileVariantSnapshot(
                    definition.TileId,
                    variantId,
                    adjustedWeight,
                    definition.Category,
                    sockets,
                    rotation));
            }
        }

        return snapshots.ToArray();
    }

    private void OnValidate()
    {
        if (tiles == null)
        {
            tiles = new List<MapTileDefinition>();
            return;
        }

        for (int i = 0; i < tiles.Count; i++)
        {
            MapTileDefinition definition = tiles[i];
            if (definition == null || definition.tile == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(definition.tileId))
            {
                definition.tileId = definition.tile.name;
            }

            definition.weight = Mathf.Max(0f, definition.weight);
        }
    }
}
