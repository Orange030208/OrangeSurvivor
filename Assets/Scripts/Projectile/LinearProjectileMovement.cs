using UnityEngine;

public sealed class LinearProjectileMovement : ProjectileMovementBehaviour
{
    [Header("直线运动")]
    [Tooltip("弹体基础移动速度，会再乘以 ProjectileDefinitionSO.SpeedMultiplier。")]
    [SerializeField, Min(0f)] private float baseMoveSpeed = 10f;

    private float currentMoveSpeed;

    public float CurrentMoveSpeed => currentMoveSpeed;

    public override void Launch()
    {
        currentMoveSpeed = baseMoveSpeed * ResolveSpeedMultiplier();

        Rigidbody2D runtimeRigidbody = RuntimeContext.Rigidbody;
        if (runtimeRigidbody == null)
        {
            return;
        }

        runtimeRigidbody.simulated = true;
        runtimeRigidbody.velocity = RuntimeContext.LaunchContext.Direction * currentMoveSpeed;
    }

    public override void Stop()
    {
        Rigidbody2D runtimeRigidbody = RuntimeContext.Rigidbody;
        if (runtimeRigidbody == null)
        {
            return;
        }

        runtimeRigidbody.velocity = Vector2.zero;
        runtimeRigidbody.angularVelocity = 0f;
        runtimeRigidbody.simulated = false;
    }

    private float ResolveSpeedMultiplier()
    {
        ProjectileDefinitionSO definition = RuntimeContext.Definition;
        return definition != null ? definition.SpeedMultiplier : 1f;
    }

    private void OnValidate()
    {
        baseMoveSpeed = Mathf.Max(0f, baseMoveSpeed);
    }
}
