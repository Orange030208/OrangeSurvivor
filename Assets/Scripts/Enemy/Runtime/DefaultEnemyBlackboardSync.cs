public sealed class DefaultEnemyBlackboardSync : EnemyBlackboardSyncBase
{
    public override void RefreshAiFacts(float deltaTime)
    {
        WriteCommonCombatFacts();
    }
}
