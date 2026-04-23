using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Chase Move Preset", menuName = "SO/Enemies/Movement/Chase Move Preset", order = 10)]
public sealed class ChaseMovePresetSO : MovePresetSO
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float stopDistance = 0.8f;

    public override Type GetComponentType()
    {
        return typeof(ChaseMove);
    }

    public override void ApplyTo(MoveBase move, EnemySO enemy)
    {
        if (move is not ChaseMove chaseMove)
        {
            throw new InvalidOperationException($"{nameof(ChaseMovePresetSO)} requires {nameof(ChaseMove)}.");
        }

        float resolvedMoveSpeed = Mathf.Max(0f, moveSpeed > 0f ? moveSpeed : enemy != null ? enemy.BaseMoveSpeed : 0f);
        chaseMove.SetMoveSpeed(resolvedMoveSpeed);
        chaseMove.SetStopDistance(stopDistance);
    }
}
