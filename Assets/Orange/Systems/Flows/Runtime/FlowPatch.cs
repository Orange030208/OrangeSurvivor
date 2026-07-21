using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Orange.Flows
{
    public enum FlowPatchOperationKind
    {
        InsertBefore = 0,
        InsertAfter = 1,
        Replace = 2,
        Remove = 3,
        RedirectNext = 4
    }

    public readonly struct FlowPatchOperation
    {
        private FlowPatchOperation(
            FlowPatchOperationKind kind,
            FlowNodeId targetNodeId,
            IFlowModule module,
            FlowNodeId redirectTargetNodeId)
        {
            Kind = kind;
            TargetNodeId = targetNodeId;
            Module = module;
            RedirectTargetNodeId = redirectTargetNodeId;
        }

        public FlowPatchOperationKind Kind { get; }
        public FlowNodeId TargetNodeId { get; }
        public IFlowModule Module { get; }
        public FlowNodeId RedirectTargetNodeId { get; }

        public static FlowPatchOperation InsertBefore(FlowNodeId targetNodeId, IFlowModule module)
        {
            return CreateWithModule(FlowPatchOperationKind.InsertBefore, targetNodeId, module);
        }

        public static FlowPatchOperation InsertAfter(FlowNodeId targetNodeId, IFlowModule module)
        {
            return CreateWithModule(FlowPatchOperationKind.InsertAfter, targetNodeId, module);
        }

        public static FlowPatchOperation Replace(FlowNodeId targetNodeId, IFlowModule module)
        {
            return CreateWithModule(FlowPatchOperationKind.Replace, targetNodeId, module);
        }

        public static FlowPatchOperation Remove(FlowNodeId targetNodeId)
        {
            return new FlowPatchOperation(FlowPatchOperationKind.Remove, RequireNode(targetNodeId), null, default);
        }

        public static FlowPatchOperation RedirectNext(FlowNodeId targetNodeId, FlowNodeId redirectTargetNodeId)
        {
            FlowNodeId target = RequireNode(targetNodeId);
            FlowNodeId redirectTarget = RequireNode(redirectTargetNodeId);
            if (target.FlowId != redirectTarget.FlowId)
            {
                throw new ArgumentException("Redirect nodes must belong to the same flow.", nameof(redirectTargetNodeId));
            }

            return new FlowPatchOperation(FlowPatchOperationKind.RedirectNext, target, null, redirectTarget);
        }

        private static FlowPatchOperation CreateWithModule(
            FlowPatchOperationKind kind,
            FlowNodeId targetNodeId,
            IFlowModule module)
        {
            if (module == null)
            {
                throw new ArgumentNullException(nameof(module));
            }

            return new FlowPatchOperation(kind, RequireNode(targetNodeId), module, default);
        }

        private static FlowNodeId RequireNode(FlowNodeId nodeId)
        {
            if (!nodeId.IsValid)
            {
                throw new ArgumentException("Patch operation requires a valid node id.", nameof(nodeId));
            }

            return nodeId;
        }
    }

    public sealed class FlowPatch
    {
        internal FlowPatch(FlowId flowId, List<FlowPatchOperation> operations)
        {
            if (!flowId.IsValid)
            {
                throw new ArgumentException("Flow patch requires a valid flow id.", nameof(flowId));
            }

            if (operations == null || operations.Count == 0)
            {
                throw new ArgumentException("Flow patch requires at least one operation.", nameof(operations));
            }

            for (int i = 0; i < operations.Count; i++)
            {
                if (operations[i].TargetNodeId.FlowId != flowId)
                {
                    throw new ArgumentException("Patch operation targets a different flow.", nameof(operations));
                }
            }

            FlowId = flowId;
            Operations = new ReadOnlyCollection<FlowPatchOperation>(operations);
        }

        public FlowId FlowId { get; }
        public IReadOnlyList<FlowPatchOperation> Operations { get; }

        public static FlowPatchBuilder For(FlowId flowId)
        {
            return new FlowPatchBuilder(flowId);
        }
    }

    public sealed class FlowPatchBuilder
    {
        private readonly FlowId flowId;
        private readonly List<FlowPatchOperation> operations = new List<FlowPatchOperation>();

        internal FlowPatchBuilder(FlowId flowId)
        {
            if (!flowId.IsValid)
            {
                throw new ArgumentException("Flow patch builder requires a valid flow id.", nameof(flowId));
            }

            this.flowId = flowId;
        }

        public FlowPatchBuilder InsertBefore(FlowNodeId targetNodeId, IFlowModule module)
        {
            operations.Add(FlowPatchOperation.InsertBefore(targetNodeId, module));
            return this;
        }

        public FlowPatchBuilder InsertAfter(FlowNodeId targetNodeId, IFlowModule module)
        {
            operations.Add(FlowPatchOperation.InsertAfter(targetNodeId, module));
            return this;
        }

        public FlowPatchBuilder Replace(FlowNodeId targetNodeId, IFlowModule module)
        {
            operations.Add(FlowPatchOperation.Replace(targetNodeId, module));
            return this;
        }

        public FlowPatchBuilder Remove(FlowNodeId targetNodeId)
        {
            operations.Add(FlowPatchOperation.Remove(targetNodeId));
            return this;
        }

        public FlowPatchBuilder RedirectNext(FlowNodeId targetNodeId, FlowNodeId redirectTargetNodeId)
        {
            operations.Add(FlowPatchOperation.RedirectNext(targetNodeId, redirectTargetNodeId));
            return this;
        }

        public FlowPatch Build()
        {
            return new FlowPatch(flowId, new List<FlowPatchOperation>(operations));
        }
    }
}
