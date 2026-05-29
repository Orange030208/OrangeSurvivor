using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Map Generation Profile", menuName = ScriptableObjectMenuPaths.MAP_GENERATION_PROFILE, order = 0)]
public class MapGenerationProfileSO : ScriptableObject
{
    public int mapWidth = 32;
    public int mapHeight = 32;
    public float cellSize = 1f;
    public int seed = 20260523;
    public int maxAttempts = 32;
    public List<MapGenerationLayerConfig> layers = new();

    public MapGenerationRequest CreateRequest()
    {
        Vector2Int gridSize = new(Mathf.Max(1, mapWidth), Mathf.Max(1, mapHeight));
        Vector3Int gridOrigin = new(-(gridSize.x / 2), -(gridSize.y / 2), 0);

        List<MapLayerGenerationRequest> layerRequests = new();
        if (layers != null)
        {
            for (int i = 0; i < layers.Count; i++)
            {
                MapGenerationLayerConfig layer = layers[i];
                if (layer == null || !layer.enabled)
                {
                    continue;
                }

                if (layer.tileSet == null)
                {
                    continue;
                }

                MapTileCatalogSnapshot tileCatalog = layer.tileSet.CreateSnapshot();
                MapAdjacencyRuleSetSnapshot adjacencyRules = layer.adjacencyRules != null
                    ? layer.adjacencyRules.CreateSnapshot()
                    : MapAdjacencyRuleSetSnapshot.CreateEmpty();
                MapConstraintProfileSnapshot constraints = layer.constraints != null
                    ? layer.constraints.CreateSnapshot()
                    : MapConstraintProfileSnapshot.CreateEmpty();

                layerRequests.Add(new MapLayerGenerationRequest(
                    layer.layerId,
                    layer.algorithm,
                    seed + layer.seedOffset,
                    tileCatalog,
                    adjacencyRules,
                    constraints));
            }
        }

        return new MapGenerationRequest(
            gridSize,
            gridOrigin,
            Mathf.Max(0.01f, cellSize),
            seed,
            Mathf.Max(1, maxAttempts),
            layerRequests);
    }

    public bool TryGetLayer(string layerId, out MapGenerationLayerConfig layerConfig)
    {
        layerConfig = null;
        if (layers == null || string.IsNullOrWhiteSpace(layerId))
        {
            return false;
        }

        for (int i = 0; i < layers.Count; i++)
        {
            MapGenerationLayerConfig layer = layers[i];
            if (layer == null)
            {
                continue;
            }

            if (string.Equals(layer.layerId, layerId, System.StringComparison.Ordinal))
            {
                layerConfig = layer;
                return true;
            }
        }

        return false;
    }

    private void OnValidate()
    {
        mapWidth = Mathf.Max(1, mapWidth);
        mapHeight = Mathf.Max(1, mapHeight);
        cellSize = Mathf.Max(0.01f, cellSize);
        maxAttempts = Mathf.Max(1, maxAttempts);

        if (layers == null)
        {
            layers = new List<MapGenerationLayerConfig>();
        }
    }
}
