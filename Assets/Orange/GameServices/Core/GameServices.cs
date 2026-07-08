using System.Collections.Generic;
using UnityEngine;

namespace Orange.GameServices
{
    public static class GameServices
    {
        private static readonly Dictionary<string, GameServiceHost> hostsByScope = new Dictionary<string, GameServiceHost>();
        private static GameServiceHost defaultHost;

        public static bool IsReady => defaultHost != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            hostsByScope.Clear();
            defaultHost = null;
        }

        public static T Get<T>() where T : class
        {
            if (defaultHost == null)
            {
                throw new GameServiceException("Default GameServices scope is not bound.");
            }

            return defaultHost.Get<T>();
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            if (defaultHost == null)
            {
                service = null;
                return false;
            }

            return defaultHost.TryGet(out service);
        }

        public static GameServiceResolver For(string scopeId)
        {
            return TryGetHost(scopeId, out GameServiceHost host)
                ? new GameServiceResolver(host)
                : new GameServiceResolver(null);
        }

        public static bool TryCaptureSnapshot(out GameServiceSnapshot snapshot)
        {
            return GameServiceDiagnostics.TryCaptureDefault(out snapshot);
        }

        internal static void Bind(GameServiceHost host, bool bindAsDefault)
        {
            if (host == null)
            {
                throw new GameServiceException("Cannot bind a null GameServiceHost.");
            }

            if (hostsByScope.TryGetValue(host.ScopeId, out GameServiceHost existingHost) && existingHost != host)
            {
                throw new GameServiceException($"GameServices scope '{host.ScopeId}' is already bound.");
            }

            hostsByScope[host.ScopeId] = host;
            if (bindAsDefault || defaultHost == null)
            {
                defaultHost = host;
            }
        }

        internal static void Unbind(GameServiceHost host)
        {
            if (host == null)
            {
                return;
            }

            if (hostsByScope.TryGetValue(host.ScopeId, out GameServiceHost existingHost) && existingHost == host)
            {
                hostsByScope.Remove(host.ScopeId);
            }

            if (defaultHost == host)
            {
                defaultHost = null;
                foreach (KeyValuePair<string, GameServiceHost> pair in hostsByScope)
                {
                    defaultHost = pair.Value;
                    break;
                }
            }
        }

        internal static bool TryGetHost(out GameServiceHost host)
        {
            host = defaultHost;
            return host != null;
        }

        internal static bool TryGetHost(string scopeId, out GameServiceHost host)
        {
            if (string.IsNullOrWhiteSpace(scopeId))
            {
                host = defaultHost;
                return host != null;
            }

            return hostsByScope.TryGetValue(scopeId, out host);
        }
    }
}
