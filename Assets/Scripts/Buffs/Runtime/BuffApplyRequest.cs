public readonly struct BuffApplyRequest
{
    public readonly BuffDataSO BuffData;
    public readonly bool OverrideDuration;
    public readonly BuffDurationPolicy DurationPolicy;
    public readonly float DurationSeconds;

    public BuffApplyRequest(BuffDataSO buffData)
    {
        BuffData = buffData;
        OverrideDuration = false;
        DurationPolicy = buffData != null ? buffData.DurationPolicy : BuffDurationPolicy.Permanent;
        DurationSeconds = buffData != null ? buffData.DurationSeconds : 0f;
    }

    public BuffApplyRequest(BuffDataSO buffData, BuffDurationPolicy durationPolicy, float durationSeconds)
    {
        BuffData = buffData;
        OverrideDuration = true;
        DurationPolicy = durationPolicy;
        DurationSeconds = durationSeconds;
    }
}
