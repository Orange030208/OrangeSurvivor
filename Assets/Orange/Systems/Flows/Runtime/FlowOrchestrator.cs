using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Orange.Flows
{
    public enum FlowRunOutcome
    {
        Completed = 0,
        Stopped = 1
    }

    public sealed class FlowTraceEntry
    {
        internal FlowTraceEntry(FlowNodeId? nodeId, IFlowModule module, FlowDirective directive)
        {
            NodeId = nodeId;
            ModuleType = module != null ? module.GetType() : typeof(object);
            Directive = directive;
        }

        public FlowNodeId? NodeId { get; }
        public Type ModuleType { get; }
        public FlowDirective Directive { get; }
    }

    public sealed class FlowRunResult
    {
        internal FlowRunResult(FlowId flowId, FlowBoard board, FlowRunOutcome outcome, List<FlowTraceEntry> trace)
        {
            FlowId = flowId;
            Board = board;
            Outcome = outcome;
            Trace = new ReadOnlyCollection<FlowTraceEntry>(trace);
        }

        public FlowId FlowId { get; }
        public FlowBoard Board { get; }
        public FlowRunOutcome Outcome { get; }
        public IReadOnlyList<FlowTraceEntry> Trace { get; }
    }

    /// <summary>
    /// Executes immutable code-defined flows after applying currently registered runtime patches.
    /// It is intentionally a pure C# type so game services can own it without adding a scene component.
    /// </summary>
    public sealed class FlowOrchestrator
    {
        private const int DEFAULT_MAXIMUM_STEP_COUNT = 10000;

        private readonly Dictionary<FlowId, FlowDefinition> definitions = new Dictionary<FlowId, FlowDefinition>();
        private readonly List<RegisteredPatch> patches = new List<RegisteredPatch>();
        private long nextPatchRegistrationOrder;

        public int MaximumStepCount { get; set; } = DEFAULT_MAXIMUM_STEP_COUNT;

        public void Define(FlowDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (definitions.ContainsKey(definition.Id))
            {
                throw new InvalidOperationException($"Flow '{definition.Id}' has already been defined.");
            }

            definitions.Add(definition.Id, definition);
        }

        public IDisposable RegisterPatch(string sourceId, FlowPatch patch, int priority = 0)
        {
            if (string.IsNullOrWhiteSpace(sourceId))
            {
                throw new ArgumentException("Patch source id cannot be null or whitespace.", nameof(sourceId));
            }

            if (patch == null)
            {
                throw new ArgumentNullException(nameof(patch));
            }

            if (!definitions.ContainsKey(patch.FlowId))
            {
                throw new InvalidOperationException($"Cannot register a patch for undefined flow '{patch.FlowId}'.");
            }

            RegisteredPatch registeredPatch = new RegisteredPatch(
                sourceId,
                patch,
                priority,
                nextPatchRegistrationOrder++);
            patches.Add(registeredPatch);
            return new PatchRegistrationHandle(this, registeredPatch);
        }

        public async UniTask<FlowRunResult> RunAsync(
            FlowId flowId,
            FlowBoard board,
            CancellationToken cancellationToken = default)
        {
            if (!flowId.IsValid)
            {
                throw new ArgumentException("Flow run requires a valid flow id.", nameof(flowId));
            }

            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (!definitions.TryGetValue(flowId, out FlowDefinition definition))
            {
                throw new InvalidOperationException($"Flow '{flowId}' has not been defined.");
            }

            ExecutionPlan plan = new ExecutionPlan(definition);
            ApplyPatches(plan, flowId);

            int maximumStepCount = Math.Max(1, MaximumStepCount);
            int executedStepCount = 0;
            List<FlowTraceEntry> trace = new List<FlowTraceEntry>();
            ExecutionNode currentNode = plan.EntryNode;
            while (currentNode != null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (executedStepCount++ >= maximumStepCount)
                {
                    throw new InvalidOperationException(
                        $"Flow '{flowId}' exceeded its maximum step count of {maximumStepCount}. Check for an unintended loop.");
                }

                FlowDirective directive;
                List<IFlowModule> insertedModules;
                board.BeginModuleExecution();
                try
                {
                    directive = await currentNode.Module.ExecuteAsync(board, cancellationToken);
                }
                finally
                {
                    insertedModules = board.EndModuleExecution();
                }

                if (insertedModules != null)
                {
                    if (directive.Kind != FlowDirectiveKind.Next)
                    {
                        throw new InvalidOperationException(
                            $"Flow module '{currentNode.Module.GetType().Name}' inserted next modules but returned '{directive.Kind}'.");
                    }

                    plan.InsertAfterRuntimeNode(currentNode, insertedModules);
                }

                trace.Add(new FlowTraceEntry(currentNode.BaseNodeId, currentNode.Module, directive));
                switch (directive.Kind)
                {
                    case FlowDirectiveKind.Next:
                        currentNode = currentNode.Next;
                        break;

                    case FlowDirectiveKind.Jump:
                        currentNode = plan.RequireBaseNode(directive.TargetNode);
                        break;

                    case FlowDirectiveKind.Stop:
                        return new FlowRunResult(flowId, board, FlowRunOutcome.Stopped, trace);

                    default:
                        throw new InvalidOperationException($"Unsupported flow directive '{directive.Kind}'.");
                }
            }

            return new FlowRunResult(flowId, board, FlowRunOutcome.Completed, trace);
        }

        private void ApplyPatches(ExecutionPlan plan, FlowId flowId)
        {
            List<RegisteredPatch> orderedPatches = new List<RegisteredPatch>();
            for (int i = 0; i < patches.Count; i++)
            {
                RegisteredPatch patch = patches[i];
                if (patch.Patch.FlowId == flowId)
                {
                    orderedPatches.Add(patch);
                }
            }

            orderedPatches.Sort(RegisteredPatchComparer.Instance);
            for (int i = 0; i < orderedPatches.Count; i++)
            {
                RegisteredPatch patch = orderedPatches[i];
                IReadOnlyList<FlowPatchOperation> operations = patch.Patch.Operations;
                for (int j = 0; j < operations.Count; j++)
                {
                    plan.Apply(operations[j]);
                }
            }
        }

        private void UnregisterPatch(RegisteredPatch registeredPatch)
        {
            patches.Remove(registeredPatch);
        }

        private sealed class PatchRegistrationHandle : IDisposable
        {
            private FlowOrchestrator owner;
            private RegisteredPatch registeredPatch;

            public PatchRegistrationHandle(FlowOrchestrator owner, RegisteredPatch registeredPatch)
            {
                this.owner = owner;
                this.registeredPatch = registeredPatch;
            }

            public void Dispose()
            {
                if (owner == null)
                {
                    return;
                }

                owner.UnregisterPatch(registeredPatch);
                owner = null;
                registeredPatch = null;
            }
        }

        private sealed class RegisteredPatch
        {
            public RegisteredPatch(string sourceId, FlowPatch patch, int priority, long registrationOrder)
            {
                SourceId = sourceId;
                Patch = patch;
                Priority = priority;
                RegistrationOrder = registrationOrder;
            }

            public string SourceId { get; }
            public FlowPatch Patch { get; }
            public int Priority { get; }
            public long RegistrationOrder { get; }
        }

        private sealed class RegisteredPatchComparer : IComparer<RegisteredPatch>
        {
            public static readonly RegisteredPatchComparer Instance = new RegisteredPatchComparer();

            public int Compare(RegisteredPatch left, RegisteredPatch right)
            {
                int priorityComparison = left.Priority.CompareTo(right.Priority);
                return priorityComparison != 0
                    ? priorityComparison
                    : left.RegistrationOrder.CompareTo(right.RegistrationOrder);
            }
        }

        private sealed class ExecutionPlan
        {
            private readonly Dictionary<FlowNodeId, ExecutionNode> baseNodes = new Dictionary<FlowNodeId, ExecutionNode>();
            private readonly List<ExecutionNode> nodes = new List<ExecutionNode>();
            private readonly Dictionary<FlowNodeId, ExecutionNode> beforeTailNodes = new Dictionary<FlowNodeId, ExecutionNode>();
            private readonly Dictionary<FlowNodeId, ExecutionNode> afterTailNodes = new Dictionary<FlowNodeId, ExecutionNode>();

            public ExecutionPlan(FlowDefinition definition)
            {
                foreach (KeyValuePair<FlowNodeId, FlowNodeDefinition> pair in definition.Nodes)
                {
                    FlowNodeDefinition definitionNode = pair.Value;
                    ExecutionNode runtimeNode = new ExecutionNode(definitionNode.Id, definitionNode.Module);
                    baseNodes.Add(definitionNode.Id, runtimeNode);
                    nodes.Add(runtimeNode);
                }

                foreach (KeyValuePair<FlowNodeId, FlowNodeDefinition> pair in definition.Nodes)
                {
                    FlowNodeDefinition definitionNode = pair.Value;
                    if (definitionNode.NextNodeId.HasValue)
                    {
                        baseNodes[definitionNode.Id].Next = baseNodes[definitionNode.NextNodeId.Value];
                    }
                }

                EntryNode = baseNodes[definition.EntryNodeId];
            }

            public ExecutionNode EntryNode { get; private set; }

            public void Apply(FlowPatchOperation operation)
            {
                switch (operation.Kind)
                {
                    case FlowPatchOperationKind.InsertBefore:
                        InsertBefore(operation.TargetNodeId, operation.Module);
                        break;

                    case FlowPatchOperationKind.InsertAfter:
                        InsertAfter(operation.TargetNodeId, operation.Module);
                        break;

                    case FlowPatchOperationKind.Replace:
                        RequireBaseNode(operation.TargetNodeId).Module = operation.Module;
                        break;

                    case FlowPatchOperationKind.Remove:
                        Remove(operation.TargetNodeId);
                        break;

                    case FlowPatchOperationKind.RedirectNext:
                        RedirectNext(operation.TargetNodeId, operation.RedirectTargetNodeId);
                        break;

                    default:
                        throw new InvalidOperationException($"Unsupported patch operation '{operation.Kind}'.");
                }
            }

            public ExecutionNode RequireBaseNode(FlowNodeId nodeId)
            {
                if (!baseNodes.TryGetValue(nodeId, out ExecutionNode node))
                {
                    throw new InvalidOperationException($"Flow patch targets missing node '{nodeId}'.");
                }

                return node;
            }

            public void InsertAfterRuntimeNode(ExecutionNode node, IReadOnlyList<IFlowModule> modules)
            {
                ExecutionNode tail = node;
                for (int i = 0; i < modules.Count; i++)
                {
                    ExecutionNode insertedNode = new ExecutionNode(null, modules[i]);
                    InsertAfterNode(tail, insertedNode);
                    tail = insertedNode;
                }
            }

            private void InsertBefore(FlowNodeId targetNodeId, IFlowModule module)
            {
                ExecutionNode targetNode = RequireBaseNode(targetNodeId);
                ExecutionNode insertedNode = new ExecutionNode(null, module);
                if (beforeTailNodes.TryGetValue(targetNodeId, out ExecutionNode previousTail))
                {
                    InsertAfterNode(previousTail, insertedNode);
                }
                else
                {
                    ReplaceInboundLinks(targetNode, insertedNode);
                    insertedNode.Next = targetNode;
                }

                beforeTailNodes[targetNodeId] = insertedNode;
                nodes.Add(insertedNode);
            }

            private void InsertAfter(FlowNodeId targetNodeId, IFlowModule module)
            {
                ExecutionNode targetNode = RequireBaseNode(targetNodeId);
                ExecutionNode tail = afterTailNodes.TryGetValue(targetNodeId, out ExecutionNode currentTail)
                    ? currentTail
                    : targetNode;
                ExecutionNode insertedNode = new ExecutionNode(null, module);
                InsertAfterNode(tail, insertedNode);
                afterTailNodes[targetNodeId] = insertedNode;
                nodes.Add(insertedNode);
            }

            private void RedirectNext(FlowNodeId targetNodeId, FlowNodeId redirectTargetNodeId)
            {
                ExecutionNode targetNode = RequireBaseNode(targetNodeId);
                ExecutionNode tail = afterTailNodes.TryGetValue(targetNodeId, out ExecutionNode currentTail)
                    ? currentTail
                    : targetNode;
                tail.Next = RequireBaseNode(redirectTargetNodeId);
            }

            private void Remove(FlowNodeId targetNodeId)
            {
                ExecutionNode targetNode = RequireBaseNode(targetNodeId);
                ReplaceInboundLinks(targetNode, targetNode.Next);
                baseNodes.Remove(targetNodeId);
                beforeTailNodes.Remove(targetNodeId);
                afterTailNodes.Remove(targetNodeId);
                nodes.Remove(targetNode);
            }

            private void ReplaceInboundLinks(ExecutionNode targetNode, ExecutionNode replacementNode)
            {
                if (EntryNode == targetNode)
                {
                    EntryNode = replacementNode;
                }

                for (int i = 0; i < nodes.Count; i++)
                {
                    ExecutionNode candidate = nodes[i];
                    if (candidate.Next == targetNode)
                    {
                        candidate.Next = replacementNode;
                    }
                }
            }

            private static void InsertAfterNode(ExecutionNode targetNode, ExecutionNode insertedNode)
            {
                insertedNode.Next = targetNode.Next;
                targetNode.Next = insertedNode;
            }
        }

        private sealed class ExecutionNode
        {
            public ExecutionNode(FlowNodeId? baseNodeId, IFlowModule module)
            {
                BaseNodeId = baseNodeId;
                Module = module ?? throw new ArgumentNullException(nameof(module));
            }

            public FlowNodeId? BaseNodeId { get; }
            public IFlowModule Module { get; set; }
            public ExecutionNode Next { get; set; }
        }
    }
}
