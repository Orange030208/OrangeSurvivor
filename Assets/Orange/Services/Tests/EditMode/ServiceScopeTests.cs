using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Orange.Services.Tests
{
    public class ServiceScopeTests
    {
        [Test]
        public void Resolve_ReturnsRegisteredServiceByContract()
        {
            using ServiceScope scope = new ServiceScope("ResolveTest");
            scope.Registry.Register<ITestService>(_ => new TestService());
            scope.Build();

            ITestService service = scope.Resolve<ITestService>();

            Assert.IsInstanceOf<TestService>(service);
        }

        [Test]
        public void Initialize_CreatesOnlyEagerServicesUntilLazyServiceIsResolved()
        {
            int createdCount = 0;
            using ServiceScope scope = new ServiceScope("EagerLazyTest");
            scope.Registry.Register<ITestService>(_ =>
            {
                createdCount++;
                return new TestService();
            }).Eager();
            scope.Registry.Register<ISecondService>(_ =>
            {
                createdCount++;
                return new SecondService();
            });
            scope.Build();

            scope.Initialize();

            Assert.AreEqual(1, createdCount);

            scope.Resolve<ISecondService>();

            Assert.AreEqual(2, createdCount);
        }

        [Test]
        public void Lifecycle_UsesDependencyOrderAndReverseShutdownOrder()
        {
            List<string> events = new List<string>();
            using ServiceScope scope = new ServiceScope("LifecycleOrderTest");
            scope.Registry.Register<ISecondService>(_ => new RecordingService("B", events)).Eager();
            scope.Registry.Register<ITestService>(_ => new RecordingService("A", events))
                .DependsOn<ISecondService>()
                .Eager();
            scope.Build();

            scope.Initialize();
            scope.Start();
            scope.Shutdown();

            CollectionAssert.AreEqual(
                new[]
                {
                    "B.Initialize",
                    "A.Initialize",
                    "B.Start",
                    "A.Start",
                    "A.Shutdown",
                    "B.Shutdown"
                },
                events);
        }

        [Test]
        public void ChildScope_FallsBackToParentAndCanShadowParentService()
        {
            TestService parentService = new TestService();
            TestService childService = new TestService();
            using ServiceScope root = new ServiceScope("Root");
            root.Registry.RegisterInstance<ITestService>(parentService);
            root.Build();

            IServiceScope fallbackChild = root.CreateChild(name: "FallbackChild");
            IServiceScope shadowChild = root.CreateChild(
                registry => registry.RegisterInstance<ITestService>(childService),
                "ShadowChild");

            Assert.AreSame(parentService, fallbackChild.Resolve<ITestService>());
            Assert.AreSame(childService, shadowChild.Resolve<ITestService>());
        }

        [Test]
        public void Registry_ThrowsWhenMutatedAfterBuild()
        {
            using ServiceScope scope = new ServiceScope("FreezeTest");
            scope.Build();

            Assert.Throws<ServiceException>(() =>
                scope.Registry.Register<ITestService>(_ => new TestService()));
        }

        [Test]
        public void Registry_ThrowsForDuplicateRegistration()
        {
            using ServiceScope scope = new ServiceScope("DuplicateTest");
            scope.Registry.Register<ITestService>(_ => new TestService());

            Assert.Throws<ServiceException>(() =>
                scope.Registry.Register<ITestService>(_ => new TestService()));
        }

        [Test]
        public void Build_ThrowsForMissingDependency()
        {
            using ServiceScope scope = new ServiceScope("MissingDependencyTest");
            scope.Registry.Register<ITestService>(_ => new TestService())
                .DependsOn<ISecondService>();

            ServiceException exception = Assert.Throws<ServiceException>(() => scope.Build());

            StringAssert.Contains(nameof(ISecondService), exception.Message);
        }

        [Test]
        public void Build_ThrowsForCyclicDependency()
        {
            using ServiceScope scope = new ServiceScope("CycleTest");
            scope.Registry.Register<ITestService>(_ => new TestService())
                .DependsOn<ISecondService>();
            scope.Registry.Register<ISecondService>(_ => new SecondService())
                .DependsOn<ITestService>();

            ServiceException exception = Assert.Throws<ServiceException>(() => scope.Build());

            StringAssert.Contains("Cyclic service dependency", exception.Message);
        }

        [Test]
        public void ResolveAfterStart_CatchesLazyServiceUpToCurrentLifecyclePhase()
        {
            List<string> events = new List<string>();
            using ServiceScope scope = new ServiceScope("LateResolveTest");
            scope.Registry.Register<ITestService>(_ => new RecordingService("Late", events));
            scope.Build();
            scope.Initialize();
            scope.Start();

            scope.Resolve<ITestService>();

            CollectionAssert.AreEqual(
                new[]
                {
                    "Late.Initialize",
                    "Late.Start"
                },
                events);
        }

        [Test]
        public void LifecycleException_IncludesPhaseContractServiceScopeAndChain()
        {
            using ServiceScope scope = new ServiceScope("DiagnosticTest");
            scope.Registry.Register<ITestService>(_ => new ThrowingInitializeService()).Eager();
            scope.Build();

            ServiceException exception = Assert.Throws<ServiceException>(() => scope.Initialize());

            Assert.AreEqual(ServiceLifecyclePhase.Initialize, exception.Phase);
            Assert.AreEqual(typeof(ITestService), exception.ContractType);
            Assert.AreEqual(typeof(ThrowingInitializeService), exception.ServiceType);
            Assert.AreEqual("DiagnosticTest", exception.ScopeName);
            StringAssert.Contains("Dependency chain", exception.Message);
        }

        private interface ITestService
        {
        }

        private interface ISecondService
        {
        }

        private sealed class TestService : ITestService
        {
        }

        private sealed class SecondService : ISecondService
        {
        }

        private sealed class RecordingService :
            ITestService,
            ISecondService,
            IServiceInitializable,
            IServiceStartable,
            IServiceShutdown
        {
            private readonly string name;
            private readonly List<string> events;

            public RecordingService(string name, List<string> events)
            {
                this.name = name;
                this.events = events;
            }

            public void Initialize(ServiceLifecycleContext context)
            {
                events.Add(name + ".Initialize");
            }

            public void Start(ServiceLifecycleContext context)
            {
                events.Add(name + ".Start");
            }

            public void Shutdown(ServiceLifecycleContext context)
            {
                events.Add(name + ".Shutdown");
            }
        }

        private sealed class ThrowingInitializeService : ITestService, IServiceInitializable
        {
            public void Initialize(ServiceLifecycleContext context)
            {
                throw new InvalidOperationException("Expected test failure.");
            }
        }
    }
}
