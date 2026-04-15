using System;
using System.Collections.Generic;

/// <summary>
/// UI 过渡序列：按声明顺序串行执行关闭、打开与回调步骤。
/// </summary>
public sealed class UITransitionSequence : IUITransitionSequence, IUITransitionPlan
{
    private readonly IUITransitionExecutor transitionExecutor;
    private readonly List<TransitionStepDefinition> steps = new List<TransitionStepDefinition>();

    internal UITransitionSequence(IUITransitionExecutor transitionExecutor)
    {
        this.transitionExecutor = transitionExecutor;
    }

    public int StepCount => steps.Count;

    public IUITransitionStep GetStep(int index)
    {
        return steps[index];
    }

    public IUITransitionSequence ClosePage<TPage>() where TPage : UIPageBase
    {
        steps.Add(TransitionStepDefinition.CreateClosePage(typeof(TPage)));
        return this;
    }

    public IUITransitionSequence CloseTopPage()
    {
        steps.Add(TransitionStepDefinition.CreateCloseTopPage());
        return this;
    }

    public IUITransitionSequence CloseAllPages()
    {
        steps.Add(TransitionStepDefinition.CreateCloseAllPages());
        return this;
    }

    public IUITransitionSequence OpenPage<TPage>(object payload = null) where TPage : UIPageBase
    {
        steps.Add(TransitionStepDefinition.CreateOpenPage(typeof(TPage), payload));
        return this;
    }

    public IUITransitionSequence Callback(Action action)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        steps.Add(TransitionStepDefinition.CreateCallback(action));
        return this;
    }

    public IUITransitionSequence Callback<T>(Action<T> action, T argument)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        steps.Add(TransitionStepDefinition.CreateCallback(() => action(argument)));
        return this;
    }

    public void Play()
    {
        transitionExecutor.PlayTransition(this);
    }

    private sealed class TransitionStepDefinition : IUITransitionStep
    {
        private TransitionStepDefinition(UITransitionStepKind kind, Type pageType, object payload, Action callback)
        {
            Kind = kind;
            PageType = pageType;
            Payload = payload;
            Callback = callback;
        }

        public UITransitionStepKind Kind { get; }
        public Type PageType { get; }
        public object Payload { get; }
        public Action Callback { get; }

        public static TransitionStepDefinition CreateClosePage(Type pageType)
        {
            return new TransitionStepDefinition(UITransitionStepKind.ClosePage, pageType, null, null);
        }

        public static TransitionStepDefinition CreateCloseTopPage()
        {
            return new TransitionStepDefinition(UITransitionStepKind.CloseTopPage, null, null, null);
        }

        public static TransitionStepDefinition CreateCloseAllPages()
        {
            return new TransitionStepDefinition(UITransitionStepKind.CloseAllPages, null, null, null);
        }

        public static TransitionStepDefinition CreateOpenPage(Type pageType, object payload)
        {
            return new TransitionStepDefinition(UITransitionStepKind.OpenPage, pageType, payload, null);
        }

        public static TransitionStepDefinition CreateCallback(Action callback)
        {
            return new TransitionStepDefinition(UITransitionStepKind.Callback, null, null, callback);
        }
    }
}
