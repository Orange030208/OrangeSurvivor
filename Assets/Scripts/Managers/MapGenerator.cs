using UnityEngine;
using UnityEngine.Tilemaps;

public class MapGenerator : MonoBehaviour
{
    private enum GroundRegion
    {
        Base = 0,
        Tuft = 1,
        WhiteFlower = 2,
        YellowFlower = 3
    }

    private static bool hasRuntimeBounds;
    private static Bounds runtimeBounds;

    [Header("Map Size")]
    [SerializeField] private int mapWidth = 30;
    [SerializeField] private int mapHeight = 30;
    [SerializeField] private float cellSize = 1f;

    [Header("Map Structure")]
    [SerializeField] private Grid targetGrid;
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Tilemap wallTilemap;

    [Header("Tile Resources")]
    [SerializeField] private TileBase groundTile;
    [SerializeField] private TileBase wallTile;
    [SerializeField] private MapGroundThemeSO groundTheme;
    [SerializeField] private int tileSeed = 20260426;
    [SerializeField] private bool fillWalls = true;

    private bool hasGenerated;

    public static bool HasRuntimeBounds => hasRuntimeBounds;
    public static Bounds RuntimeBounds => runtimeBounds;
    public Vector2 MapSize => new(mapWidth * cellSize, mapHeight * cellSize);

    public void GenerateIfNeeded()
    {
        if (hasGenerated)
        {
            PublishRuntimeBounds();
            return;
        }

        ValidateReferences();
        ClearGeneratedTiles();
        BuildRuntimeMap();
        hasGenerated = true;
        PublishRuntimeBounds();
    }

    public void Regenerate()
    {
        hasGenerated = false;
        ValidateReferences();
        ClearGeneratedTiles();
        BuildRuntimeMap();
        hasGenerated = true;
        PublishRuntimeBounds();
    }

    public static bool TryGetRuntimeBounds(out Bounds bounds)
    {
        bounds = runtimeBounds;
        return hasRuntimeBounds;
    }

    [ContextMenu("Regenerate Map")]
    private void RegenerateFromContextMenu()
    {
        Regenerate();
    }

    [ContextMenu("Randomize Tile Seed And Regenerate")]
    private void RandomizeTileSeedAndRegenerate()
    {
        tileSeed = Random.Range(-900000000, 900000000);
        Regenerate();
    }

    private void BuildRuntimeMap()
    {
        mapWidth = Mathf.Max(1, mapWidth);
        mapHeight = Mathf.Max(1, mapHeight);
        cellSize = Mathf.Max(0.1f, cellSize);

        if (targetGrid != null)
        {
            targetGrid.cellSize = new Vector3(cellSize, cellSize, 1f);
        }

        FillGroundTiles();

        if (fillWalls)
        {
            FillWallTiles();
        }

        groundTilemap.CompressBounds();
        if (wallTilemap != null)
        {
            wallTilemap.CompressBounds();
        }
    }

