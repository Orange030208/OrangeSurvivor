using System;
using System.Collections.Generic;
using UnityEngine;

public readonly struct MapTileDefinitionSnapshot
{
    public MapTileDefinitionSnapshot(
        string tileId,
        float weight,
        MapTileCategory category,
        bool allowRotation90,
        MapSocketSet sockets)
    {
        TileId = tileId;
        Weight = weight;
        Category = category;
        AllowRotation90 = allowRotation90;
        Sockets = sockets;
    }

    public string TileId { get; }
    public float Weight { get; }
    public MapTileCategory Category { get; }
    public bool AllowRotation90 { get; }
    public MapSocketSet Sockets { get; }
}

public readonly struct MapTileVariantSnapshot
{
    public MapTileVariantSnapshot(
        string sourceTileId,
        string variantId,
        float weight,
        MapTileCategory category,
        MapSocketSet sockets,
        int rotationQuarterTurns)
    {
        SourceTileId = sourceTileId;
        VariantId = variantId;
        Weight = weight;
        Category = category;
        Sockets = sockets;
        RotationQuarterTurns = rotationQuarterTurns;
    }

    public string SourceTileId { get; }
    public string VariantId { get; }
    public float Weight { get; }
    public MapTileCategory Category { get; }
    public MapSocketSet Sockets { get; }
    public int RotationQuarterTurns { get; }
}

public sealed class MapTileCatalogSnapshot
{
    private readonly Dictionary<string, int> definitionIndexById;
    private readonly Dictionary<string, int> variantIndexById;

    public MapTileCatalogSnapshot(MapTileDefinitionSnapshot[] definitions, MapTileVariantSnapshot[] variants)
    {
        Definitions = definitions ?? Array.Empty<MapTileDefinitionSnapshot>();
        Variants = variants ?? Array.Empty<MapTileVariantSnapshot>();
        definitionIndexById = BuildDefinitionLookup(Definitions);
        variantIndexById = BuildVariantLookup(Variants);
    }

    public IReadOnlyList<MapTileDefinitionSnapshot> Definitions { get; }
    public IReadOnlyList<MapTileVariantSnapshot> Variants { get; }

    public bool TryGetDefinitionIndex(string tileId, out int index)
    {
        return definitionIndexById.TryGetValue(tileId, out index);
    }

    public bool TryGetVariantIndex(string variantId, out int index)
    {
        return variantIndexById.TryGetValue(variantId, out index);
    }

    public bool TryGetDefinition(string tileId, out MapTileDefinitionSnapshot definition)
    {
        if (definitionIndexById.TryGetValue(tileId, out int index))
        {
            definition = Definitions[index];
            return true;
        }

        definition = default;
        return false;
    }

    public bool TryGetVariant(string variantId, out MapTileVariantSnapshot variant)
    {
        if (variantIndexById.TryGetValue(variantId, out int index))
        {
            variant = Variants[index];
            return true;
        }

        variant = default;
        return false;
    }

    private static Dictionary<string, int> BuildDefinitionLookup(IReadOnlyList<MapTileDefinitionSnapshot> definitions)
    {
        Dictionary<string, int> lookup = new(StringComparer.Ordinal);
        for (int i = 0; i < definitions.Count; i++)
        {
            MapTileDefinitionSnapshot definition = definitions[i];
            if (string.IsNullOrWhiteSpace(definition.TileId))
            {
                continue;
            }

            if (!lookup.ContainsKey(definition.TileId))
            {
                lookup.Add(definition.TileId, i);
            }
        }

        return lookup;
    }

    private static Dictionary<string, int> BuildVariantLookup(IReadOnlyList<MapTileVariantSnapshot> variants)
    {
        Dictionary<string, int> lookup = new(StringComparer.Ordinal);
        for (int i = 0; i < variants.Count; i++)
        {
            MapTileVariantSnapshot variant = variants[i];
            if (string.IsNullOrWhiteSpace(variant.VariantId))
            {
                continue;
            }

            if (!lookup.ContainsKey(variant.VariantId))
            {
                lookup.Add(variant.VariantId, i);
            }
        }

        return lookup;
    }
}

