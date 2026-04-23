using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Keep Distance Move Preset", menuName = "SO/Enemies/Movement/Keep Distance Move Preset", order = 11)]
public sealed class KeepDistanceMovePresetSO : MovePresetSO
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float desiredDistance = 5f;
    [SerializeField] private float tolerance = 0.5f;

    public override Type GetComponentType()
    {
        return typeof(KeepDistanceMove);
    }

    public override void ApplyTo(MoveBase move, EnemySO enemy)
    {
        if (move is not KeepDistanceMove keepDistanceMove)
        {
            throw new InvalidOperationException($"{nameof(KeepDistanceMovePresetSO)} requires {nameof(KeepDistanceMove)}.");
        }

        float resolvedMoveSpeed = Mathf.Max(0f, moveSpeed > 0f ? moveSpeed : enemy != null ? enemy.BaseMoveSpeed : 0f);
        keepDistanceMove.SetMoveSpeed(resolvedMoveSpeed);
        keepDistanceMove.SetDesiredDistance(desiredDistance);
        keepDistanceMove.SetTolerance(tolerance);
    }
}
