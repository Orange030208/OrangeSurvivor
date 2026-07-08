namespace Orange.Services
{
    internal sealed class ServiceNode
    {
        public ServiceNode(ServiceRegistrationDescriptor descriptor)
        {
            Descriptor = descriptor;
        }

        public ServiceRegistrationDescriptor Descriptor { get; }
        public object Instance { get; private set; }
        public bool IsCreating { get; set; }
        public bool IsInitialized { get; set; }
        public bool IsStarted { get; set; }
        public bool IsShutdown { get; set; }
        public bool HasInstance => Instance != null;

        public void SetInstance(object instance)
        {
            Instance = instance;
        }
    }
}
