using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 单个对象参与波次结束流程的统一入口。
/// 相同优先级会在同一批次同时执行；需要严格先后关系时使用不同优先级。
/// </summary>
public interface IWaveEndStep
{
    int WaveEndPriority { get; }
    UniTask ExecuteWaveEndAsync(CancellationToken cancellationToken);
}

public static class WaveEndPriorities
{
    public const int PrepareRuntime = -1000;
    public const int StopCombat = -100;
    public const int EntityCleanup = 0;
}
