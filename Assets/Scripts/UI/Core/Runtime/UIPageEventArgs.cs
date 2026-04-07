using System;

public sealed class UIPageEventArgs : EventArgs
{
    public UIPageEventArgs(Type pageType, string instanceId)
    {
        PageType = pageType;
        InstanceId = instanceId;
    }

    public Type PageType { get; }
    public string InstanceId { get; }
}