public readonly struct MapAdjacencyRuleKey : IEquatable<MapAdjacencyRuleKey>
{
    public MapAdjacencyRuleKey(string sourceTileId, MapDirection direction, string neighborTileId)
    {
        SourceTileId = sourceTileId ?? string.Empty;
        Direction = direction;
        NeighborTileId = neighborTileId ?? string.Empty;
    }

    public string SourceTileId { get; }
    public MapDirection Direction { get; }
    public string NeighborTileId { get; }

    public bool Equals(MapAdjacencyRuleKey other)
    {
        return Direction == other.Direction
            && string.Equals(SourceTileId, other.SourceTileId, StringComparison.Ordinal)
            && string.Equals(NeighborTileId, other.NeighborTileId, StringComparison.Ordinal);
    }

    public override bool Equals(object obj)
    {
        return obj is MapAdjacencyRuleKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = (hash * 31) + (int)Direction;
            hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(SourceTileId);
            hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(NeighborTileId);
            return hash;
        }
    }
}

public sealed class MapAdjacencyRuleSetSnapshot
{
    private readonly Dictionary<MapAdjacencyRuleKey, bool> explicitRules;

    private MapAdjacencyRuleSetSnapshot(bool useExplicitRules, bool useSocketCompatibility, bool allowMissingRules, Dictionary<MapAdjacencyRuleKey, bool> explicitRules)
    {
        UseExplicitRules = useExplicitRules;
        UseSocketCompatibility = useSocketCompatibility;
        AllowMissingRules = allowMissingRules;
        this.explicitRules = explicitRules ?? new Dictionary<MapAdjacencyRuleKey, bool>();
    }

    public bool UseExplicitRules { get; }
    public bool UseSocketCompatibility { get; }
    public bool AllowMissingRules { get; }

    public static MapAdjacencyRuleSetSnapshot CreateEmpty()
    {
        return new MapAdjacencyRuleSetSnapshot(false, false, false, new Dictionary<MapAdjacencyRuleKey, bool>());
    }

    public MapAdjacencyRuleSetSnapshot(bool useExplicitRules, bool useSocketCompatibility, bool allowMissingRules, IReadOnlyList<MapAdjacencyRule> rules)
        : this(useExplicitRules, useSocketCompatibility, allowMissingRules, BuildRuleLookup(rules))
    {
    }

    public bool IsCompatible(in MapTileVariantSnapshot source, MapDirection direction, in MapTileVariantSnapshot neighbor)
    {
        MapAdjacencyRuleKey key = new(source.SourceTileId, direction, neighbor.SourceTileId);
        if (explicitRules.TryGetValue(key, out bool allowed))
        {
            return allowed;
        }

        if (UseSocketCompatibility)
        {
            return MapSocketSet.Matches(
                source.Sockets.Get(direction),
                neighbor.Sockets.Get(direction.Opposite()));
        }

        return AllowMissingRules;
    }

    private static Dictionary<MapAdjacencyRuleKey, bool> BuildRuleLookup(IReadOnlyList<MapAdjacencyRule> rules)
    {
        Dictionary<MapAdjacencyRuleKey, bool> lookup = new();
        if (rules == null)
        {
            return lookup;
        }

        for (int i = 0; i < rules.Count; i++)
        {
            MapAdjacencyRule rule = rules[i];
            if (rule == null || string.IsNullOrWhiteSpace(rule.sourceTileId) || string.IsNullOrWhiteSpace(rule.neighborTileId))
            {
                continue;
            }

            lookup[new MapAdjacencyRuleKey(rule.sourceTileId, rule.direction, rule.neighborTileId)] = rule.allowed;
        }

        return lookup;
    }
}

public sealed class MapConstraintProfileSnapshot
{
    private readonly Dictionary<Vector2Int, string> forcedTileIds;
    private readonly HashSet<Vector2Int> blockedCellLookup;

