using System;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum MapDirection
{
    North = 0,
    East = 1,
    South = 2,
    West = 3
}

public static class MapDirectionExtensions
{
    public static MapDirection Opposite(this MapDirection direction)
    {
        return direction switch
        {
            MapDirection.North => MapDirection.South,
            MapDirection.East => MapDirection.West,
            MapDirection.South => MapDirection.North,
            MapDirection.West => MapDirection.East,
            _ => MapDirection.South
        };
    }

    public static Vector2Int ToOffset(this MapDirection direction)
    {
        return direction switch
        {
            MapDirection.North => Vector2Int.up,
            MapDirection.East => Vector2Int.right,
            MapDirection.South => Vector2Int.down,
            MapDirection.West => Vector2Int.left,
            _ => Vector2Int.zero
        };
    }
}

public enum MapTileCategory
{
    Floor = 0,
    Accent = 1,
    Decor = 2,
    Edge = 3,
    Transition = 4,
    Wall = 5
}

public enum MapGenerationAlgorithmType
{
    WaveFunctionCollapse = 0
}

[Serializable]
public struct MapSocketSet
{
    public string north;
    public string east;
    public string south;
    public string west;

    public string Get(MapDirection direction)
    {
        return direction switch
        {
            MapDirection.North => north,
            MapDirection.East => east,
            MapDirection.South => south,
            MapDirection.West => west,
            _ => null
        };
    }

    public void Set(MapDirection direction, string value)
    {
        switch (direction)
        {
            case MapDirection.North:
                north = value;
                break;
            case MapDirection.East:
                east = value;
                break;
            case MapDirection.South:
                south = value;
                break;
            case MapDirection.West:
                west = value;
                break;
        }
    }

    public MapSocketSet RotateClockwise()
    {
        return new MapSocketSet
        {
            north = west,
            east = north,
            south = east,
            west = south
        };
    }

    public static bool IsWildcard(string socket)
    {
        return string.IsNullOrEmpty(socket) || socket == "*";
    }

    public static bool Matches(string sourceSocket, string neighborSocket)
    {
        if (IsWildcard(sourceSocket) || IsWildcard(neighborSocket))
        {
            return true;
        }

        return string.Equals(sourceSocket, neighborSocket, StringComparison.Ordinal);
    }
}

[Serializable]
public class MapTileDefinition
{
    public string tileId;
    public TileBase tile;
    public float weight = 1f;
    public MapTileCategory category = MapTileCategory.Floor;
    public bool allowRotation90;
    public MapSocketSet sockets;
    public string[] tags = Array.Empty<string>();
}

[Serializable]
public class MapAdjacencyRule
{
    public string sourceTileId;
    public MapDirection direction;
    public string neighborTileId;
    public bool allowed = true;
}

[Serializable]
public class MapForcedCell
{
    public Vector2Int position;
    public string tileId;
}

[Serializable]
public class MapGenerationLayerConfig
{
    public string layerId = "Ground";
    public bool enabled = true;
    public MapGenerationAlgorithmType algorithm = MapGenerationAlgorithmType.WaveFunctionCollapse;
    public MapTileSetSO tileSet;
    public MapAdjacencyRuleSetSO adjacencyRules;
    public MapConstraintProfileSO constraints;
    public int seedOffset;
}
