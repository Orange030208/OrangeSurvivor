public readonly struct AnimationStateProgress
{
    public AnimationStateProgress(bool isPlaying, float normalizedTime)
    {
        IsPlaying = isPlaying;
        NormalizedTime = normalizedTime;
    }

    public bool IsPlaying { get; }
    public float NormalizedTime { get; }

    public bool IsComplete(float exitNormalizedTime)
    {
        return IsPlaying && NormalizedTime >= exitNormalizedTime;
    }
}