    private MapConstraintProfileSnapshot(int borderPadding, bool requireConnectedFloor, int minimumConnectedFloorArea)
    {
        BorderPadding = borderPadding;
        RequireConnectedFloor = requireConnectedFloor;
        MinimumConnectedFloorArea = minimumConnectedFloorArea;
        forcedTileIds = new Dictionary<Vector2Int, string>();
        blockedCellLookup = new HashSet<Vector2Int>();
    }

    public MapConstraintProfileSnapshot(
        int borderPadding,
        bool requireConnectedFloor,
        int minimumConnectedFloorArea,
        IReadOnlyList<RectInt> blockedRegions,
        IReadOnlyList<MapForcedCell> forcedCells)
        : this(borderPadding, requireConnectedFloor, minimumConnectedFloorArea)
    {
        if (blockedRegions != null)
        {
            for (int i = 0; i < blockedRegions.Count; i++)
            {
                AddBlockedRegion(blockedRegions[i]);
            }
        }

        if (forcedCells != null)
        {
            for (int i = 0; i < forcedCells.Count; i++)
            {
                MapForcedCell cell = forcedCells[i];
                if (cell == null || string.IsNullOrWhiteSpace(cell.tileId))
                {
                    continue;
                }

                forcedTileIds[cell.position] = cell.tileId;
            }
        }
    }

    public int BorderPadding { get; }
    public bool RequireConnectedFloor { get; }
    public int MinimumConnectedFloorArea { get; }

    public static MapConstraintProfileSnapshot CreateEmpty()
    {
        return new MapConstraintProfileSnapshot(0, false, 0);
    }

    public bool IsBlocked(Vector2Int position, Vector2Int size)
    {
        if (BorderPadding > 0)
        {
            int minX = BorderPadding;
            int minY = BorderPadding;
            int maxX = Mathf.Max(0, size.x - BorderPadding - 1);
            int maxY = Mathf.Max(0, size.y - BorderPadding - 1);
            if (position.x < minX || position.y < minY || position.x > maxX || position.y > maxY)
            {
                return true;
            }
        }

        return blockedCellLookup.Contains(position);
    }

    public bool TryGetForcedTileId(Vector2Int position, out string tileId)
    {
        return forcedTileIds.TryGetValue(position, out tileId);
    }

    private void AddBlockedRegion(RectInt region)
    {
        int minX = region.xMin;
        int minY = region.yMin;
        int maxX = region.xMax;
        int maxY = region.yMax;
        for (int x = minX; x < maxX; x++)
        {
            for (int y = minY; y < maxY; y++)
            {
                blockedCellLookup.Add(new Vector2Int(x, y));
            }
        }
    }
}

public sealed class MapLayerGenerationRequest
{
    public MapLayerGenerationRequest(
        string layerId,
        MapGenerationAlgorithmType algorithm,
        int seed,
        MapTileCatalogSnapshot tileCatalog,
        MapAdjacencyRuleSetSnapshot adjacencyRules,
        MapConstraintProfileSnapshot constraints)
    {
        LayerId = layerId;
        Algorithm = algorithm;
        Seed = seed;
        TileCatalog = tileCatalog;
        AdjacencyRules = adjacencyRules;
        Constraints = constraints;
    }

    public string LayerId { get; }
    public MapGenerationAlgorithmType Algorithm { get; }
    public int Seed { get; }
    public MapTileCatalogSnapshot TileCatalog { get; }
    public MapAdjacencyRuleSetSnapshot AdjacencyRules { get; }
    public MapConstraintProfileSnapshot Constraints { get; }
}

public sealed class MapGenerationRequest
{
    public MapGenerationRequest(
        Vector2Int gridSize,
        Vector3Int gridOrigin,
        float cellSize,
        int seed,
        int maxAttempts,
        IReadOnlyList<MapLayerGenerationRequest> layers)
    {
        GridSize = gridSize;
        GridOrigin = gridOrigin;
        CellSize = cellSize;
        Seed = seed;
        MaxAttempts = maxAttempts;
        Layers = layers ?? Array.Empty<MapLayerGenerationRequest>();
    }

