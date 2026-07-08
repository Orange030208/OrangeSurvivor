using System.Collections.Generic;

namespace Orange.GameServices
{
    public sealed class GameServiceSnapshot
    {
        public GameServiceSnapshot(
            string scopeId,
            GameServiceState state,
            IReadOnlyList<GameServiceEntrySnapshot> services,
            IReadOnlyList<GameServiceValidationMessage> validationMessages)
        {
            ScopeId = scopeId;
            State = state;
            Services = services;
            ValidationMessages = validationMessages;
        }

        public string ScopeId { get; }
        public GameServiceState State { get; }
        public IReadOnlyList<GameServiceEntrySnapshot> Services { get; }
        public IReadOnlyList<GameServiceValidationMessage> ValidationMessages { get; }
    }
}