    private void FillGroundTiles()
    {
        int startX = -mapWidth / 2;
        int startY = -mapHeight / 2;

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                int cellX = startX + x;
                int cellY = startY + y;
                Vector3Int cellPosition = new(cellX, cellY, 0);
                groundTilemap.SetTile(cellPosition, ResolveGroundTile(cellX, cellY));
            }
        }
    }

    private TileBase ResolveGroundTile(int cellX, int cellY)
    {
        TileBase fallbackGroundTile = GetResolvedGroundFallbackTile();
        if (!HasStyledGroundTheme())
        {
            return fallbackGroundTile;
        }

        switch (ResolveGroundRegion(cellX, cellY))
        {
            case GroundRegion.YellowFlower:
                if (CanPlaceAccent(cellX, cellY, groundTheme.YellowFlowerThreshold, groundTheme.YellowFlowerMinSpacing, 701))
                {
                    return PickThemeTile(groundTheme.YellowFlowerTiles, cellX, cellY, 711, ResolveBaseTile(cellX, cellY, true));
                }

                return ResolveBaseTile(cellX, cellY, true);

            case GroundRegion.WhiteFlower:
                if (CanPlaceAccent(cellX, cellY, groundTheme.WhiteFlowerThreshold, groundTheme.WhiteFlowerMinSpacing, 601))
                {
                    return PickThemeTile(groundTheme.WhiteFlowerTiles, cellX, cellY, 611, ResolveBaseTile(cellX, cellY, true));
                }

                return ResolveBaseTile(cellX, cellY, true);

            case GroundRegion.Tuft:
                return ResolveBaseTile(cellX, cellY, true);

            default:
                return ResolveBaseTile(cellX, cellY, false);
        }
    }

    private TileBase ResolveBaseTile(int cellX, int cellY, bool preferTuft)
    {
        TileBase fallbackGroundTile = GetResolvedGroundFallbackTile();
        if (!HasStyledGroundTheme())
        {
            return fallbackGroundTile;
        }

        if (groundTheme.HasTuftTiles && ShouldUseTuftTile(cellX, cellY, preferTuft))
        {
            return PickThemeTile(groundTheme.TuftTiles, cellX, cellY, 521, fallbackGroundTile);
        }

        return PickThemeTile(groundTheme.BaseTiles, cellX, cellY, 431, fallbackGroundTile);
    }

    private GroundRegion ResolveGroundRegion(int cellX, int cellY)
    {
        if (!HasStyledGroundTheme())
        {
            return GroundRegion.Base;
        }

        float regionScore = SampleRegionScore(cellX, cellY, groundTheme.MacroNoiseScale, 211);
        float yellowThreshold = 1f - groundTheme.YellowFlowerRegionWeight;
        float whiteThreshold = yellowThreshold - groundTheme.WhiteFlowerRegionWeight;
        float tuftThreshold = whiteThreshold - groundTheme.TuftRegionWeight;

        if (groundTheme.HasYellowFlowerTiles && regionScore >= yellowThreshold)
        {
            return GroundRegion.YellowFlower;
        }

        if (groundTheme.HasWhiteFlowerTiles && regionScore >= whiteThreshold)
        {
            return GroundRegion.WhiteFlower;
        }

        if (groundTheme.HasTuftTiles && regionScore >= tuftThreshold)
        {
            return GroundRegion.Tuft;
        }

        return GroundRegion.Base;
    }

    private bool ShouldUseTuftTile(int cellX, int cellY, bool preferTuft)
    {
        if (groundTheme == null || !groundTheme.HasTuftTiles)
        {
            return false;
        }

        float tuftScore = SampleAccentScore(cellX, cellY, 331);
        float threshold = preferTuft
            ? Mathf.Lerp(0.7f, 0.28f, groundTheme.TuftBlendChance)
            : Mathf.Lerp(0.92f, 0.52f, groundTheme.TuftBlendChance);

        return tuftScore >= threshold;
    }

    private bool CanPlaceAccent(int cellX, int cellY, float threshold, int minSpacing, int offset)
    {
        float currentScore = SampleAccentScore(cellX, cellY, offset);
        if (currentScore < threshold)
        {
            return false;
        }

        int spacing = Mathf.Max(0, minSpacing);
        for (int x = -spacing; x <= spacing; x++)
        {
            for (int y = -spacing; y <= spacing; y++)
            {
                if (x == 0 && y == 0)
                {
                    continue;
                }

                if (Mathf.Abs(x) + Mathf.Abs(y) > spacing + 1)
                {
                    continue;
                }

                float otherScore = SampleAccentScore(cellX + x, cellY + y, offset);
                if (otherScore > currentScore)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private float SampleRegionScore(int cellX, int cellY, float scale, int offset)
    {
        float primary = SamplePerlin(cellX, cellY, scale, offset);
        float secondary = SamplePerlin(cellX, cellY, scale * 0.53f, offset + 37);
        float hash = Hash01(cellX, cellY, offset + 79);
        return Mathf.Clamp01(primary * 0.72f + secondary * 0.2f + hash * 0.08f);
    }

    private float SampleAccentScore(int cellX, int cellY, int offset)
    {
        float scale = groundTheme != null ? groundTheme.MicroNoiseScale : 0.2f;
        float primary = SamplePerlin(cellX, cellY, scale, offset);
        float secondary = SamplePerlin(cellX, cellY, scale * 1.91f, offset + 23);
        float hash = Hash01(cellX, cellY, offset + 67);
        return Mathf.Clamp01(primary * 0.58f + secondary * 0.27f + hash * 0.15f);
    }

    private float SamplePerlin(int cellX, int cellY, float scale, int offset)
    {
        float safeScale = Mathf.Max(0.0001f, scale);
        float seedOffset = tileSeed * 0.00131f;
        float sampleX = (cellX + offset * 0.173f + seedOffset) * safeScale;
        float sampleY = (cellY - offset * 0.217f - seedOffset) * safeScale;
        return Mathf.PerlinNoise(sampleX, sampleY);
    }

    private float Hash01(int cellX, int cellY, int offset)
    {
        unchecked
        {
            int hash = tileSeed;
            hash = (hash * 486187739) ^ (cellX * 73856093);
            hash = (hash * 16777619) ^ (cellY * 19349663);
            hash = (hash * 374761393) ^ (offset * 668265263);
            hash ^= hash >> 13;
            hash *= 1274126177;
            hash ^= hash >> 16;
            return (uint)hash / (float)uint.MaxValue;
        }
    }

    private TileBase PickThemeTile(TileBase[] tiles, int cellX, int cellY, int offset, TileBase fallbackTile)
    {
        if (tiles == null || tiles.Length == 0)
        {
            return fallbackTile;
        }

        int rawIndex = Mathf.FloorToInt(Hash01(cellX, cellY, offset) * tiles.Length);
        int index = Mathf.Clamp(rawIndex, 0, tiles.Length - 1);
        TileBase pickedTile = tiles[index];
        if (pickedTile != null)
        {
            return pickedTile;
        }

        for (int i = 0; i < tiles.Length; i++)
        {
            if (tiles[i] != null)
            {
                return tiles[i];
            }
        }

        return fallbackTile;
    }

    private void FillWallTiles()
    {
        TileBase resolvedWallTile = GetResolvedWallTile();
        if (wallTilemap == null || resolvedWallTile == null)
        {
            return;
        }

        int startX = -mapWidth / 2;
        int startY = -mapHeight / 2;
        int endX = startX + mapWidth - 1;
        int endY = startY + mapHeight - 1;

        for (int x = startX; x <= endX; x++)
        {
            wallTilemap.SetTile(new Vector3Int(x, startY, 0), resolvedWallTile);
            wallTilemap.SetTile(new Vector3Int(x, endY, 0), resolvedWallTile);
        }

        for (int y = startY + 1; y < endY; y++)
        {
            wallTilemap.SetTile(new Vector3Int(startX, y, 0), resolvedWallTile);
            wallTilemap.SetTile(new Vector3Int(endX, y, 0), resolvedWallTile);
        }
    }

    private void ClearGeneratedTiles()
    {
        if (groundTilemap != null)
        {
            groundTilemap.ClearAllTiles();
        }

        if (wallTilemap != null)
        {
            wallTilemap.ClearAllTiles();
        }
    }

    private void PublishRuntimeBounds()
    {
        Vector2 size = MapSize;
        runtimeBounds = new Bounds(Vector3.zero, new Vector3(size.x, size.y, 0f));
        hasRuntimeBounds = true;
    }

    private void ValidateReferences()
    {
        if (groundTilemap == null)
        {
            throw new MissingReferenceException($"{nameof(MapGenerator)} requires a ground {nameof(Tilemap)} from the map prefab.");
        }

        if (GetResolvedGroundFallbackTile() == null)
        {
            throw new MissingReferenceException($"{nameof(MapGenerator)} requires a ground {nameof(TileBase)} assignment or a {nameof(MapGroundThemeSO)} with at least one ground tile.");
        }

        if (fillWalls)
        {
            if (wallTilemap == null)
            {
                throw new MissingReferenceException($"{nameof(MapGenerator)} requires a wall {nameof(Tilemap)} when wall filling is enabled.");
            }

            if (GetResolvedWallTile() == null)
            {
                throw new MissingReferenceException($"{nameof(MapGenerator)} requires a wall {nameof(TileBase)} assignment or a {nameof(MapGroundThemeSO)} with a wall fallback tile when wall filling is enabled.");
            }
        }
    }

    private TileBase GetResolvedGroundFallbackTile()
    {
        if (groundTheme != null)
        {
            return groundTheme.GetGroundFallbackOrDefault(groundTile);
        }

        return groundTile;
    }

    private TileBase GetResolvedWallTile()
    {
        if (groundTheme != null)
        {
            return groundTheme.GetWallFallbackOrDefault(wallTile);
        }

        return wallTile;
    }

    private bool HasStyledGroundTheme()
    {
        return groundTheme != null && groundTheme.HasGroundTiles;
    }

    private void OnValidate()
    {
        mapWidth = Mathf.Max(1, mapWidth);
        mapHeight = Mathf.Max(1, mapHeight);
        cellSize = Mathf.Max(0.1f, cellSize);
    }
}
