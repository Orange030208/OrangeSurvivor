using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Keep Distance Move Config", menuName = "SO/Enemies/Movement/Keep Distance Move Config", order = 11)]
public sealed class KeepDistanceMoveConfigSO : MoveConfigSO
{
    [SerializeField] [Min(0f)] private float desiredDistance = 4f;
    [SerializeField] [Min(0f)] private float tolerance = 0.5f;

    public float DesiredDistance => Mathf.Max(0f, desiredDistance);
    public float Tolerance => Mathf.Max(0f, tolerance);

    public override Type GetComponentType()
    {
        return typeof(KeepDistanceMove);
    }

    public override void ApplyTo(MoveBase move, float moveSpeed, float attackDetectionRadius)
    {
        if (move is not KeepDistanceMove keepDistanceMove)
        {
            throw new InvalidOperationException($"{nameof(KeepDistanceMoveConfigSO)} requires {nameof(KeepDistanceMove)}.");
        }

        keepDistanceMove.SetMoveSpeed(moveSpeed);
        keepDistanceMove.ApplyConfig(this, attackDetectionRadius);
    }
}
