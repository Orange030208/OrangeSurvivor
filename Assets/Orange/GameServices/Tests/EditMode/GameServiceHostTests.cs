using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Orange.GameServices.Tests
{
    public sealed class GameServiceHostTests
    {
        [Test]
        public void Host_AttachesStartsTicksAndDisposesService()
        {
            GameObject rootObject = new GameObject("Game Service Root");
            GameServiceRoot root = rootObject.AddComponent<GameServiceRoot>();
            RecordingService service = new RecordingService();
            GameServiceHost host = new GameServiceHost(root, "Test", new GameService[] { service });

            try
            {
                host.Attach();
                Assert.AreEqual(GameServiceState.Attached, service.State);
                Assert.AreEqual(1, service.AttachCount);
                Assert.AreSame(service, host.Get<RecordingService>());

                host.Start();
                Assert.AreEqual(GameServiceState.Running, service.State);
                Assert.AreEqual(1, service.StartCount);

                host.Update(0.25f, 1f);
                Assert.AreEqual(1, service.UpdateCount);
                Assert.AreEqual(0.25f, service.LastUpdateDelta);

                host.FixedUpdate(0.02f);
                Assert.AreEqual(1, service.FixedUpdateCount);

                host.LateUpdate(0.33f);
                Assert.AreEqual(1, service.LateUpdateCount);
            }
            finally
            {
                host.Dispose();
                Object.DestroyImmediate(rootObject);
            }

            Assert.AreEqual(GameServiceState.Disposed, service.State);
            Assert.AreEqual(1, service.DisposeCount);
        }

        [Test]
        public void Host_OrdersRequiredDependenciesBeforeDependentServices()
        {
            GameObject rootObject = new GameObject("Game Service Root");
            GameServiceRoot root = rootObject.AddComponent<GameServiceRoot>();
            List<string> calls = new List<string>();
            OrderedDependencyService dependency = new OrderedDependencyService(calls);
            OrderedDependentService dependent = new OrderedDependentService(calls);
            GameServiceHost host = new GameServiceHost(root, "Test", new GameService[] { dependent, dependency });

            try
            {
                host.Attach();

                Assert.AreEqual(2, calls.Count);
                Assert.AreEqual("Dependency", calls[0]);
                Assert.AreEqual("Dependent", calls[1]);
            }
            finally
            {
                host.Dispose();
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Host_RegistersExplicitContracts()
        {
            GameObject rootObject = new GameObject("Game Service Root");
            GameServiceRoot root = rootObject.AddComponent<GameServiceRoot>();
            ContractService service = new ContractService();
            GameServiceHost host = new GameServiceHost(root, "Test", new GameService[] { service });

            try
            {
                host.Attach();

                Assert.AreSame(service, host.Get<ITestContract>());
            }
            finally
            {
                host.Dispose();
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Host_ThrowsWhenRequiredDependencyIsMissing()
        {
            GameObject rootObject = new GameObject("Game Service Root");
            GameServiceRoot root = rootObject.AddComponent<GameServiceRoot>();
            MissingDependencyService service = new MissingDependencyService();
            GameServiceHost host = new GameServiceHost(root, "Test", new GameService[] { service });

            try
            {
                Assert.Throws<GameServiceException>(() => host.Attach());
            }
            finally
            {
                host.Dispose();
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Host_ThrowsWhenTwoServicesRegisterTheSameContract()
        {
            GameObject rootObject = new GameObject("Game Service Root");
            GameServiceRoot root = rootObject.AddComponent<GameServiceRoot>();
            GameServiceHost host = new GameServiceHost(
                root,
                "Test",
                new GameService[] { new ContractService(), new AlternateContractService() });

            try
            {
                Assert.Throws<GameServiceException>(() => host.Attach());
            }
            finally
            {
                host.Dispose();
                Object.DestroyImmediate(rootObject);
            }
        }

        private sealed class RecordingService : GameService
        {
            public override GameServiceTickMode TickMode =>
                GameServiceTickMode.Update | GameServiceTickMode.FixedUpdate | GameServiceTickMode.LateUpdate;

            public int AttachCount { get; private set; }
            public int StartCount { get; private set; }
            public int UpdateCount { get; private set; }
            public int FixedUpdateCount { get; private set; }
            public int LateUpdateCount { get; private set; }
            public int DisposeCount { get; private set; }
            public float LastUpdateDelta { get; private set; }

            protected override void OnAttach()
            {
                AttachCount++;
            }

            protected override void OnStart()
            {
                StartCount++;
            }

            protected override void OnUpdate(float deltaTime)
            {
                UpdateCount++;
                LastUpdateDelta = deltaTime;
            }

            protected override void OnFixedUpdate(float fixedDeltaTime)
            {
                FixedUpdateCount++;
            }

            protected override void OnLateUpdate(float deltaTime)
            {
                LateUpdateCount++;
            }

            protected override void OnDispose()
            {
                DisposeCount++;
            }
        }

        private sealed class OrderedDependencyService : GameService
        {
            private readonly List<string> calls;

            public OrderedDependencyService(List<string> calls)
            {
                this.calls = calls;
            }

            public override int Order => 100;

            protected override void OnAttach()
            {
                calls.Add("Dependency");
            }
        }

        private sealed class OrderedDependentService : GameService
        {
            private readonly List<string> calls;

            public OrderedDependentService(List<string> calls)
            {
                this.calls = calls;
            }

            public override int Order => -100;

            protected override void DeclareDependencies(GameServiceDependencyBuilder dependencies)
            {
                dependencies.Require<OrderedDependencyService>();
            }

            protected override void OnAttach()
            {
                calls.Add("Dependent");
            }
        }

        private interface ITestContract
        {
        }

        private interface IMissingContract
        {
        }

        private sealed class ContractService : GameService, ITestContract
        {
            protected override void RegisterContracts(GameServiceRegistry registry)
            {
                registry.Register<ITestContract>(this);
            }
        }

        private sealed class AlternateContractService : GameService, ITestContract
        {
            protected override void RegisterContracts(GameServiceRegistry registry)
            {
                registry.Register<ITestContract>(this);
            }
        }

        private sealed class MissingDependencyService : GameService
        {
            protected override void DeclareDependencies(GameServiceDependencyBuilder dependencies)
            {
                dependencies.Require<IMissingContract>();
            }
        }
    }
}
