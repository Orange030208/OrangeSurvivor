using System.Collections.Generic;
using UnityEngine;

namespace Orange.GameServices
{
    [DefaultExecutionOrder(-9000)]
    [DisallowMultipleComponent]
    /// <summary>
    /// 面向场景的装配入口，用来把序列化的服务定义转换为运行中的 Host。
    /// </summary>
    public sealed class GameServiceRoot : MonoBehaviour
    {
        public const string DefaultScopeId = "Default";

        [SerializeField] private string scopeId = DefaultScopeId;
        [SerializeField] private bool bindAsDefault = true;
        [SerializeField] private bool dontDestroyOnLoad;
        [SerializeField] private GameServiceProfileMode profileMode = GameServiceProfileMode.ProfilesThenLocal;
        [SerializeField] private GameServiceProfile[] profiles;
        // 本地服务直接序列化在 Root 上，方便保持场景侧的归属关系和可见性。
        [SerializeReference] private List<GameService> localServices = new List<GameService>();

        private readonly List<GameServiceProfile> runtimeProfileInstances = new List<GameServiceProfile>();
        private GameServiceHost host;

        public string ScopeId => string.IsNullOrWhiteSpace(scopeId) ? DefaultScopeId : scopeId;
        public GameServiceHost Host => host;

        public T GetService<T>() where T : class
        {
            if (host == null)
            {
                throw new GameServiceException("GameServiceRoot has not created a host yet.");
            }

            return host.Get<T>();
        }

        public bool TryGetService<T>(out T service) where T : class
        {
            if (host == null)
            {
                service = null;
                return false;
            }

            return host.TryGet(out service);
        }

        private void Awake()
        {
            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }

            // 先把 Profile 和本地服务拍平成一个列表，再交给 Host 做校验、
            // 依赖排序和生命周期驱动。
            List<GameService> services = BuildServiceList();
            host = new GameServiceHost(this, ScopeId, services);
            try
            {
                GameServices.Bind(host, bindAsDefault);
                host.Attach();
            }
            catch
            {
                host.Dispose();
                GameServices.Unbind(host);
                host = null;
                throw;
            }
        }

        private void Start()
        {
            host?.Start();
        }

        private void Update()
        {
            host?.Update(Time.deltaTime, Time.unscaledDeltaTime);
        }

        private void FixedUpdate()
        {
            host?.FixedUpdate(Time.fixedDeltaTime);
        }

        private void LateUpdate()
        {
            host?.LateUpdate(Time.deltaTime);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            host?.ApplicationPause(pauseStatus);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            host?.ApplicationFocus(hasFocus);
        }

        private void OnDestroy()
        {
            if (host != null)
            {
                host.Dispose();
                GameServices.Unbind(host);
                host = null;
            }

            DestroyRuntimeProfileInstances();
        }

        private List<GameService> BuildServiceList()
        {
            List<GameService> services = new List<GameService>();
            if (profileMode == GameServiceProfileMode.LocalThenProfiles)
            {
                AppendLocalServices(services);
                AppendProfileServices(services);
            }
            else
            {
                AppendProfileServices(services);
                AppendLocalServices(services);
            }

            return services;
        }

        private void AppendProfileServices(List<GameService> services)
        {
            if (profiles == null)
            {
                return;
            }

            for (int i = 0; i < profiles.Length; i++)
            {
                GameServiceProfile profile = profiles[i];
                if (profile == null)
                {
                    services.Add(null);
                    continue;
                }

                GameServiceProfile runtimeProfile = Instantiate(profile);
                runtimeProfileInstances.Add(runtimeProfile);
                // 运行时克隆 Profile，避免多个 Root 共享同一组服务实例。
                runtimeProfile.AppendServices(services);
            }
        }

        private void AppendLocalServices(List<GameService> services)
        {
            for (int i = 0; i < localServices.Count; i++)
            {
                services.Add(localServices[i]);
            }
        }

        private void DestroyRuntimeProfileInstances()
        {
            for (int i = runtimeProfileInstances.Count - 1; i >= 0; i--)
            {
                GameServiceProfile profile = runtimeProfileInstances[i];
                if (profile == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(profile);
                }
                else
                {
                    DestroyImmediate(profile);
                }
            }

            runtimeProfileInstances.Clear();
        }
    }
}
