using UnityEngine;

public abstract class MoveBase : MonoBehaviour, IMovement
{
    private const float MIN_MOVE_SPEED = 0f;

    [SerializeField] private float moveSpeed = 2f;

    private bool movementDisabled;
    private Vector2 moveDirection;

    public float MoveSpeed => moveSpeed;
    public float Speed => moveSpeed;
    public Vector2 MoveDirection => moveDirection;
    public bool IsMoving => !movementDisabled && moveDirection.sqrMagnitude > Mathf.Epsilon;

    public void EnableMovement()
    {
        movementDisabled = false;
    }

    public void DisableMovement()
    {
        movementDisabled = true;
        moveDirection = Vector2.zero;
        OnStopped();
    }

    public void StopImmediately()
    {
        moveDirection = Vector2.zero;
        OnStopped();
    }

    public void SetMoveSpeed(float value)
    {
        moveSpeed = Mathf.Max(MIN_MOVE_SPEED, value);
    }

    protected bool CanMove => !movementDisabled;
    protected float CurrentMoveSpeed => moveSpeed;

    protected void ApplyMoveDirection(Vector2 direction)
    {
        moveDirection = direction;
    }

    protected void ClearMoveDirection()
    {
        moveDirection = Vector2.zero;
    }

    protected virtual void OnStopped()
    {
    }

    public abstract void Tick(Entity owner, Entity target, float deltaTime, float desiredDistance);
}
