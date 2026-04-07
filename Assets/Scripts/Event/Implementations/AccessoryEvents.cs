using System;

public struct AccessorySelectionStartedEvent : IGameEvent
{
    public AccessoryDataSO accessoryData;

    public AccessorySelectionStartedEvent(AccessoryDataSO accessoryData)
    {
        this.accessoryData = accessoryData;
    }
}

public struct AccessoryOperateEvent : IGameEvent
{
    public AccessoryDataSO accessoryData;
    /// <summary>
    /// true为获取,false为回收
    /// </summary>
    public bool selected;

    public AccessoryOperateEvent(AccessoryDataSO accessoryData, bool selected)
    {
        this.accessoryData = accessoryData;
        this.selected = selected;
    }
}