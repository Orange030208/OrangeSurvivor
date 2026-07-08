using System;

namespace Orange.Services
{
    /// <summary>
    /// 服务注册、解析、依赖分析或生命周期执行失败时抛出的异常。
    /// </summary>
    public class ServiceException : Exception
    {
        public ServiceException(string message)
            : base(message)
        {
        }

        public ServiceException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public ServiceException(
            string message,
            Type contractType,
            Type serviceType,
            ServiceLifecyclePhase phase,
            string scopeName,
            Exception innerException = null)
            : base(message, innerException)
        {
            ContractType = contractType;
            ServiceType = serviceType;
            Phase = phase;
            ScopeName = scopeName;
        }

        public Type ContractType { get; }
        public Type ServiceType { get; }
        public ServiceLifecyclePhase Phase { get; }
        public string ScopeName { get; }
    }
}
