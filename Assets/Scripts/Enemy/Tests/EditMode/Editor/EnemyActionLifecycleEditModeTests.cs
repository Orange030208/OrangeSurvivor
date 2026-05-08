using NUnit.Framework;

public sealed class EnemyActionLifecycleEditModeTests
{
    private enum TestState
    {
        Idle,
        Action,
        Chase
    }

    [Test]
    public void StateMachine_DefersNormalRequestUntilCurrentStateCanExit()
    {
        StateMachine<TestState> stateMachine = new();
        GateState actionState = new(TestState.Action);

        stateMachine.RegisterState(new GateState(TestState.Idle));
        stateMachine.RegisterState(actionState);
        stateMachine.RegisterState(new GateState(TestState.Chase));

        stateMachine.ChangeState(TestState.Action);

        StateChangeResult result = stateMachine.RequestState(TestState.Chase);

        Assert.That(result, Is.EqualTo(StateChangeResult.Deferred));
        Assert.That(stateMachine.IsCurrentState(TestState.Action), Is.True);
        Assert.That(stateMachine.HasPendingState, Is.True);

        actionState.CanExit = true;
        Assert.That(stateMachine.TryApplyPendingState(), Is.True);
        Assert.That(stateMachine.IsCurrentState(TestState.Chase), Is.True);
    }

    [Test]
    public void StateMachine_ForceRequestBypassesExitGate()
    {
        StateMachine<TestState> stateMachine = new();

        stateMachine.RegisterState(new GateState(TestState.Idle));
        stateMachine.RegisterState(new GateState(TestState.Action));
        stateMachine.RegisterState(new GateState(TestState.Chase));

        stateMachine.ChangeState(TestState.Action);

        StateChangeResult result = stateMachine.RequestState(TestState.Chase, StateChangeMode.Force);

        Assert.That(result, Is.EqualTo(StateChangeResult.Changed));
        Assert.That(stateMachine.IsCurrentState(TestState.Chase), Is.True);
    }

    [Test]
    public void EnemyActionRunner_CommitsOnceWhenCommitPointIsReached()
    {
        EnemyActionRunner runner = new();
        FakeAnimatable animatable = new();
        EnemyActionDefinition action = new("Test_Action", "Action", 0.5f);

        runner.Begin(action, animatable);
        animatable.NormalizedTime = 0.5f;
        runner.Tick(0f);

        Assert.That(runner.ShouldCommit, Is.True);
        runner.MarkCommitted();

        runner.Tick(0f);

        Assert.That(runner.ShouldCommit, Is.False);
    }

    [Test]
    public void EnemyActionRunner_CompletesAnimationNormalizedActionAtExitPoint()
    {
        EnemyActionRunner runner = new();
        FakeAnimatable animatable = new();
        EnemyActionDefinition action = new("Test_Action", "Action", 0.5f);

        runner.Begin(action, animatable);
        animatable.NormalizedTime = 1f;
        runner.Tick(0f);

        Assert.That(runner.IsComplete, Is.True);
    }

    [Test]
    public void EnemyActionRunner_CompletionConsumesPendingCommit()
    {
        EnemyActionRunner runner = new();
        FakeAnimatable animatable = new();
        EnemyActionDefinition action = new("Test_Action", "Action", 1f);

        runner.Begin(action, animatable);
        animatable.NormalizedTime = 1f;
        runner.Tick(0f);

        Assert.That(runner.IsComplete, Is.True);
        Assert.That(runner.ShouldCommit, Is.False);
        Assert.That(runner.IsCommitted, Is.True);
    }

    [Test]
    public void EnemyActionRunner_CompletesDurationActionAfterDuration()
    {
        EnemyActionRunner runner = new();
        FakeAnimatable animatable = new();
        EnemyActionDefinition action = new();
        action.ConfigureDefaults(
            "Duration_Action",
            "Action",
            0f,
            EnemyActionCompletionMode.Duration,
            false,
            0.25f);

        runner.Begin(action, animatable);
        runner.Tick(0.24f);

        Assert.That(runner.IsComplete, Is.False);

        runner.Tick(0.01f);

        Assert.That(runner.IsComplete, Is.True);
    }

    [Test]
    public void EnemyActionRunner_ManualActionOnlyCompletesWhenMarked()
    {
        EnemyActionRunner runner = new();
        FakeAnimatable animatable = new();
        EnemyActionDefinition action = new();
        action.ConfigureDefaults(
            "Manual_Action",
            "Action",
            0f,
            EnemyActionCompletionMode.Manual,
            false);

        runner.Begin(action, animatable);
        runner.Tick(10f);

        Assert.That(runner.IsComplete, Is.False);

        runner.MarkComplete();

        Assert.That(runner.IsComplete, Is.True);
    }

    private sealed class GateState : StateBase<TestState>
    {
        public GateState(TestState stateKey) : base(stateKey)
        {
        }

        public bool CanExit { get; set; }

        public override bool CanExitTo(TestState nextState, StateChangeMode mode)
        {
            return mode == StateChangeMode.Force || CanExit;
        }
    }

    private sealed class FakeAnimatable : IAnimatable
    {
        private int currentStateHash;

        public float NormalizedTime { get; set; }

        public void SetBool(int id, bool value)
        {
        }

        public void SetTrigger(int id)
        {
        }

        public void SetFloat(int id, float value)
        {
        }

        public void SetInteger(int id, int value)
        {
        }

        public void SetBool(string paramName, bool value)
        {
        }

        public void SetTrigger(string paramName)
        {
        }

        public void SetFloat(string paramName, float value)
        {
        }

        public void SetInteger(string paramName, int value)
        {
        }

        public void PlayState(string stateName)
        {
            currentStateHash = UnityEngine.Animator.StringToHash(stateName);
            NormalizedTime = 0f;
        }

        public void PlayState(int stateHash)
        {
            PlayState(stateHash, 0f);
        }

        public void PlayState(int stateHash, float normalizedTime, int layerIndex = 0)
        {
            currentStateHash = stateHash;
            NormalizedTime = normalizedTime;
        }

        public void SetPlaybackSpeed(float speed)
        {
        }

        public void ResetPlaybackSpeed()
        {
        }

        public bool IsCurrentState(int stateHash, int layerIndex = 0)
        {
            return currentStateHash == stateHash;
        }

        public float GetCurrentStateNormalizedTime(int layerIndex = 0)
        {
            return NormalizedTime;
        }

        public AnimationStateProgress GetStateProgress(int stateHash, int layerIndex = 0)
        {
            return new AnimationStateProgress(currentStateHash == stateHash, NormalizedTime);
        }
    }
}
