using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Chase Move Config", menuName = "SO/Enemies/Movement/Chase Move Config", order = 10)]
public sealed class ChaseMoveConfigSO : MoveConfigSO
{
    [SerializeField] [Min(0f)] private float stopDistance = 0f;

    public float StopDistance => Mathf.Max(0f, stopDistance);

    public override Type GetComponentType()
    {
        return typeof(ChaseMove);
    }

    public override void ApplyTo(MoveBase move, float moveSpeed, float attackDetectionRadius)
    {
        if (move is not ChaseMove chaseMove)
        {
            throw new InvalidOperationException($"{nameof(ChaseMoveConfigSO)} requires {nameof(ChaseMove)}.");
        }

        chaseMove.SetMoveSpeed(moveSpeed);
        chaseMove.ApplyConfig(this, attackDetectionRadius);
    }
}
