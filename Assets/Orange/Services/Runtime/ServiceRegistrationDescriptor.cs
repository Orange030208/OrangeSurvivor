using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Orange.Services
{
    internal sealed class ServiceRegistrationDescriptor
    {
        private readonly List<Type> dependencies = new List<Type>();

        public ServiceRegistrationDescriptor(
            Type contractType,
            Type serviceType,
            Func<IServiceResolver, object> factory)
        {
            ContractType = contractType ?? throw new ArgumentNullException(nameof(contractType));
            ServiceType = serviceType ?? contractType;
            Factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public Type ContractType { get; }
        public Type ServiceType { get; private set; }
        public Func<IServiceResolver, object> Factory { get; }
        public bool Eager { get; set; }
        public IReadOnlyList<Type> Dependencies => new ReadOnlyCollection<Type>(dependencies);

        public void AddDependency(Type dependencyType)
        {
            if (dependencyType == null)
            {
                throw new ArgumentNullException(nameof(dependencyType));
            }

            if (dependencyType == ContractType)
            {
                throw new ServiceException(
                    $"Service '{FormatType(ContractType)}' cannot depend on itself.");
            }

            if (dependencies.Contains(dependencyType))
            {
                return;
            }

            dependencies.Add(dependencyType);
        }

        public object Create(IServiceResolver resolver)
        {
            object instance = Factory.Invoke(resolver);
            if (instance != null)
            {
                ServiceType = instance.GetType();
            }

            return instance;
        }

        private static string FormatType(Type type)
        {
            return type != null ? type.FullName : "<null>";
        }
    }
}
