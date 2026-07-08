using System;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Orange.Services.Tests
{
    public class ServiceHostTests
    {
        [TearDown]
        public void TearDown()
        {
            TestHost.InstallAction = null;
        }

        [Test]
        public void ServiceHost_DrivesStartTickAndShutdown()
        {
            HostLifecycleService service = new HostLifecycleService();
            TestHost.InstallAction = registry =>
                registry.RegisterInstance<IHostService>(service).Eager();
            GameObject gameObject = new GameObject("ServiceHostTest");

            try
            {
                TestHost host = gameObject.AddComponent<TestHost>();
                host.EnsureAwakeForTest();

                host.InvokeStartForTest();
                host.InvokeUpdateForTest();

                Assert.IsTrue(service.Initialized);
                Assert.IsTrue(service.Started);
                Assert.AreEqual(1, service.TickCount);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }

            Assert.IsTrue(service.WasShutdown);
            Assert.IsTrue(service.Disposed);
        }

        private interface IHostService
        {
        }

        private sealed class HostLifecycleService :
            IHostService,
            IServiceInitializable,
            IServiceStartable,
            IServiceTickable,
            IServiceShutdown,
            IDisposable
        {
            public bool Initialized { get; private set; }
            public bool Started { get; private set; }
            public bool WasShutdown { get; private set; }
            public bool Disposed { get; private set; }
            public int TickCount { get; private set; }

            public void Initialize(ServiceLifecycleContext context)
            {
                Initialized = true;
                Assert.IsInstanceOf<TestHost>(context.Owner);
            }

            public void Start(ServiceLifecycleContext context)
            {
                Started = true;
            }

            public void Tick(float deltaTime)
            {
                TickCount++;
            }

            public void Shutdown(ServiceLifecycleContext context)
            {
                WasShutdown = true;
            }

            public void Dispose()
            {
                Disposed = true;
            }
        }

        private sealed class TestHost : ServiceHost
        {
            public static Action<IServiceRegistry> InstallAction;

            protected override void InstallServices(IServiceRegistry registry)
            {
                InstallAction?.Invoke(registry);
            }

            public void EnsureAwakeForTest()
            {
                if (Scope == null)
                {
                    Awake();
                }
            }

            public void InvokeStartForTest()
            {
                Start();
            }

            public void InvokeUpdateForTest()
            {
                Update();
            }
        }
    }
}
