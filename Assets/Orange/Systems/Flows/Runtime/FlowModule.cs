using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Orange.Flows
{
    public interface IFlowModule
    {
        UniTask<FlowDirective> ExecuteAsync(FlowBoard board, CancellationToken cancellationToken);
    }

    public enum FlowDirectiveKind
    {
        Next = 0,
        Jump = 1,
        Stop = 2
    }

    /// <summary>
    /// Explicit control result returned by one flow module.
    /// </summary>
    public readonly struct FlowDirective
    {
        private FlowDirective(FlowDirectiveKind kind, FlowNodeId targetNode)
        {
            Kind = kind;
            TargetNode = targetNode;
        }

        public FlowDirectiveKind Kind { get; }
        public FlowNodeId TargetNode { get; }

        public static FlowDirective Next => new FlowDirective(FlowDirectiveKind.Next, default);
        public static FlowDirective Stop => new FlowDirective(FlowDirectiveKind.Stop, default);

        public static FlowDirective Jump(FlowNodeId targetNode)
        {
            if (!targetNode.IsValid)
            {
                throw new ArgumentException("Jump requires a valid target node.", nameof(targetNode));
            }

            return new FlowDirective(FlowDirectiveKind.Jump, targetNode);
        }
    }
}
