using UnityEngine;

namespace Orange.Services
{
    /// <summary>
    /// 传递给服务生命周期回调的上下文。
    /// </summary>
    public sealed class ServiceLifecycleContext
    {
        internal ServiceLifecycleContext(
            IServiceScope scope,
            IServiceResolver resolver,
            ServiceLifecyclePhase phase,
            Object owner)
        {
            Scope = scope;
            Resolver = resolver;
            Phase = phase;
            Owner = owner;
        }

        /// <summary>
        /// 当前接收生命周期回调的服务所属作用域。
        /// </summary>
        public IServiceScope Scope { get; }

        /// <summary>
        /// 可解析当前作用域可见服务的解析器。
        /// </summary>
        public IServiceResolver Resolver { get; }

        /// <summary>
        /// 当前生命周期阶段。
        /// </summary>
        public ServiceLifecyclePhase Phase { get; }

        /// <summary>
        /// 持有或创建该作用域的 Unity 对象，通常是一个 ServiceHost。
        /// </summary>
        public Object Owner { get; }
    }
}
