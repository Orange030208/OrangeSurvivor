public readonly struct EnemyRuntimeSetup
{
    public EnemyDefinitionSO Definition { get; }
    public Player Player { get; }

    public EnemyRuntimeSetup(EnemyDefinitionSO definition, Player player)
    {
        Definition = definition;
        Player = player;
    }
}
