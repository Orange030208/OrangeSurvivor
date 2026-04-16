using UnityEngine;

public sealed class AudioCueCatalogProvider : IAudioCueProvider
{
    private readonly AudioCueCatalogSO catalog;

    public AudioCueCatalogProvider(AudioCueCatalogSO catalog)
    {
        this.catalog = catalog != null
            ? catalog
            : throw new System.ArgumentNullException(nameof(catalog), "Audio cue catalog cannot be null.");
    }

    public bool TryGetCue(string cueId, out AudioCueData cueData)
    {
        return catalog.TryGetCue(cueId, out cueData);
    }
}
