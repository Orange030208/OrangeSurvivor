using System;

namespace Orange.GameServices
{
    public sealed class GameServiceDependencyRule
    {
        public GameServiceDependencyRule(Type contractType, bool required)
        {
            ContractType = contractType ?? throw new ArgumentNullException(nameof(contractType));
            Required = required;
        }

        public Type ContractType { get; }
        public bool Required { get; }
    }
}
