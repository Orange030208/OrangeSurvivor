# Services 快速开始

`Orange.Services` 是一套面向 Unity 的服务生命周期框架。程序集和命名空间归属 Orange，但公共类型名不带 `Orange` 前缀。

```csharp
using Orange.Services;

public sealed class GameServiceHost : ServiceHost
{
    protected override void InstallServices(IServiceRegistry registry)
    {
        registry.Register<ITimeService>(_ => new TimeService()).Eager();
        registry.Register<IGameFlowService>(resolver =>
            new GameFlowService(resolver.Resolve<ITimeService>()))
            .DependsOn<ITimeService>();
    }
}
```

外部代码建议通过显式传入的 `IServiceResolver` 或 `IServiceScope` 解析服务：

```csharp
public sealed class SomeController
{
    private readonly IGameFlowService gameFlowService;

    public SomeController(IServiceResolver resolver)
    {
        gameFlowService = resolver.Resolve<IGameFlowService>();
    }
}
```

需要场景级、局内级或流程级服务时，可以从现有 scope 创建子作用域：

```csharp
IServiceScope sessionScope = rootScope.CreateChild(registry =>
{
    registry.Register<ISessionState>(_ => new SessionState()).Eager();
}, "Session");
```
