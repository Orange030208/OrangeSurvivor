using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Orange.Flows
{
    public sealed class FlowNodeDefinition
    {
        public FlowNodeDefinition(FlowNodeId id, IFlowModule module, FlowNodeId? nextNodeId)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException("Flow node requires a valid id.", nameof(id));
            }

            Id = id;
            Module = module ?? throw new ArgumentNullException(nameof(module));
            NextNodeId = nextNodeId;
        }

        public FlowNodeId Id { get; }
        public IFlowModule Module { get; }
        public FlowNodeId? NextNodeId { get; }
    }

    /// <summary>
    /// Immutable code-defined flow graph. Runtime code must apply a patch instead of mutating this definition.
    /// </summary>
    public sealed class FlowDefinition
    {
        private readonly ReadOnlyDictionary<FlowNodeId, FlowNodeDefinition> nodes;

        internal FlowDefinition(FlowId id, FlowNodeId entryNodeId, Dictionary<FlowNodeId, FlowNodeDefinition> nodes)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException("Flow definition requires a valid id.", nameof(id));
            }

            if (!entryNodeId.IsValid || entryNodeId.FlowId != id)
            {
                throw new ArgumentException("Entry node must belong to the flow definition.", nameof(entryNodeId));
            }

            if (nodes == null || nodes.Count == 0)
            {
                throw new ArgumentException("Flow definition requires at least one node.", nameof(nodes));
            }

            if (!nodes.ContainsKey(entryNodeId))
            {
                throw new ArgumentException("Flow definition is missing its entry node.", nameof(entryNodeId));
            }

            foreach (KeyValuePair<FlowNodeId, FlowNodeDefinition> pair in nodes)
            {
                FlowNodeDefinition node = pair.Value;
                if (node == null || pair.Key.FlowId != id || node.Id != pair.Key)
                {
                    throw new ArgumentException("Flow definition contains an invalid node.", nameof(nodes));
                }

                if (node.NextNodeId.HasValue && !nodes.ContainsKey(node.NextNodeId.Value))
                {
                    throw new ArgumentException($"Flow node '{node.Id}' points to a missing next node.", nameof(nodes));
                }
            }

            Id = id;
            EntryNodeId = entryNodeId;
            this.nodes = new ReadOnlyDictionary<FlowNodeId, FlowNodeDefinition>(nodes);
        }

        public FlowId Id { get; }
        public FlowNodeId EntryNodeId { get; }
        public IReadOnlyDictionary<FlowNodeId, FlowNodeDefinition> Nodes => nodes;
    }

    /// <summary>
    /// Code-first builder for a flow graph. Consecutive Step calls create a linear default chain;
    /// SetNext can turn it into an arbitrary graph.
    /// </summary>
    public sealed class FlowBuilder
    {
        private readonly FlowId flowId;
        private readonly Dictionary<FlowNodeId, MutableNodeDefinition> nodes = new Dictionary<FlowNodeId, MutableNodeDefinition>();
        private FlowNodeId? entryNodeId;
        private FlowNodeId? previousNodeId;

        public FlowBuilder(FlowId flowId)
        {
            if (!flowId.IsValid)
            {
                throw new ArgumentException("Flow builder requires a valid flow id.", nameof(flowId));
            }

            this.flowId = flowId;
        }

        public FlowBuilder Step(string nodeId, IFlowModule module)
        {
            return Step(new FlowNodeId(flowId, nodeId), module);
        }

        public FlowBuilder Step(FlowNodeId nodeId, IFlowModule module)
        {
            if (nodeId.FlowId != flowId)
            {
                throw new ArgumentException("Flow node must belong to this builder's flow.", nameof(nodeId));
            }

            if (module == null)
            {
                throw new ArgumentNullException(nameof(module));
            }

            if (nodes.ContainsKey(nodeId))
            {
                throw new InvalidOperationException($"Flow '{flowId}' already contains node '{nodeId.Value}'.");
            }

            MutableNodeDefinition node = new MutableNodeDefinition(nodeId, module);
            nodes.Add(nodeId, node);
            if (!entryNodeId.HasValue)
            {
                entryNodeId = nodeId;
            }

            if (previousNodeId.HasValue)
            {
                nodes[previousNodeId.Value].NextNodeId = nodeId;
            }

            previousNodeId = nodeId;
            return this;
        }

        public FlowBuilder SetEntry(FlowNodeId nodeId)
        {
            RequireExistingNode(nodeId);
            entryNodeId = nodeId;
            return this;
        }

        public FlowBuilder SetNext(FlowNodeId sourceNodeId, FlowNodeId? targetNodeId)
        {
            MutableNodeDefinition sourceNode = RequireExistingNode(sourceNodeId);
            if (targetNodeId.HasValue)
            {
                RequireExistingNode(targetNodeId.Value);
            }

            sourceNode.NextNodeId = targetNodeId;
            return this;
        }

        public FlowDefinition Build()
        {
            if (!entryNodeId.HasValue)
            {
                throw new InvalidOperationException($"Flow '{flowId}' has no entry node.");
            }

            Dictionary<FlowNodeId, FlowNodeDefinition> immutableNodes = new Dictionary<FlowNodeId, FlowNodeDefinition>();
            foreach (KeyValuePair<FlowNodeId, MutableNodeDefinition> pair in nodes)
            {
                MutableNodeDefinition node = pair.Value;
                immutableNodes.Add(node.Id, new FlowNodeDefinition(node.Id, node.Module, node.NextNodeId));
            }

            return new FlowDefinition(flowId, entryNodeId.Value, immutableNodes);
        }

        private MutableNodeDefinition RequireExistingNode(FlowNodeId nodeId)
        {
            if (nodeId.FlowId != flowId || !nodes.TryGetValue(nodeId, out MutableNodeDefinition node))
            {
                throw new InvalidOperationException($"Flow '{flowId}' does not contain node '{nodeId}'.");
            }

            return node;
        }

        private sealed class MutableNodeDefinition
        {
            public MutableNodeDefinition(FlowNodeId id, IFlowModule module)
            {
                Id = id;
                Module = module;
            }

            public FlowNodeId Id { get; }
            public IFlowModule Module { get; }
            public FlowNodeId? NextNodeId { get; set; }
        }
    }
}
