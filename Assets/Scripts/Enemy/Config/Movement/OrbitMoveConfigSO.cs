using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Orbit Move Config", menuName = "SO/Enemies/Movement/Orbit Move Config", order = 12)]
public sealed class OrbitMoveConfigSO : MoveConfigSO
{
    [SerializeField] [Min(0f)] private float orbitRadius = 3f;
    [SerializeField] [Min(0f)] private float radiusTolerance = 0.35f;
    [SerializeField] private bool clockwise = true;

    public float OrbitRadius => Mathf.Max(0f, orbitRadius);
    public float RadiusTolerance => Mathf.Max(0f, radiusTolerance);
    public bool Clockwise => clockwise;

    public override Type GetComponentType()
    {
        return typeof(OrbitMove);
    }

    public override void ApplyTo(MoveBase move, float moveSpeed, float attackDetectionRadius)
    {
        if (move is not OrbitMove orbitMove)
        {
            throw new InvalidOperationException($"{nameof(OrbitMoveConfigSO)} requires {nameof(OrbitMove)}.");
        }

        orbitMove.SetMoveSpeed(moveSpeed);
        orbitMove.ApplyConfig(this, attackDetectionRadius);
    }
}
