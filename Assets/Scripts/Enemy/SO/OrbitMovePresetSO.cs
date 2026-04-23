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
        throw new NotImplementedException();
    }

    public override void ApplyTo(MoveBase move, EnemySO enemy)
    {
        throw new NotImplementedException();
    }
}