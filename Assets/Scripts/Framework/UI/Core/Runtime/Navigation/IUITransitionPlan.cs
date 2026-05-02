using UnityEngine.Scripting.APIUpdating;

namespace AXR.Framework.UI
{
    using System;

public interface IUITransitionPlan
{
    int StepCount { get; }
    IUITransitionStep GetStep(int index);
}
}
