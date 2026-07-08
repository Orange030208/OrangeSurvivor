using System;

namespace Orange.GameServices
{
    public sealed class GameServiceValidationMessage
    {
        public GameServiceValidationMessage(
            GameServiceValidationSeverity severity,
            string message,
            Type serviceType = null,
            Type contractType = null)
        {
            Severity = severity;
            Message = message ?? string.Empty;
            ServiceType = serviceType;
            ContractType = contractType;
        }

        public GameServiceValidationSeverity Severity { get; }
        public string Message { get; }
        public Type ServiceType { get; }
        public Type ContractType { get; }

        public override string ToString()
        {
            string serviceName = ServiceType != null ? GameServiceTypeCache.GetDisplayName(ServiceType) : "Service";
            string contractName = ContractType != null ? $" Contract={GameServiceTypeCache.GetDisplayName(ContractType)}" : string.Empty;
            return $"[{Severity}] {serviceName}:{contractName} {Message}";
        }
    }
}
