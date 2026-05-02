using UnityEngine.Scripting.APIUpdating;

namespace AXR.Framework.UI
{
    using System;

public sealed class UIPageOpenContext
{
    public UIPageOpenContext(Type pageType, string instanceId, object payload)
    {
        PageType = pageType;
        InstanceId = instanceId;
        Payload = payload;
    }

    public Type PageType { get; }
    public string InstanceId { get; }
    public object Payload { get; }

    public T GetPayload<T>() where T : class
    {
        return Payload as T;
    }
}
}
