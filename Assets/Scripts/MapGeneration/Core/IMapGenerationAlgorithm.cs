public interface IMapGenerationAlgorithm
{
    MapGenerationAlgorithmType AlgorithmType { get; }

    MapLayerResult Generate(MapGenerationRequest request, MapLayerGenerationRequest layerRequest);
}
