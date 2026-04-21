using UnityEngine;

public abstract class EnemyMovementDefinitionSO : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string movementId = "EnemyMovement_Default";

    public string MovementId => movementId;

    public abstract EnemyMovementType MovementType { get; }
    public abstract IEnemyMovementStrategy CreateRuntimeStrategy();
}
