public readonly struct WaveCompletionContext
{
    public int WaveIndex { get; }
    public int WaveNumber { get; }
    public float ElapsedTime { get; }
    public float WaveDuration { get; }

    public WaveCompletionContext(int waveIndex, int waveNumber, float elapsedTime, float waveDuration)
    {
        WaveIndex = waveIndex;
        WaveNumber = waveNumber;
        ElapsedTime = elapsedTime;
        WaveDuration = waveDuration;
    }
}
