public interface IAudioCueProvider
{
    bool TryGetCue(string cueId, out AudioCueData cueData);
}
