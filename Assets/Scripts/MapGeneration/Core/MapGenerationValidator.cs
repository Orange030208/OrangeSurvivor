using System;
using System.Collections.Generic;
using UnityEngine;

public static class MapGenerationValidator
{
    public static MapGenerationValidationResult ValidateProfile(
        MapGenerationProfileSO profile,
        Func<string, bool> hasLayerTilemapBinding = null)
    {
        MapGenerationValidationResult result = new();
        if (profile == null)
        {
            result.AddError("MapProfileMissing", "MapGenerationProfileSO is required.");
            return result;
        }

        if (profile.mapWidth <= 0 || profile.mapHeight <= 0)
        {
            result.AddError("MapProfileInvalidSize", "Map width and height must be greater than zero.");
        }

        if (profile.cellSize <= 0f)
        {
            result.AddError("MapProfileInvalidCellSize", "Cell size must be greater than zero.");
        }

        if (profile.maxAttempts <= 0)
        {
            result.AddError("MapProfileInvalidAttempts", "WFC max attempts must be greater than zero.");
        }

        if (profile.layers == null || profile.layers.Count == 0)
        {
            result.AddError("MapProfileNoLayers", "At least one enabled map generation layer is required.");
            return result;
        }

        HashSet<string> layerIds = new(StringComparer.Ordinal);
        int enabledLayerCount = 0;
        for (int i = 0; i < profile.layers.Count; i++)
        {
            MapGenerationLayerConfig layer = profile.layers[i];
            if (layer == null || !layer.enabled)
            {
                continue;
            }

            enabledLayerCount++;
            string layerId = NormalizeLayerId(layer.layerId, i);
            if (!layerIds.Add(layerId))
            {
                result.AddError("MapProfileDuplicateLayerId", $"Layer id '{layerId}' is duplicated.", layerId);
            }

            if (hasLayerTilemapBinding != null && !hasLayerTilemapBinding(layerId))
            {
                result.AddError("MapProfileMissingTilemapBinding", $"Layer '{layerId}' has no Tilemap binding.", layerId);
            }

            ValidateLayer(profile, layer, layerId, result);
        }

        if (enabledLayerCount == 0)
        {
            result.AddError("MapProfileNoEnabledLayers", "At least one map generation layer must be enabled.");
        }

        return result;
    }

