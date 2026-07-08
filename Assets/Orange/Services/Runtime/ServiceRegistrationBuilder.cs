using System;

namespace Orange.Services
{
    /// <summary>
    /// 单个服务注册项的链式配置入口。
    /// </summary>
    public sealed class ServiceRegistrationBuilder<TContract> where TContract : class
    {
        private readonly ServiceRegistry owner;
        private readonly ServiceRegistrationDescriptor descriptor;

        internal ServiceRegistrationBuilder(ServiceRegistry owner, ServiceRegistrationDescriptor descriptor)
        {
            this.owner = owner;
            this.descriptor = descriptor;
        }

        public ServiceRegistrationBuilder<TContract> DependsOn<TService>() where TService : class
        {
            owner.EnsureMutable();
            descriptor.AddDependency(typeof(TService));
            return this;
        }

        public ServiceRegistrationBuilder<TContract> Eager()
        {
            owner.EnsureMutable();
            descriptor.Eager = true;
            return this;
        }

        public ServiceRegistrationBuilder<TContract> Lazy()
        {
            owner.EnsureMutable();
            descriptor.Eager = false;
            return this;
        }
    }
}
