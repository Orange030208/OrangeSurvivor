using System;

namespace Orange.UIFramework
{
    public sealed class OpenContext
    {
        public OpenContext(
            Type viewType,
            string viewId,
            string instanceId,
            ViewKind kind,
            object payload,
            int requestVersion)
        {
            ViewType = viewType ?? throw new ArgumentNullException(nameof(viewType));

            if (string.IsNullOrWhiteSpace(instanceId))
            {
                throw new ArgumentException("OpenContext requires a non-empty instance id.", nameof(instanceId));
            }

            ViewId = viewId ?? string.Empty;
            InstanceId = instanceId;
            Kind = kind;
            Payload = payload;
            RequestVersion = requestVersion;
        }

        public Type ViewType { get; }
        public string ViewId { get; }
        public string InstanceId { get; }
        public ViewKind Kind { get; }
        public object Payload { get; }
        public int RequestVersion { get; }

        public TPayload GetPayload<TPayload>() where TPayload : class
        {
            return Payload as TPayload;
        }

        public bool TryGetPayload<TPayload>(out TPayload payload) where TPayload : class
        {
            payload = Payload as TPayload;
            return payload != null;
        }
    }
}
