using UnityEngine;

public class Movement : MonoBehaviour
{
    private const float MIN_STOP_DISTANCE = 0f;
    private const float MIN_MOVE_SPEED = 0f;

    [SerializeField] private float moveSpeed = 2f;

    public float MoveSpeed => moveSpeed;

    public void FollowPlayer(Player target, float deltaTime, float stopDistance = 0f)
    {
        if (target == null)
        {
            return;
        }

        Vector2 currentPosition = transform.position;
        Vector2 targetPosition = target.transform.position;
        Vector2 offset = targetPosition - currentPosition;
        float distance = offset.magnitude;
        float clampedStopDistance = Mathf.Max(MIN_STOP_DISTANCE, stopDistance);

        if (distance <= clampedStopDistance || distance <= Mathf.Epsilon)
        {
            return;
        }

        Vector2 direction = offset / distance;
        float moveDistance = Mathf.Min(moveSpeed * deltaTime, distance - clampedStopDistance);
        transform.position = currentPosition + direction * moveDistance;
    }

    public void SetMoveSpeed(float value)
    {
        moveSpeed = Mathf.Max(MIN_MOVE_SPEED, value);
    }
}
