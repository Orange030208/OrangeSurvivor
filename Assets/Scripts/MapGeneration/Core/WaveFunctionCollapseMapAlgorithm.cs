using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class WaveFunctionCollapseMapAlgorithm : IMapGenerationAlgorithm
{
    private static readonly MapDirection[] Directions =
    {
        MapDirection.North,
        MapDirection.East,
        MapDirection.South,
        MapDirection.West
    };

    public MapGenerationAlgorithmType AlgorithmType => MapGenerationAlgorithmType.WaveFunctionCollapse;

    public MapLayerResult Generate(MapGenerationRequest request, MapLayerGenerationRequest layerRequest)
    {
        if (request == null)
        {
            return Failure("Unknown", default, "Map generation request is null.");
        }

        if (layerRequest == null)
        {
            return Failure("Unknown", request.GridSize, "Layer generation request is null.");
        }

        if (layerRequest.TileCatalog == null || layerRequest.TileCatalog.Variants.Count == 0)
        {
            return Failure(layerRequest.LayerId, request.GridSize, "Layer has no tile variants.");
        }

        CompatibilityTable compatibility = new(layerRequest.TileCatalog, layerRequest.AdjacencyRules);
        Dictionary<int, HashSet<int>> forbiddenChoices = new();
        string lastFailure = null;

        for (int attempt = 0; attempt < request.MaxAttempts; attempt++)
        {
            int attemptSeed = CombineSeed(layerRequest.Seed, attempt);
            System.Random random = new(attemptSeed);
            if (TrySolveAttempt(
                    request,
                    layerRequest,
                    compatibility,
                    forbiddenChoices,
                    random,
                    out MapGeneratedCell[] cells,
                    out List<Decision> decisions,
                    out string failureReason))
            {
                return new MapLayerResult(layerRequest.LayerId, request.GridSize, true, null, cells);
            }

            lastFailure = failureReason;
            if (!TryForbidLastDecision(decisions, forbiddenChoices))
            {
                break;
            }
        }

        return Failure(
            layerRequest.LayerId,
            request.GridSize,
            $"WFC failed after {request.MaxAttempts} attempt(s). Last failure: {lastFailure ?? "unknown contradiction."}");
    }

    private static bool TrySolveAttempt(
        MapGenerationRequest request,
        MapLayerGenerationRequest layerRequest,
        CompatibilityTable compatibility,
        IReadOnlyDictionary<int, HashSet<int>> forbiddenChoices,
        System.Random random,
        out MapGeneratedCell[] cells,
        out List<Decision> decisions,
        out string failureReason)
    {
        Vector2Int gridSize = request.GridSize;
        int cellCount = gridSize.x * gridSize.y;
        List<int>[] candidates = new List<int>[cellCount];
        bool[] blockedCells = new bool[cellCount];
        decisions = new List<Decision>(cellCount);

        if (!InitializeCandidates(request, layerRequest, forbiddenChoices, candidates, blockedCells, out failureReason))
        {
            cells = null;
            return false;
        }

        Queue<int> propagationQueue = new(cellCount);
        for (int i = 0; i < cellCount; i++)
        {
            if (!blockedCells[i])
            {
                propagationQueue.Enqueue(i);
            }
        }

        if (!Propagate(gridSize, candidates, blockedCells, compatibility, propagationQueue, out failureReason))
        {
            cells = null;
            return false;
        }

        while (true)
        {
            int cellIndex = SelectNextCell(candidates, blockedCells, layerRequest.TileCatalog, random);
            if (cellIndex < 0)
            {
                break;
            }

            int chosenVariant = PickWeighted(candidates[cellIndex], layerRequest.TileCatalog, random);
            candidates[cellIndex].Clear();
            candidates[cellIndex].Add(chosenVariant);
            decisions.Add(new Decision(cellIndex, chosenVariant));

            propagationQueue.Enqueue(cellIndex);
            if (!Propagate(gridSize, candidates, blockedCells, compatibility, propagationQueue, out failureReason))
            {
                cells = null;
                return false;
            }
        }

        if (!BuildCells(request, layerRequest, candidates, blockedCells, out cells, out failureReason))
        {
            return false;
        }

        if (!ValidateConnectivity(request, layerRequest, cells, out failureReason))
        {
            return false;
        }

        return true;
    }

    private static bool InitializeCandidates(
        MapGenerationRequest request,
        MapLayerGenerationRequest layerRequest,
        IReadOnlyDictionary<int, HashSet<int>> forbiddenChoices,
        IList<int>[] candidates,
        bool[] blockedCells,
        out string failureReason)
    {
        MapTileCatalogSnapshot catalog = layerRequest.TileCatalog;
        Vector2Int gridSize = request.GridSize;
        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                Vector2Int position = new(x, y);
                int cellIndex = ToIndex(position, gridSize.x);
                blockedCells[cellIndex] = layerRequest.Constraints != null
                    && layerRequest.Constraints.IsBlocked(position, gridSize);
                candidates[cellIndex] = new List<int>(catalog.Variants.Count);

                if (blockedCells[cellIndex])
                {
                    continue;
                }

                if (layerRequest.Constraints != null
                    && layerRequest.Constraints.TryGetForcedTileId(position, out string forcedTileId))
                {
                    AddForcedCandidates(catalog, forcedTileId, candidates[cellIndex]);
                }
                else
                {
                    AddUnforcedCandidates(catalog, forbiddenChoices, cellIndex, candidates[cellIndex]);
                }

                if (candidates[cellIndex].Count == 0)
                {
                    failureReason = $"Cell {position} has no available tile variants.";
                    return false;
                }
            }
        }

        failureReason = null;
        return true;
    }

    private static void AddForcedCandidates(MapTileCatalogSnapshot catalog, string forcedTileId, ICollection<int> candidates)
    {
        for (int i = 0; i < catalog.Variants.Count; i++)
        {
            if (string.Equals(catalog.Variants[i].SourceTileId, forcedTileId, StringComparison.Ordinal))
            {
                candidates.Add(i);
            }
        }
    }

    private static void AddUnforcedCandidates(
        MapTileCatalogSnapshot catalog,
        IReadOnlyDictionary<int, HashSet<int>> forbiddenChoices,
        int cellIndex,
        ICollection<int> candidates)
    {
        forbiddenChoices.TryGetValue(cellIndex, out HashSet<int> forbiddenVariants);
        for (int i = 0; i < catalog.Variants.Count; i++)
        {
            if (forbiddenVariants != null && forbiddenVariants.Contains(i))
            {
                continue;
            }

            candidates.Add(i);
        }
    }

    private static bool Propagate(
        Vector2Int gridSize,
        List<int>[] candidates,
        bool[] blockedCells,
        CompatibilityTable compatibility,
        Queue<int> propagationQueue,
        out string failureReason)
    {
        while (propagationQueue.Count > 0)
        {
            int sourceIndex = propagationQueue.Dequeue();
            if (blockedCells[sourceIndex])
            {
                continue;
            }

            Vector2Int sourcePosition = ToPosition(sourceIndex, gridSize.x);
            for (int directionIndex = 0; directionIndex < Directions.Length; directionIndex++)
            {
                MapDirection direction = Directions[directionIndex];
                Vector2Int neighborPosition = sourcePosition + direction.ToOffset();
                if (!IsInside(neighborPosition, gridSize))
                {
                    continue;
                }

                int neighborIndex = ToIndex(neighborPosition, gridSize.x);
                if (blockedCells[neighborIndex])
                {
                    continue;
                }

                if (RemoveIncompatibleCandidates(
                        candidates[sourceIndex],
                        candidates[neighborIndex],
                        direction,
                        compatibility))
                {
                    if (candidates[neighborIndex].Count == 0)
                    {
                        failureReason = $"Contradiction at cell {neighborPosition}: all candidates were removed.";
                        return false;
                    }

                    propagationQueue.Enqueue(neighborIndex);
                }
            }
        }

        failureReason = null;
        return true;
    }

    private static bool RemoveIncompatibleCandidates(
        IReadOnlyList<int> sourceCandidates,
        List<int> neighborCandidates,
        MapDirection direction,
        CompatibilityTable compatibility)
    {
        bool changed = false;
        for (int i = neighborCandidates.Count - 1; i >= 0; i--)
        {
            int neighborVariantIndex = neighborCandidates[i];
            if (HasCompatibleSource(sourceCandidates, neighborVariantIndex, direction, compatibility))
            {
                continue;
            }

            neighborCandidates.RemoveAt(i);
            changed = true;
        }

        return changed;
    }

    private static bool HasCompatibleSource(
        IReadOnlyList<int> sourceCandidates,
        int neighborVariantIndex,
        MapDirection direction,
        CompatibilityTable compatibility)
    {
        for (int i = 0; i < sourceCandidates.Count; i++)
        {
            int sourceVariantIndex = sourceCandidates[i];
            if (compatibility.Allows(sourceVariantIndex, direction, neighborVariantIndex)
                && compatibility.Allows(neighborVariantIndex, direction.Opposite(), sourceVariantIndex))
            {
                return true;
            }
        }

        return false;
    }

    private static int SelectNextCell(
        IReadOnlyList<int>[] candidates,
        IReadOnlyList<bool> blockedCells,
        MapTileCatalogSnapshot catalog,
        System.Random random)
    {
        int bestIndex = -1;
        double bestEntropy = double.MaxValue;
        for (int i = 0; i < candidates.Length; i++)
        {
            if (blockedCells[i] || candidates[i].Count <= 1)
            {
                continue;
            }

            double entropy = CalculateEntropy(candidates[i], catalog);
            if (entropy < bestEntropy - 0.000001d)
            {
                bestEntropy = entropy;
                bestIndex = i;
            }
            else if (Math.Abs(entropy - bestEntropy) <= 0.000001d && random.NextDouble() < 0.5d)
            {
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private static double CalculateEntropy(IReadOnlyList<int> candidates, MapTileCatalogSnapshot catalog)
    {
        double weightSum = 0d;
        double weightedLogSum = 0d;
        for (int i = 0; i < candidates.Count; i++)
        {
            float weight = Mathf.Max(0f, catalog.Variants[candidates[i]].Weight);
            if (weight <= 0f)
            {
                continue;
            }

            weightSum += weight;
            weightedLogSum += weight * Math.Log(weight);
        }

        if (weightSum <= 0d)
        {
            return candidates.Count;
        }

        return Math.Log(weightSum) - (weightedLogSum / weightSum);
    }

    private static int PickWeighted(IReadOnlyList<int> candidates, MapTileCatalogSnapshot catalog, System.Random random)
    {
        double weightSum = 0d;
        for (int i = 0; i < candidates.Count; i++)
        {
            weightSum += Mathf.Max(0f, catalog.Variants[candidates[i]].Weight);
        }

        if (weightSum <= 0d)
        {
            return candidates[random.Next(candidates.Count)];
        }

        double target = random.NextDouble() * weightSum;
        double accumulator = 0d;
        for (int i = 0; i < candidates.Count; i++)
        {
            accumulator += Mathf.Max(0f, catalog.Variants[candidates[i]].Weight);
            if (accumulator >= target)
            {
                return candidates[i];
            }
        }

        return candidates[candidates.Count - 1];
    }

    private static bool BuildCells(
        MapGenerationRequest request,
        MapLayerGenerationRequest layerRequest,
        IReadOnlyList<int>[] candidates,
        IReadOnlyList<bool> blockedCells,
        out MapGeneratedCell[] cells,
        out string failureReason)
    {
        Vector2Int gridSize = request.GridSize;
        cells = new MapGeneratedCell[gridSize.x * gridSize.y];
        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                Vector2Int position = new(x, y);
                int cellIndex = ToIndex(position, gridSize.x);
                if (blockedCells[cellIndex])
                {
                    cells[cellIndex] = new MapGeneratedCell(position, null, null, 0, true);
                    continue;
                }

                if (candidates[cellIndex].Count != 1)
                {
                    failureReason = $"Cell {position} is unresolved with {candidates[cellIndex].Count} candidate(s).";
                    return false;
                }

                MapTileVariantSnapshot variant = layerRequest.TileCatalog.Variants[candidates[cellIndex][0]];
                cells[cellIndex] = new MapGeneratedCell(
                    position,
                    variant.SourceTileId,
                    variant.VariantId,
                    variant.RotationQuarterTurns,
                    false);
            }
        }

        failureReason = null;
        return true;
    }

    private static bool ValidateConnectivity(
        MapGenerationRequest request,
        MapLayerGenerationRequest layerRequest,
        IReadOnlyList<MapGeneratedCell> cells,
        out string failureReason)
    {
        if (layerRequest.Constraints == null || !layerRequest.Constraints.RequireConnectedFloor)
        {
            failureReason = null;
            return true;
        }

        int buildableCellCount = 0;
        int startIndex = -1;
        for (int i = 0; i < cells.Count; i++)
        {
            if (cells[i].Blocked)
            {
                continue;
            }

            buildableCellCount++;
            if (startIndex < 0)
            {
                startIndex = i;
            }
        }

        if (buildableCellCount == 0)
        {
            failureReason = "Connectivity validation failed: no buildable cells exist.";
            return false;
        }

        int connectedCount = CountConnectedCells(request.GridSize, cells, startIndex);
        int requiredCount = layerRequest.Constraints.MinimumConnectedFloorArea > 0
            ? layerRequest.Constraints.MinimumConnectedFloorArea
            : buildableCellCount;

        if (connectedCount < requiredCount)
        {
            failureReason = $"Connectivity validation failed: largest connected area has {connectedCount} cell(s), required {requiredCount}.";
            return false;
        }

        failureReason = null;
        return true;
    }

    private static int CountConnectedCells(Vector2Int gridSize, IReadOnlyList<MapGeneratedCell> cells, int startIndex)
    {
        bool[] visited = new bool[cells.Count];
        Queue<int> queue = new();
        queue.Enqueue(startIndex);
        visited[startIndex] = true;
        int count = 0;

        while (queue.Count > 0)
        {
            int index = queue.Dequeue();
            count++;
            Vector2Int position = ToPosition(index, gridSize.x);
            for (int i = 0; i < Directions.Length; i++)
            {
                Vector2Int neighborPosition = position + Directions[i].ToOffset();
                if (!IsInside(neighborPosition, gridSize))
                {
                    continue;
                }

                int neighborIndex = ToIndex(neighborPosition, gridSize.x);
                if (visited[neighborIndex] || cells[neighborIndex].Blocked)
                {
                    continue;
                }

                visited[neighborIndex] = true;
                queue.Enqueue(neighborIndex);
            }
        }

        return count;
    }

    private static bool TryForbidLastDecision(
        IReadOnlyList<Decision> decisions,
        IDictionary<int, HashSet<int>> forbiddenChoices)
    {
        if (decisions == null || decisions.Count == 0)
        {
            return false;
        }

        for (int i = decisions.Count - 1; i >= 0; i--)
        {
            Decision decision = decisions[i];
            if (!forbiddenChoices.TryGetValue(decision.CellIndex, out HashSet<int> forbiddenVariants))
            {
                forbiddenVariants = new HashSet<int>();
                forbiddenChoices.Add(decision.CellIndex, forbiddenVariants);
            }

            if (forbiddenVariants.Add(decision.VariantIndex))
            {
                return true;
            }
        }

        return false;
    }

    private static MapLayerResult Failure(string layerId, Vector2Int gridSize, string failureReason)
    {
        return new MapLayerResult(layerId, gridSize, false, failureReason, Array.Empty<MapGeneratedCell>());
    }

    private static int CombineSeed(int seed, int attempt)
    {
        unchecked
        {
            int hash = seed;
            hash = (hash * 486187739) ^ (attempt * 19349663);
            hash ^= hash >> 13;
            hash *= 1274126177;
            hash ^= hash >> 16;
            return hash;
        }
    }

    private static bool IsInside(Vector2Int position, Vector2Int gridSize)
    {
        return position.x >= 0 && position.y >= 0 && position.x < gridSize.x && position.y < gridSize.y;
    }

    private static int ToIndex(Vector2Int position, int width)
    {
        return position.y * width + position.x;
    }

    private static Vector2Int ToPosition(int index, int width)
    {
        return new Vector2Int(index % width, index / width);
    }

    private readonly struct Decision
    {
        public Decision(int cellIndex, int variantIndex)
        {
            CellIndex = cellIndex;
            VariantIndex = variantIndex;
        }

        public int CellIndex { get; }
        public int VariantIndex { get; }
    }

    private sealed class CompatibilityTable
    {
        private readonly bool[] compatibility;
        private readonly int variantCount;

        public CompatibilityTable(MapTileCatalogSnapshot tileCatalog, MapAdjacencyRuleSetSnapshot rules)
        {
            variantCount = tileCatalog.Variants.Count;
            compatibility = new bool[variantCount * Directions.Length * variantCount];
            MapAdjacencyRuleSetSnapshot resolvedRules = rules ?? MapAdjacencyRuleSetSnapshot.CreateEmpty();
            for (int sourceIndex = 0; sourceIndex < variantCount; sourceIndex++)
            {
                MapTileVariantSnapshot source = tileCatalog.Variants[sourceIndex];
                for (int directionIndex = 0; directionIndex < Directions.Length; directionIndex++)
                {
                    MapDirection direction = Directions[directionIndex];
                    for (int neighborIndex = 0; neighborIndex < variantCount; neighborIndex++)
                    {
                        MapTileVariantSnapshot neighbor = tileCatalog.Variants[neighborIndex];
                        compatibility[ToCompatibilityIndex(sourceIndex, directionIndex, neighborIndex)] =
                            resolvedRules.IsCompatible(source, direction, neighbor);
                    }
                }
            }
        }

        public bool Allows(int sourceVariantIndex, MapDirection direction, int neighborVariantIndex)
        {
            return compatibility[ToCompatibilityIndex(sourceVariantIndex, (int)direction, neighborVariantIndex)];
        }

        private int ToCompatibilityIndex(int sourceVariantIndex, int directionIndex, int neighborVariantIndex)
        {
            return ((sourceVariantIndex * Directions.Length) + directionIndex) * variantCount + neighborVariantIndex;
        }
    }
}
