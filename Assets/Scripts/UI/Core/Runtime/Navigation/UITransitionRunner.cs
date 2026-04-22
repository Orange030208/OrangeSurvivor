using System;
using UnityEngine;

internal sealed class UITransitionRunner : IUITransitionExecutor
{
    private readonly IUITransitionRunnerHost host;
    private IUITransitionPlan activeTransitionPlan;
    private IUITransitionPlan pendingTransitionPlan;
    private int activeTransitionStepIndex;
    private bool waitingForTransitionClosures;

    public UITransitionRunner(IUITransitionRunnerHost host)
    {
        this.host = host;
    }

    public void PlayTransition(IUITransitionPlan transitionPlan)
    {
        if (transitionPlan == null)
        {
            throw new ArgumentNullException(nameof(transitionPlan));
        }

        if (transitionPlan.StepCount == 0)
        {
            return;
        }

        pendingTransitionPlan = transitionPlan;
        if (activeTransitionPlan != null || waitingForTransitionClosures)
        {
            return;
        }

        StartPendingTransition();
    }

    public void NotifyTransitionClosuresCompleted()
    {
        waitingForTransitionClosures = false;
        TryAdvanceTransition();
    }

    public string GetDebugSummary()
    {
        string activeSummary = DescribePlan(activeTransitionPlan, activeTransitionStepIndex);
        string pendingSummary = DescribePlan(pendingTransitionPlan, 0);
        return $"waitingForTransitionClosures={waitingForTransitionClosures}, active={activeSummary}, pending={pendingSummary}";
    }

    private void StartPendingTransition()
    {
        if (pendingTransitionPlan == null)
        {
            return;
        }

        activeTransitionPlan = pendingTransitionPlan;
        pendingTransitionPlan = null;
        activeTransitionStepIndex = 0;
        waitingForTransitionClosures = false;
        TryAdvanceTransition();
    }

    private void TryAdvanceTransition()
    {
        if (activeTransitionPlan == null)
        {
            StartPendingTransition();
            return;
        }

        if (waitingForTransitionClosures)
        {
            return;
        }

        while (activeTransitionPlan != null && activeTransitionStepIndex < activeTransitionPlan.StepCount)
        {
            IUITransitionStep step = activeTransitionPlan.GetStep(activeTransitionStepIndex);
            activeTransitionStepIndex++;

            if (TryExecuteAsyncTransitionStep(step))
            {
                return;
            }

            ExecuteSyncTransitionStep(step);
        }

        activeTransitionPlan = null;
        activeTransitionStepIndex = 0;
        waitingForTransitionClosures = false;
        StartPendingTransition();
    }

    private bool TryExecuteAsyncTransitionStep(IUITransitionStep step)
    {
        switch (step.Kind)
        {
            case UITransitionStepKind.ClosePage:
                return TryStartClosePageStep(step.PageType);
            case UITransitionStepKind.CloseTopPage:
                return TryStartCloseTopPageStep();
            case UITransitionStepKind.CloseAllPages:
                return TryStartCloseAllPagesStep();
            default:
                return false;
        }
    }

    private void ExecuteSyncTransitionStep(IUITransitionStep step)
    {
        switch (step.Kind)
        {
            case UITransitionStepKind.OpenPage:
                host.OpenPage(step.PageType, step.Payload);
                break;
            case UITransitionStepKind.Callback:
                step.Callback?.Invoke();
                break;
        }
    }

    private bool TryStartClosePageStep(Type pageType)
    {
        bool closeStarted = host.ClosePage(pageType);
        if (closeStarted)
        {
            waitingForTransitionClosures = true;
        }

        return closeStarted;
    }

    private bool TryStartCloseTopPageStep()
    {
        bool closeStarted = host.CloseTopPage();
        if (closeStarted)
        {
            waitingForTransitionClosures = true;
        }

        return closeStarted;
    }

    private bool TryStartCloseAllPagesStep()
    {
        int closedCount = host.CloseAllPages();
        if (closedCount > 0)
        {
            waitingForTransitionClosures = true;
            return true;
        }

        return false;
    }

    private static string DescribePlan(IUITransitionPlan plan, int currentStepIndex)
    {
        if (plan == null)
        {
            return "null";
        }

        return $"steps={plan.StepCount}, currentStepIndex={currentStepIndex}";
    }
}
