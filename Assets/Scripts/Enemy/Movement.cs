using UnityEngine;

public class Movement : MonoBehaviour, IMovement
{
    private const float MIN_STOP_DISTANCE = 0f;
    private const float MIN_MOVE_SPEED = 0f;

    [SerializeField] private float moveSpeed = 2f;

    private bool movementDisabled;
    private Vector2 moveDirection;

    public float MoveSpeed => moveSpeed;
    public float Speed => moveSpeed;
    public Vector2 MoveDirection => moveDirection;
    public bool IsMoving => !movementDisabled && moveDirection.sqrMagnitude > Mathf.Epsilon;

    public void FollowTarget(Entity target, float deltaTime, float stopDistance = 0f)
    {
        moveDirection = Vector2.zero;

        if (movementDisabled || target == null)
        {
            return;
        }

        Vector2 currentPosition = transform.position;
        Vector2 targetPosition = target.Transform.position;
        Vector2 offset = targetPosition - currentPosition;
        float distance = offset.magnitude;
        float clampedStopDistance = Mathf.Max(MIN_STOP_DISTANCE, stopDistance);

        if (distance <= clampedStopDistance || distance <= Mathf.Epsilon)
        {
            return;
        }

        Vector2 direction = offset / distance;
        float moveDistance = Mathf.Min(moveSpeed * deltaTime, distance - clampedStopDistance);
        moveDirection = direction;
        transform.position = currentPosition + direction * moveDistance;
    }

    public void MoveTowardsPosition(Vector2 targetPosition, float deltaTime, float stopDistance = 0f)
    {
        moveDirection = Vector2.zero;

        if (movementDisabled)
        {
            return;
        }

        Vector2 currentPosition = transform.position;
        Vector2 offset = targetPosition - currentPosition;
        float distance = offset.magnitude;
        float clampedStopDistance = Mathf.Max(MIN_STOP_DISTANCE, stopDistance);

        if (distance <= clampedStopDistance || distance <= Mathf.Epsilon)
        {
            return;
        }

        Vector2 direction = offset / distance;
        float moveDistance = Mathf.Min(moveSpeed * deltaTime, distance - clampedStopDistance);
        moveDirection = direction;
        transform.position = currentPosition + direction * moveDistance;
    }

    public void MoveInDirection(Vector2 direction, float deltaTime)
    {
        moveDirection = Vector2.zero;

        if (movementDisabled)
        {
            return;
        }

        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        Vector2 normalizedDirection = direction.normalized;
        moveDirection = normalizedDirection;
        transform.position += (Vector3)(normalizedDirection * moveSpeed * deltaTime);
    }

    public void Stop()
    {
        moveDirection = Vector2.zero;
    }

    public void SetMoveSpeed(float value)
    {
        moveSpeed = Mathf.Max(MIN_MOVE_SPEED, value);
    }

    public void EnableMovement()
    {
        movementDisabled = false;
    }

    public void DisableMovement()
    {
        movementDisabled = true;
        moveDirection = Vector2.zero;
    }
}
