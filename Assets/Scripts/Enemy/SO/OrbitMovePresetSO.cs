using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Orbit Move Preset", menuName = "SO/Enemies/Movement/Orbit Move Preset", order = 12)]
public sealed class OrbitMovePresetSO : MovePresetSO
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float orbitRadius = 3.5f;
    [SerializeField] private float radiusTolerance = 0.35f;
    [SerializeField] private bool clockwise = true;

    public override Type GetComponentType()
    {
        return typeof(OrbitMove);
    }

    public override void ApplyTo(MoveBase move, EnemySO enemy)
    {
        if (move is not OrbitMove orbitMove)
        {
            throw new InvalidOperationException($"{nameof(OrbitMovePresetSO)} requires {nameof(OrbitMove)}.");
        }

        float resolvedMoveSpeed = Mathf.Max(0f, moveSpeed > 0f ? moveSpeed : enemy != null ? enemy.BaseMoveSpeed : 0f);
        orbitMove.SetMoveSpeed(resolvedMoveSpeed);
        orbitMove.SetOrbitRadius(orbitRadius);
        orbitMove.SetRadiusTolerance(radiusTolerance);
        orbitMove.SetClockwise(clockwise);
    }
}
