using System.Collections.Generic;

public sealed class MapGenerationPipeline
{
    private readonly IMapGenerationAlgorithm waveFunctionCollapseAlgorithm;

    public MapGenerationPipeline()
        : this(new WaveFunctionCollapseMapAlgorithm())
    {
    }

    public MapGenerationPipeline(IMapGenerationAlgorithm waveFunctionCollapseAlgorithm)
    {
        this.waveFunctionCollapseAlgorithm = waveFunctionCollapseAlgorithm;
    }

    public MapGenerationResult Generate(MapGenerationRequest request)
    {
        MapGenerationValidationResult validation = MapGenerationValidator.ValidateRequest(request);
        if (!validation.IsValid)
        {
            return new MapGenerationResult(
                request != null ? request.GridSize : default,
                request != null ? request.GridOrigin : default,
                request != null ? request.CellSize : 1f,
                false,
                validation.Format(),
                System.Array.Empty<MapLayerResult>());
        }

        List<MapLayerResult> layerResults = new(request.Layers.Count);
        for (int i = 0; i < request.Layers.Count; i++)
        {
            MapLayerGenerationRequest layerRequest = request.Layers[i];
            MapLayerResult layerResult = ResolveAlgorithm(layerRequest.Algorithm).Generate(request, layerRequest);
            layerResults.Add(layerResult);
            if (!layerResult.Success)
            {
                return new MapGenerationResult(
                    request.GridSize,
                    request.GridOrigin,
                    request.CellSize,
                    false,
                    layerResult.FailureReason,
                    layerResults);
            }
        }

        return new MapGenerationResult(
            request.GridSize,
            request.GridOrigin,
            request.CellSize,
            true,
            null,
            layerResults);
    }

    private IMapGenerationAlgorithm ResolveAlgorithm(MapGenerationAlgorithmType algorithmType)
    {
        return algorithmType switch
        {
            MapGenerationAlgorithmType.WaveFunctionCollapse => waveFunctionCollapseAlgorithm,
            _ => waveFunctionCollapseAlgorithm
        };
    }
}
