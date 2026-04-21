using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 运行时地图生成器。
/// 地图结构（Grid / Ground Tilemap / Wall Tilemap）由外部预制体提供，
/// 本类只负责按固定尺寸填充地面和墙体瓦片，并发布统一运行时边界。
/// </summary>
public class MapGenerator : MonoBehaviour
{
    private static bool hasRuntimeBounds;
    private static Bounds runtimeBounds;

    [Header("地图尺寸")]
    [SerializeField] private int mapWidth = 30;
    [SerializeField] private int mapHeight = 30;
    [SerializeField] private float cellSize = 1f;

    [Header("地图结构（由预制体提供）")]
    [SerializeField] private Grid targetGrid;
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Tilemap wallTilemap;

    [Header("瓦片资源")]
    [SerializeField] private TileBase groundTile;
    [SerializeField] private TileBase wallTile;
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

    private void BuildRuntimeMap()
    {
        mapWidth = Mathf.Max(1, mapWidth);
        mapHeight = Mathf.Max(1, mapHeight);
        cellSize = Mathf.Max(0.1f, cellSize);

        FillGroundTiles();

        if (fillWalls && wallTilemap != null && wallTile != null)
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
                Vector3Int cellPosition = new(startX + x, startY + y, 0);
                groundTilemap.SetTile(cellPosition, groundTile);
            }
        }
    }

    private void FillWallTiles()
    {
        int startX = -mapWidth / 2;
        int startY = -mapHeight / 2;
        int endX = startX + mapWidth - 1;
        int endY = startY + mapHeight - 1;

        for (int x = startX; x <= endX; x++)
        {
            wallTilemap.SetTile(new Vector3Int(x, startY, 0), wallTile);
            wallTilemap.SetTile(new Vector3Int(x, endY, 0), wallTile);
        }

        for (int y = startY + 1; y < endY; y++)
        {
            wallTilemap.SetTile(new Vector3Int(startX, y, 0), wallTile);
            wallTilemap.SetTile(new Vector3Int(endX, y, 0), wallTile);
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

        if (groundTile == null)
        {
            throw new MissingReferenceException($"{nameof(MapGenerator)} requires a ground {nameof(TileBase)} assignment.");
        }

        if (fillWalls)
        {
            if (wallTilemap == null)
            {
                throw new MissingReferenceException($"{nameof(MapGenerator)} requires a wall {nameof(Tilemap)} when wall filling is enabled.");
            }

            if (wallTile == null)
            {
                throw new MissingReferenceException($"{nameof(MapGenerator)} requires a wall {nameof(TileBase)} assignment when wall filling is enabled.");
            }
        }
    }

    private void OnValidate()
    {
        mapWidth = Mathf.Max(1, mapWidth);
        mapHeight = Mathf.Max(1, mapHeight);
        cellSize = Mathf.Max(0.1f, cellSize);
    }
}
