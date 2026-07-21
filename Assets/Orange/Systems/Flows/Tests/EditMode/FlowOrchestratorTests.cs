using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace Orange.Flows.Tests
{
    public sealed class FlowOrchestratorTests
    {
        private static readonly FlowKey<List<string>> CallsKey = new FlowKey<List<string>>("Calls");

        [Test]
        public void Board_StoresAndRemovesTypedValues()
        {
            FlowBoard board = new FlowBoard();
            FlowKey<int> countKey = new FlowKey<int>("Count");

            board.Set(countKey, 3);

            Assert.IsTrue(board.TryGet(countKey, out int count));
            Assert.AreEqual(3, count);
            Assert.AreEqual(3, board.Require(countKey));
            Assert.IsTrue(board.Remove(countKey));
            Assert.IsFalse(board.TryGet(countKey, out _));
            Assert.Throws<InvalidOperationException>(() => board.Require(countKey));
        }

        [Test]
        public void Run_ExecutesDefaultStepsInDeclaredOrder()
        {
            FlowId flowId = new FlowId("Test.Default");
            FlowOrchestrator orchestrator = new FlowOrchestrator();
            orchestrator.Define(
                new FlowBuilder(flowId)
                    .Step("First", new RecordModule("First"))
                    .Step("Second", new RecordModule("Second"))
                    .Build());

            FlowRunResult result = Run(orchestrator, flowId, CreateBoard());

            CollectionAssert.AreEqual(new[] { "First", "Second" }, result.Board.Require(CallsKey));
            Assert.AreEqual(FlowRunOutcome.Completed, result.Outcome);
            Assert.AreEqual(2, result.Trace.Count);
        }

        [Test]
        public void Run_AppliesRegisteredPatchInPriorityOrder()
        {
            FlowId flowId = new FlowId("Test.Patch");
            FlowOrchestrator orchestrator = new FlowOrchestrator();
            FlowNodeId firstNode = new FlowNodeId(flowId, "First");
            orchestrator.Define(
                new FlowBuilder(flowId)
                    .Step(firstNode, new RecordModule("First"))
                    .Step("Second", new RecordModule("Second"))
                    .Build());

            using (orchestrator.RegisterPatch(
                       "LowPriority",
                       FlowPatch.For(flowId).InsertAfter(firstNode, new RecordModule("Low")).Build(),
                       priority: 10))
            using (orchestrator.RegisterPatch(
                       "HighPriority",
                       FlowPatch.For(flowId).InsertAfter(firstNode, new RecordModule("High")).Build(),
                       priority: 20))
            {
                FlowRunResult result = Run(orchestrator, flowId, CreateBoard());

                CollectionAssert.AreEqual(
                    new[] { "First", "Low", "High", "Second" },
                    result.Board.Require(CallsKey));
            }
        }

        [Test]
        public void Run_RemovesPatchWhenRegistrationHandleIsDisposed()
        {
            FlowId flowId = new FlowId("Test.RemovePatch");
            FlowOrchestrator orchestrator = new FlowOrchestrator();
            FlowNodeId firstNode = new FlowNodeId(flowId, "First");
            orchestrator.Define(
                new FlowBuilder(flowId)
                    .Step(firstNode, new RecordModule("First"))
                    .Build());

            IDisposable registration = orchestrator.RegisterPatch(
                "Patch",
                FlowPatch.For(flowId).InsertAfter(firstNode, new RecordModule("Patched")).Build());
            registration.Dispose();

            FlowRunResult result = Run(orchestrator, flowId, CreateBoard());

            CollectionAssert.AreEqual(new[] { "First" }, result.Board.Require(CallsKey));
        }

        [Test]
        public void Run_ReplacesTheTargetModule()
        {
            FlowId flowId = new FlowId("Test.Replace");
            FlowOrchestrator orchestrator = new FlowOrchestrator();
            FlowNodeId firstNode = new FlowNodeId(flowId, "First");
            orchestrator.Define(
                new FlowBuilder(flowId)
                    .Step(firstNode, new RecordModule("Original"))
                    .Step("Second", new RecordModule("Second"))
                    .Build());

            using (orchestrator.RegisterPatch(
                       "Replace",
                       FlowPatch.For(flowId).Replace(firstNode, new RecordModule("Replacement")).Build()))
            {
                FlowRunResult result = Run(orchestrator, flowId, CreateBoard());

                CollectionAssert.AreEqual(
                    new[] { "Replacement", "Second" },
                    result.Board.Require(CallsKey));
            }
        }

        [Test]
        public void Run_RemovesTheTargetNode()
        {
            FlowId flowId = new FlowId("Test.RemoveNode");
            FlowOrchestrator orchestrator = new FlowOrchestrator();
            FlowNodeId middleNode = new FlowNodeId(flowId, "Middle");
            orchestrator.Define(
                new FlowBuilder(flowId)
                    .Step("First", new RecordModule("First"))
                    .Step(middleNode, new RecordModule("Middle"))
                    .Step("Last", new RecordModule("Last"))
                    .Build());

            using (orchestrator.RegisterPatch(
                       "Remove",
                       FlowPatch.For(flowId).Remove(middleNode).Build()))
            {
                FlowRunResult result = Run(orchestrator, flowId, CreateBoard());

                CollectionAssert.AreEqual(new[] { "First", "Last" }, result.Board.Require(CallsKey));
            }
        }

        [Test]
        public void Run_RedirectsTheTargetNodeNextLink()
        {
            FlowId flowId = new FlowId("Test.Redirect");
            FlowOrchestrator orchestrator = new FlowOrchestrator();
            FlowNodeId firstNode = new FlowNodeId(flowId, "First");
            FlowNodeId destinationNode = new FlowNodeId(flowId, "Destination");
            orchestrator.Define(
                new FlowBuilder(flowId)
                    .Step(firstNode, new RecordModule("First"))
                    .Step("Skipped", new RecordModule("Skipped"))
                    .Step(destinationNode, new RecordModule("Destination"))
                    .Build());

            using (orchestrator.RegisterPatch(
                       "Redirect",
                       FlowPatch.For(flowId).RedirectNext(firstNode, destinationNode).Build()))
            {
                FlowRunResult result = Run(orchestrator, flowId, CreateBoard());

                CollectionAssert.AreEqual(
                    new[] { "First", "Destination" },
                    result.Board.Require(CallsKey));
            }
        }

        [Test]
        public void Run_AllowsModuleToInsertTheNextStep()
        {
            FlowId flowId = new FlowId("Test.InsertNext");
            FlowOrchestrator orchestrator = new FlowOrchestrator();
            orchestrator.Define(
                new FlowBuilder(flowId)
                    .Step("First", new InsertNextModule("First", new RecordModule("Inserted")))
                    .Step("Second", new RecordModule("Second"))
                    .Build());

            FlowRunResult result = Run(orchestrator, flowId, CreateBoard());

            CollectionAssert.AreEqual(
                new[] { "First", "Inserted", "Second" },
                result.Board.Require(CallsKey));
        }

        [Test]
        public void Run_CanJumpToAnotherDeclaredNode()
        {
            FlowId flowId = new FlowId("Test.Jump");
            FlowOrchestrator orchestrator = new FlowOrchestrator();
            FlowNodeId finishNode = new FlowNodeId(flowId, "Finish");
            orchestrator.Define(
                new FlowBuilder(flowId)
                    .Step("Start", new JumpModule("Start", finishNode))
                    .Step("Skipped", new RecordModule("Skipped"))
                    .Step(finishNode, new RecordModule("Finish"))
                    .Build());

            FlowRunResult result = Run(orchestrator, flowId, CreateBoard());

            CollectionAssert.AreEqual(new[] { "Start", "Finish" }, result.Board.Require(CallsKey));
        }

        private static FlowBoard CreateBoard()
        {
            FlowBoard board = new FlowBoard();
            board.Set(CallsKey, new List<string>());
            return board;
        }

        private static FlowRunResult Run(FlowOrchestrator orchestrator, FlowId flowId, FlowBoard board)
        {
            return orchestrator.RunAsync(flowId, board, CancellationToken.None).GetAwaiter().GetResult();
        }

        private sealed class RecordModule : IFlowModule
        {
            private readonly string value;

            public RecordModule(string value)
            {
                this.value = value;
            }

            public UniTask<FlowDirective> ExecuteAsync(FlowBoard board, CancellationToken cancellationToken)
            {
                board.Require(CallsKey).Add(value);
                return UniTask.FromResult(FlowDirective.Next);
            }
        }

        private sealed class InsertNextModule : IFlowModule
        {
            private readonly string value;
            private readonly IFlowModule nextModule;

            public InsertNextModule(string value, IFlowModule nextModule)
            {
                this.value = value;
                this.nextModule = nextModule;
            }

            public UniTask<FlowDirective> ExecuteAsync(FlowBoard board, CancellationToken cancellationToken)
            {
                board.Require(CallsKey).Add(value);
                board.InsertNext(nextModule);
                return UniTask.FromResult(FlowDirective.Next);
            }
        }

        private sealed class JumpModule : IFlowModule
        {
            private readonly string value;
            private readonly FlowNodeId target;

            public JumpModule(string value, FlowNodeId target)
            {
                this.value = value;
                this.target = target;
            }

            public UniTask<FlowDirective> ExecuteAsync(FlowBoard board, CancellationToken cancellationToken)
            {
                board.Require(CallsKey).Add(value);
                return UniTask.FromResult(FlowDirective.Jump(target));
            }
        }
    }
}