    public static MapGenerationValidationResult ValidateRequest(MapGenerationRequest request)
    {
        MapGenerationValidationResult result = new();
        if (request == null)
        {
            result.AddError("MapRequestMissing", "MapGenerationRequest is required.");
            return result;
        }

        if (request.GridSize.x <= 0 || request.GridSize.y <= 0)
        {
            result.AddError("MapRequestInvalidSize", "Request grid size must be greater than zero.");
        }

        if (request.CellSize <= 0f)
        {
            result.AddError("MapRequestInvalidCellSize", "Request cell size must be greater than zero.");
        }

        if (request.Layers == null || request.Layers.Count == 0)
        {
            result.AddError("MapRequestNoLayers", "Request must contain at least one layer.");
            return result;
        }

        for (int i = 0; i < request.Layers.Count; i++)
        {
            MapLayerGenerationRequest layer = request.Layers[i];
            if (layer == null)
            {
                result.AddError("MapRequestNullLayer", $"Layer request at index {i} is null.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(layer.LayerId))
            {
                result.AddError("MapRequestEmptyLayerId", $"Layer request at index {i} has an empty id.");
            }

            if (layer.TileCatalog == null || layer.TileCatalog.Variants.Count == 0)
            {
                result.AddError("MapRequestNoTiles", "Layer request has no tile variants.", layer.LayerId);
            }
        }

        return result;
    }

    private static void ValidateLayer(
        MapGenerationProfileSO profile,
        MapGenerationLayerConfig layer,
        string layerId,
        MapGenerationValidationResult result)
    {
        if (layer.tileSet == null)
        {
            result.AddError("MapLayerMissingTileSet", $"Layer '{layerId}' requires a MapTileSetSO.", layerId);
            return;
        }

        HashSet<string> tileIds = ValidateTileSet(layer.tileSet, layerId, result);
        if (layer.adjacencyRules == null)
        {
            result.AddError("MapLayerMissingAdjacencyRules", $"Layer '{layerId}' requires a MapAdjacencyRuleSetSO.", layerId);
            return;
        }

        ValidateAdjacencyRules(layer.adjacencyRules, tileIds, layerId, result);
        ValidateConstraints(layer.constraints, tileIds, profile.mapWidth, profile.mapHeight, layerId, result);

        if (!result.HasErrors)
        {
            ValidateCompatibility(profile, layer, layerId, result);
        }
    }

    private static HashSet<string> ValidateTileSet(MapTileSetSO tileSet, string layerId, MapGenerationValidationResult result)
    {
        HashSet<string> tileIds = new(StringComparer.Ordinal);
        if (tileSet.tiles == null || tileSet.tiles.Count == 0)
        {
            result.AddError("MapTileSetEmpty", "Tile set must contain at least one tile definition.", layerId);
            return tileIds;
        }

        int validTileCount = 0;
        for (int i = 0; i < tileSet.tiles.Count; i++)
        {
            MapTileDefinition definition = tileSet.tiles[i];
            if (definition == null)
            {
                result.AddError("MapTileSetNullTile", $"Tile definition at index {i} is null.", layerId);
                continue;
            }

            if (definition.tile == null)
            {
                result.AddError("MapTileSetMissingTileBase", $"Tile definition at index {i} has no TileBase reference.", layerId);
                continue;
            }

            if (string.IsNullOrWhiteSpace(definition.tileId))
            {
                result.AddError("MapTileSetMissingTileId", $"Tile definition for '{definition.tile.name}' has no tile id.", layerId);
                continue;
            }

            if (definition.weight < 0f)
            {
                result.AddError("MapTileSetInvalidWeight", $"Tile '{definition.tileId}' has a negative weight.", layerId);
            }

            if (!tileIds.Add(definition.tileId))
            {
                result.AddError("MapTileSetDuplicateTileId", $"Tile id '{definition.tileId}' is duplicated.", layerId);
            }

            validTileCount++;
        }

        if (validTileCount == 0)
        {
            result.AddError("MapTileSetNoValidTiles", "Tile set contains no valid tile definitions.", layerId);
        }

        return tileIds;
    }

    private static void ValidateAdjacencyRules(
        MapAdjacencyRuleSetSO ruleSet,
        HashSet<string> tileIds,
        string layerId,
        MapGenerationValidationResult result)
    {
        if (!ruleSet.useExplicitRules && !ruleSet.useSocketCompatibility && !ruleSet.allowMissingRules)
        {
            result.AddError(
                "MapRuleSetNoCompatibilitySource",
                "Adjacency rule set must use explicit rules, socket compatibility, or allow missing rules.",
                layerId);
        }

        if (ruleSet.rules == null)
        {
            return;
        }

        Dictionary<MapAdjacencyRuleKey, bool> seenRules = new();
        for (int i = 0; i < ruleSet.rules.Count; i++)
        {
            MapAdjacencyRule rule = ruleSet.rules[i];
            if (rule == null)
            {
                result.AddWarning("MapRuleSetNullRule", $"Adjacency rule at index {i} is null.", layerId);
                continue;
            }

            if (string.IsNullOrWhiteSpace(rule.sourceTileId))
            {
                result.AddError("MapRuleSetMissingSource", $"Adjacency rule at index {i} has no source tile id.", layerId);
                continue;
            }

            if (string.IsNullOrWhiteSpace(rule.neighborTileId))
            {
                result.AddError("MapRuleSetMissingNeighbor", $"Adjacency rule at index {i} has no neighbor tile id.", layerId);
                continue;
            }

            if (!tileIds.Contains(rule.sourceTileId))
            {
                result.AddError("MapRuleSetUnknownSource", $"Adjacency rule source tile '{rule.sourceTileId}' is not in the layer tile set.", layerId);
            }

            if (!tileIds.Contains(rule.neighborTileId))
            {
                result.AddError("MapRuleSetUnknownNeighbor", $"Adjacency rule neighbor tile '{rule.neighborTileId}' is not in the layer tile set.", layerId);
            }

            MapAdjacencyRuleKey key = new(rule.sourceTileId, rule.direction, rule.neighborTileId);
            if (seenRules.TryGetValue(key, out bool existingAllowed) && existingAllowed != rule.allowed)
            {
                result.AddError(
                    "MapRuleSetConflictingRule",
                    $"Adjacency rule '{rule.sourceTileId}' {rule.direction} '{rule.neighborTileId}' has conflicting allow/deny values.",
                    layerId);
            }

            seenRules[key] = rule.allowed;
        }
    }

    private static void ValidateConstraints(
        MapConstraintProfileSO constraints,
        HashSet<string> tileIds,
        int mapWidth,
        int mapHeight,
        string layerId,
        MapGenerationValidationResult result)
    {
        if (constraints == null)
        {
            return;
        }

        if (constraints.borderPadding * 2 >= mapWidth || constraints.borderPadding * 2 >= mapHeight)
        {
            result.AddError("MapConstraintBlocksAllCells", "Constraint border padding leaves no buildable map cells.", layerId);
        }

        if (constraints.minimumConnectedFloorArea > mapWidth * mapHeight)
        {
            result.AddError("MapConstraintImpossibleConnectedArea", "Minimum connected floor area is larger than the map.", layerId);
        }

        if (constraints.forcedCells == null)
        {
            return;
        }

        HashSet<Vector2Int> forcedPositions = new();
        for (int i = 0; i < constraints.forcedCells.Count; i++)
        {
            MapForcedCell forcedCell = constraints.forcedCells[i];
            if (forcedCell == null)
            {
                result.AddWarning("MapConstraintNullForcedCell", $"Forced cell at index {i} is null.", layerId);
                continue;
            }

            if (!IsInside(forcedCell.position, mapWidth, mapHeight))
            {
                result.AddError("MapConstraintForcedCellOutOfBounds", $"Forced cell {forcedCell.position} is outside the map.", layerId);
            }

            if (string.IsNullOrWhiteSpace(forcedCell.tileId))
            {
                result.AddError("MapConstraintForcedCellMissingTile", $"Forced cell {forcedCell.position} has no tile id.", layerId);
            }
            else if (!tileIds.Contains(forcedCell.tileId))
            {
                result.AddError("MapConstraintForcedCellUnknownTile", $"Forced cell references unknown tile '{forcedCell.tileId}'.", layerId);
            }

            if (!forcedPositions.Add(forcedCell.position))
            {
                result.AddError("MapConstraintDuplicateForcedCell", $"Forced cell {forcedCell.position} is defined more than once.", layerId);
            }
        }
    }

    private static void ValidateCompatibility(
        MapGenerationProfileSO profile,
        MapGenerationLayerConfig layer,
        string layerId,
        MapGenerationValidationResult result)
    {
        MapTileCatalogSnapshot tileCatalog = layer.tileSet.CreateSnapshot();
        MapAdjacencyRuleSetSnapshot rules = layer.adjacencyRules.CreateSnapshot();
        if (tileCatalog.Variants.Count == 0)
        {
            return;
        }

        if (profile.mapWidth > 1 && !HasAnyCompatiblePair(tileCatalog, rules, MapDirection.East))
        {
            result.AddError("MapCompatibilityNoEastWestPair", "No compatible east/west tile pair exists for this layer.", layerId);
        }

        if (profile.mapHeight > 1 && !HasAnyCompatiblePair(tileCatalog, rules, MapDirection.North))
        {
            result.AddError("MapCompatibilityNoNorthSouthPair", "No compatible north/south tile pair exists for this layer.", layerId);
        }
    }

    private static bool HasAnyCompatiblePair(
        MapTileCatalogSnapshot tileCatalog,
        MapAdjacencyRuleSetSnapshot rules,
        MapDirection direction)
    {
        for (int sourceIndex = 0; sourceIndex < tileCatalog.Variants.Count; sourceIndex++)
        {
            MapTileVariantSnapshot source = tileCatalog.Variants[sourceIndex];
            for (int neighborIndex = 0; neighborIndex < tileCatalog.Variants.Count; neighborIndex++)
            {
                MapTileVariantSnapshot neighbor = tileCatalog.Variants[neighborIndex];
                if (rules.IsCompatible(source, direction, neighbor)
                    && rules.IsCompatible(neighbor, direction.Opposite(), source))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static string NormalizeLayerId(string layerId, int index)
    {
        return string.IsNullOrWhiteSpace(layerId) ? $"Layer {index}" : layerId.Trim();
    }

    private static bool IsInside(Vector2Int position, int mapWidth, int mapHeight)
    {
        return position.x >= 0 && position.y >= 0 && position.x < mapWidth && position.y < mapHeight;
    }
}
