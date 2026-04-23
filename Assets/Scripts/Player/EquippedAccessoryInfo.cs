public readonly struct EquippedAccessoryInfo
{
    public AccessoryDataSO Data { get; }
    public string RuntimeSourceId { get; }

    public EquippedAccessoryInfo(AccessoryDataSO data, string runtimeSourceId)
    {
        Data = data;
        RuntimeSourceId = runtimeSourceId;
    }
}
