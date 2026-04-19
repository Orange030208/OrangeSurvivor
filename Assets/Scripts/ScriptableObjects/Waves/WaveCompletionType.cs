using UnityEngine;

/// <summary>
/// 波次完成条件。
/// 决定当前波是按计时结束、清空敌人结束，还是按 Boss 死亡结束。
/// </summary>
public enum WaveCompletionType
{
    DurationElapsed = 0,
    ClearAllEnemies = 1,
    BossDefeated = 2
}
