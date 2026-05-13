using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class WaveEndPipelineTests
{
    private readonly List<Object> createdObjects = new();
    private TestWaveEndStep registeredStep;

    [TearDown]
    public void TearDown()
    {
        if (registeredStep != null)
        {
            WaveEndStepRegistry.Unregister(registeredStep);
            registeredStep = null;
        }

        for (int i = 0; i < createdObjects.Count; i++)
        {
            if (createdObjects[i] != null)
            {
                Object.DestroyImmediate(createdObjects[i]);
            }
        }

        createdObjects.Clear();
    }

    [Test]
    public void PipelineExecutesStepsByPriority()
    {
        List<string> executionOrder = new();
        WaveEndPipeline pipeline = new(new IWaveEndStep[]
        {
            new TestWaveEndStep("late", 10, executionOrder),
            new TestWaveEndStep("middle", 0, executionOrder),
            new TestWaveEndStep("early", -10, executionOrder)
        });

        pipeline.RunAsync(CancellationToken.None).GetAwaiter().GetResult();

        CollectionAssert.AreEqual(
            new[] { "early", "middle", "late" },
            executionOrder);
    }

    [Test]
    public void RegistryMergesDefaultAndRegisteredSteps()
    {
        List<string> executionOrder = new();
        registeredStep = new TestWaveEndStep("registered", 0, executionOrder);
        TestWaveEndStep defaultStep = new("default", 10, executionOrder);

        WaveEndStepRegistry.Register(registeredStep);
        IWaveEndStep[] mergedSteps = WaveEndStepRegistry.MergeWithRegisteredSteps(new IWaveEndStep[] { defaultStep });

        WaveEndPipeline pipeline = new(mergedSteps);
        pipeline.RunAsync(CancellationToken.None).GetAwaiter().GetResult();

        CollectionAssert.AreEqual(new[] { "registered", "default" }, executionOrder);
    }

    [Test]
    public void PipelineStartsSamePriorityStepsBeforeAwaitingBatchCompletion()
    {
        List<string> executionOrder = new();
        UniTaskCompletionSource gate = new();
        using CancellationTokenSource cancellationTokenSource = new();
        cancellationTokenSource.CancelAfter(2000);
        WaveEndPipeline pipeline = new(new IWaveEndStep[]
        {
            new AWaitingWaveEndStep(gate.Task, executionOrder),
            new BReleasingWaveEndStep(gate, executionOrder)
        });

        pipeline.RunAsync(cancellationTokenSource.Token).GetAwaiter().GetResult();

        Assert.Less(
            executionOrder.IndexOf(BReleasingWaveEndStep.StartedId),
            executionOrder.IndexOf(AWaitingWaveEndStep.FinishedId));
    }

    [Test]
    public void ProjectilePrepareForWaveEndAlwaysStopsMotion()
    {
        GameObject gameObject = CreateGameObject("wave_end_projectile");
        Rigidbody2D rigidbody = gameObject.AddComponent<Rigidbody2D>();
        Collider2D collider = gameObject.AddComponent<BoxCollider2D>();
        Projectile projectile = gameObject.AddComponent<Projectile>();

        rigidbody.simulated = true;
        rigidbody.velocity = Vector2.right * 5f;
        projectile.PrepareForWaveEnd();

        Assert.That(rigidbody.velocity.sqrMagnitude, Is.EqualTo(0f).Within(0.0001f));
        Assert.IsFalse(rigidbody.simulated);
        Assert.IsFalse(collider.enabled);

        rigidbody.simulated = true;
        rigidbody.velocity = Vector2.up * 5f;
        collider.enabled = true;
        projectile.PrepareForWaveEnd();

        Assert.That(rigidbody.velocity.sqrMagnitude, Is.EqualTo(0f).Within(0.0001f));
        Assert.IsFalse(rigidbody.simulated);
        Assert.IsFalse(collider.enabled);
    }

    [Test]
    public void DefaultPipelineIncludesWaveEndStepsUnderPlayerHierarchy()
    {
        GameObject playerObject = CreateGameObject("wave_end_player");
        Player player = playerObject.AddComponent<Player>();
        GameObject childObject = CreateGameObject("wave_end_child_step");
        childObject.transform.SetParent(playerObject.transform, false);
        TestWaveEndComponent childStep = childObject.AddComponent<TestWaveEndComponent>();

        WaveEndPipelineFactory.CreateDefault(player, null)
            .RunAsync(CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        Assert.IsTrue(childStep.Executed);
    }

    [Test]
    public void DefaultPipelineIgnoresEntityWithoutWaveEndStep()
    {
        GameObject gameObject = CreateGameObject("plain_entity");
        TestEntity entity = gameObject.AddComponent<TestEntity>();

        WaveEndPipelineFactory.CreateDefault()
            .RunAsync(CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        Assert.IsTrue(entity.IsRuntimeEnabled);
    }

    private GameObject CreateGameObject(string name)
    {
        GameObject gameObject = new GameObject(name);
        createdObjects.Add(gameObject);
        return gameObject;
    }

    private sealed class TestWaveEndStep : IWaveEndStep
    {
        private readonly string id;
        private readonly List<string> executionOrder;

        public TestWaveEndStep(string id, int priority, List<string> executionOrder)
        {
            this.id = id;
            WaveEndPriority = priority;
            this.executionOrder = executionOrder;
        }

        public int WaveEndPriority { get; }

        public UniTask ExecuteWaveEndAsync(CancellationToken cancellationToken)
        {
            executionOrder.Add(id);
            return UniTask.CompletedTask;
        }
    }

    private sealed class TestWaveEndComponent : MonoBehaviour, IWaveEndStep
    {
        public bool Executed { get; private set; }
        public int WaveEndPriority => WaveEndPriorities.EntityCleanup;

        public UniTask ExecuteWaveEndAsync(CancellationToken cancellationToken)
        {
            Executed = true;
            return UniTask.CompletedTask;
        }
    }

    private sealed class AWaitingWaveEndStep : IWaveEndStep
    {
        public const string FinishedId = "waiting_finish";
        private readonly UniTask gate;
        private readonly List<string> executionOrder;

        public AWaitingWaveEndStep(UniTask gate, List<string> executionOrder)
        {
            this.gate = gate;
            this.executionOrder = executionOrder;
        }

        public int WaveEndPriority => WaveEndPriorities.EntityCleanup;

        public async UniTask ExecuteWaveEndAsync(CancellationToken cancellationToken)
        {
            executionOrder.Add("waiting_start");
            await gate.AttachExternalCancellation(cancellationToken);
            executionOrder.Add(FinishedId);
        }
    }

    private sealed class BReleasingWaveEndStep : IWaveEndStep
    {
        public const string StartedId = "releasing_start";
        private readonly UniTaskCompletionSource gate;
        private readonly List<string> executionOrder;

        public BReleasingWaveEndStep(UniTaskCompletionSource gate, List<string> executionOrder)
        {
            this.gate = gate;
            this.executionOrder = executionOrder;
        }

        public int WaveEndPriority => WaveEndPriorities.EntityCleanup;

        public UniTask ExecuteWaveEndAsync(CancellationToken cancellationToken)
        {
            executionOrder.Add(StartedId);
            gate.TrySetResult();
            return UniTask.CompletedTask;
        }
    }

    private sealed class TestEntity : Entity
    {
    }
}
