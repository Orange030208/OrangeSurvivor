using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public class MapLayerTilemapBinding
{
    public string layerId = "Ground";
    public Tilemap tilemap;
}

public static class MapTilemapPainter
{
    public static void ClearBindings(IReadOnlyList<MapLayerTilemapBinding> bindings)
    {
        if (bindings == null)
        {
            return;
        }

        for (int i = 0; i < bindings.Count; i++)
        {
            Tilemap tilemap = bindings[i]?.tilemap;
            if (tilemap != null)
            {
                tilemap.ClearAllTiles();
            }
        }
    }

    public static void Paint(
        MapGenerationResult result,
        MapGenerationProfileSO profile,
        IReadOnlyList<MapLayerTilemapBinding> bindings)
    {
        if (result == null)
        {
            throw new System.ArgumentNullException(nameof(result));
        }

        if (!result.Success)
        {
            throw new System.InvalidOperationException($"Cannot paint failed map generation result: {result.FailureReason}");
        }

        Dictionary<string, Tilemap> tilemapsByLayerId = BuildTilemapLookup(bindings);
        ClearBindings(bindings);

        for (int i = 0; i < result.Layers.Count; i++)
        {
            MapLayerResult layerResult = result.Layers[i];
            if (!tilemapsByLayerId.TryGetValue(layerResult.LayerId, out Tilemap tilemap) || tilemap == null)
            {
                throw new MissingReferenceException($"No Tilemap binding exists for generated map layer '{layerResult.LayerId}'.");
            }

            if (profile == null || !profile.TryGetLayer(layerResult.LayerId, out MapGenerationLayerConfig layerConfig) || layerConfig.tileSet == null)
            {
                throw new MissingReferenceException($"No layer configuration exists for generated map layer '{layerResult.LayerId}'.");
            }

            PaintLayer(result, layerResult, layerConfig.tileSet, tilemap);
        }
    }

    public static bool HasTilemapBinding(IReadOnlyList<MapLayerTilemapBinding> bindings, string layerId)
    {
        if (bindings == null || string.IsNullOrWhiteSpace(layerId))
        {
            return false;
        }

        for (int i = 0; i < bindings.Count; i++)
        {
            MapLayerTilemapBinding binding = bindings[i];
            if (binding == null || binding.tilemap == null)
            {
                continue;
            }

            if (string.Equals(binding.layerId, layerId, System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static Dictionary<string, Tilemap> BuildTilemapLookup(IReadOnlyList<MapLayerTilemapBinding> bindings)
    {
        Dictionary<string, Tilemap> lookup = new(System.StringComparer.Ordinal);
        if (bindings == null)
        {
            return lookup;
        }

        for (int i = 0; i < bindings.Count; i++)
        {
            MapLayerTilemapBinding binding = bindings[i];
            if (binding == null || binding.tilemap == null || string.IsNullOrWhiteSpace(binding.layerId))
            {
                continue;
            }

            lookup[binding.layerId] = binding.tilemap;
        }

        return lookup;
    }

    private static void PaintLayer(
        MapGenerationResult result,
        MapLayerResult layerResult,
        MapTileSetSO tileSet,
        Tilemap tilemap)
    {
        for (int i = 0; i < layerResult.Cells.Count; i++)
        {
            MapGeneratedCell cell = layerResult.Cells[i];
            if (cell.Blocked)
            {
                continue;
            }

            if (!tileSet.TryGetTile(cell.SourceTileId, out TileBase tile) || tile == null)
            {
                throw new MissingReferenceException($"Tile '{cell.SourceTileId}' was generated for layer '{layerResult.LayerId}' but cannot be resolved from its tile set.");
            }

            Vector3Int cellPosition = new(
                result.GridOrigin.x + cell.Position.x,
                result.GridOrigin.y + cell.Position.y,
                result.GridOrigin.z);
            tilemap.SetTile(cellPosition, tile);
            ApplyCellTransform(tilemap, cellPosition, cell.RotationQuarterTurns);
        }

        tilemap.CompressBounds();
    }

    private static void ApplyCellTransform(Tilemap tilemap, Vector3Int position, int rotationQuarterTurns)
    {
        if (rotationQuarterTurns == 0)
        {
            tilemap.SetTransformMatrix(position, Matrix4x4.identity);
            return;
        }

        Quaternion rotation = Quaternion.Euler(0f, 0f, -90f * rotationQuarterTurns);
        tilemap.SetTransformMatrix(position, Matrix4x4.Rotate(rotation));
    }
}
