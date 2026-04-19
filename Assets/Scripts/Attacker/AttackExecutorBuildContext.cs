using UnityEngine;

public readonly struct AttackExecutorBuildContext
{
    public Entity Owner { get; }
    public Transform AttackOrigin { get; }
    public AttackDefinitionSO AttackDefinition { get; }

    public AttackExecutorBuildContext(Entity owner, Transform attackOrigin, AttackDefinitionSO attackDefinition)
    {
        Owner = owner;
        AttackOrigin = attackOrigin;
        AttackDefinition = attackDefinition;
    }
}
