/// <summary>
/// 波次完成规则。默认计时完成，BossDefeated 用于需要击杀 Boss 才结算的阶段波。
/// </summary>
public enum WaveCompletionMode
{
    TimerOnly = 0,
    BossDefeated = 1
}
