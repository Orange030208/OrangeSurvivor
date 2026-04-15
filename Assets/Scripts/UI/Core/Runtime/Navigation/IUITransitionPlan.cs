using System;

public interface IUITransitionPlan
{
    int StepCount { get; }
    IUITransitionStep GetStep(int index);
}
