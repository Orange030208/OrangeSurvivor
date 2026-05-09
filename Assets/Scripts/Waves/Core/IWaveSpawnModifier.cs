using System.Collections.Generic;

/// <summary>
/// 波次结构 Modifier 只处理节奏、最终生成请求和额外生成请求；敌人候选权重统一交给 ContentPool Modifier。
/// </summary>
public interface IWaveSpawnModifier
{
    int Priority { get; }
    void OnWaveStarted(WaveSpawnContext context);
    void OnWaveEnded(WaveSpawnContext context);
    void ModifySchedule(WaveSpawnModifierContext context, WaveSpawnSchedule schedule);
    void ModifySpawnRequest(WaveSpawnModifierContext context, WaveSpawnRequest request);
    void AppendSpawnRequests(WaveSpawnModifierContext context, List<WaveSpawnRequest> requests);
}
