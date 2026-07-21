using System;

namespace Orange.Flows
{
    /// <summary>
    /// Stable identifier for one independently executable flow definition.
    /// </summary>
    public readonly struct FlowId : IEquatable<FlowId>
    {
        private readonly string value;

        public FlowId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Flow id cannot be null or whitespace.", nameof(value));
            }

            this.value = value;
        }

        public string Value => value ?? string.Empty;
        public bool IsValid => !string.IsNullOrWhiteSpace(value);

        public bool Equals(FlowId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is FlowId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(FlowId left, FlowId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(FlowId left, FlowId right)
        {
            return !left.Equals(right);
        }
    }

    /// <summary>
    /// Stable node identifier within one flow. Runtime patches always target this id, never a display label.
    /// </summary>
    public readonly struct FlowNodeId : IEquatable<FlowNodeId>
    {
        private readonly FlowId flowId;
        private readonly string value;

        public FlowNodeId(FlowId flowId, string value)
        {
            if (!flowId.IsValid)
            {
                throw new ArgumentException("Node id requires a valid flow id.", nameof(flowId));
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Node id cannot be null or whitespace.", nameof(value));
            }

            this.flowId = flowId;
            this.value = value;
        }

        public FlowId FlowId => flowId;
        public string Value => value ?? string.Empty;
        public bool IsValid => flowId.IsValid && !string.IsNullOrWhiteSpace(value);

        public bool Equals(FlowNodeId other)
        {
            return flowId.Equals(other.flowId) && string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is FlowNodeId other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (flowId.GetHashCode() * 397) ^ StringComparer.Ordinal.GetHashCode(Value);
            }
        }

        public override string ToString()
        {
            return $"{flowId}:{Value}";
        }

        public static bool operator ==(FlowNodeId left, FlowNodeId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(FlowNodeId left, FlowNodeId right)
        {
            return !left.Equals(right);
        }
    }
}
