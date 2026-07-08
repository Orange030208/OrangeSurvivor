using System;

namespace Orange.GameServices
{
    public sealed class GameServiceEntrySnapshot
    {
        public GameServiceEntrySnapshot(
            Type serviceType,
            bool enabled,
            GameServiceState state,
            int order,
            GameServiceTickMode tickMode,
            GameServiceExecutionPolicy executionPolicy)
        {
            ServiceType = serviceType;
            Enabled = enabled;
            State = state;
            Order = order;
            TickMode = tickMode;
            ExecutionPolicy = executionPolicy;
        }

        public Type ServiceType { get; }
        public bool Enabled { get; }
        public GameServiceState State { get; }
        public int Order { get; }
        public GameServiceTickMode TickMode { get; }
        public GameServiceExecutionPolicy ExecutionPolicy { get; }
    }
}