    public Vector2Int GridSize { get; }
    public Vector3Int GridOrigin { get; }
    public float CellSize { get; }
    public int Seed { get; }
    public int MaxAttempts { get; }
    public IReadOnlyList<MapLayerGenerationRequest> Layers { get; }
}

public readonly struct MapGeneratedCell
{
    public MapGeneratedCell(Vector2Int position, string sourceTileId, string variantId, int rotationQuarterTurns, bool blocked)
    {
        Position = position;
        SourceTileId = sourceTileId;
        VariantId = variantId;
        RotationQuarterTurns = rotationQuarterTurns;
        Blocked = blocked;
    }

    public Vector2Int Position { get; }
    public string SourceTileId { get; }
    public string VariantId { get; }
    public int RotationQuarterTurns { get; }
    public bool Blocked { get; }
}

public sealed class MapLayerResult
{
    public MapLayerResult(string layerId, Vector2Int gridSize, bool success, string failureReason, MapGeneratedCell[] cells)
    {
        LayerId = layerId;
        GridSize = gridSize;
        Success = success;
        FailureReason = failureReason;
        Cells = cells ?? Array.Empty<MapGeneratedCell>();
    }

    public string LayerId { get; }
    public Vector2Int GridSize { get; }
    public bool Success { get; }
    public string FailureReason { get; }
    public IReadOnlyList<MapGeneratedCell> Cells { get; }
}

public sealed class MapGenerationResult
{
    public MapGenerationResult(
        Vector2Int gridSize,
        Vector3Int gridOrigin,
        float cellSize,
        bool success,
        string failureReason,
        IReadOnlyList<MapLayerResult> layers)
    {
        GridSize = gridSize;
        GridOrigin = gridOrigin;
        CellSize = cellSize;
        Success = success;
        FailureReason = failureReason;
        Layers = layers ?? Array.Empty<MapLayerResult>();
    }

    public Vector2Int GridSize { get; }
    public Vector3Int GridOrigin { get; }
    public float CellSize { get; }
    public bool Success { get; }
    public string FailureReason { get; }
    public IReadOnlyList<MapLayerResult> Layers { get; }
}

public enum MapGenerationIssueSeverity
{
    Warning = 0,
    Error = 1
}

public sealed class MapGenerationIssue
{
    public MapGenerationIssue(MapGenerationIssueSeverity severity, string code, string message, string layerId = null)
    {
        Severity = severity;
        Code = code;
        Message = message;
        LayerId = layerId;
    }

    public MapGenerationIssueSeverity Severity { get; }
    public string Code { get; }
    public string Message { get; }
    public string LayerId { get; }
}

public sealed class MapGenerationValidationResult
{
    private readonly List<MapGenerationIssue> issues = new();

    public IReadOnlyList<MapGenerationIssue> Issues => issues;
    public bool IsValid => !HasErrors;
    public bool HasErrors { get; private set; }

    public void AddError(string code, string message, string layerId = null)
    {
        issues.Add(new MapGenerationIssue(MapGenerationIssueSeverity.Error, code, message, layerId));
        HasErrors = true;
    }

    public void AddWarning(string code, string message, string layerId = null)
    {
        issues.Add(new MapGenerationIssue(MapGenerationIssueSeverity.Warning, code, message, layerId));
    }

    public string Format()
    {
        if (issues.Count == 0)
        {
            return "No validation issues.";
        }

        System.Text.StringBuilder builder = new();
        for (int i = 0; i < issues.Count; i++)
        {
            MapGenerationIssue issue = issues[i];
            builder.Append('[').Append(issue.Severity).Append("] ").Append(issue.Code).Append(": ").Append(issue.Message);
            if (!string.IsNullOrWhiteSpace(issue.LayerId))
            {
                builder.Append(" (Layer: ").Append(issue.LayerId).Append(')');
            }

            if (i < issues.Count - 1)
            {
                builder.AppendLine();
            }
        }

        return builder.ToString();
    }
}
