using UnityEngine.Scripting.APIUpdating;

namespace AXR.Framework.UI
{
    using System;

public interface IUITransitionStep
{
    UITransitionStepKind Kind { get; }
    Type PageType { get; }
    object Payload { get; }
    Action Callback { get; }
}
}
