using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector2 minXY;
    [SerializeField] private Vector2 maxXY;
    [SerializeField] private bool useRuntimeMapBounds = true;

    private void OnEnable()
    {
        GameEventBus.Subscribe<PlayerSpawnedEvent>(OnPlayerSpawned);

        if (target == null)
        {
            Player player = FindFirstObjectByType<Player>();
            if (player != null)
            {
                target = player.transform;
            }
        }
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<PlayerSpawnedEvent>(OnPlayerSpawned);
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector2 minBounds = minXY;
        Vector2 maxBounds = maxXY;
        if (useRuntimeMapBounds && MapGenerator.TryGetRuntimeBounds(out Bounds runtimeBounds))
        {
            Vector3 extents = runtimeBounds.extents;
            minBounds = new Vector2(runtimeBounds.center.x - extents.x, runtimeBounds.center.y - extents.y);
            maxBounds = new Vector2(runtimeBounds.center.x + extents.x, runtimeBounds.center.y + extents.y);
        }

        Vector3 targetPosition = target.position;
        targetPosition.z = -10;
        targetPosition.x = Mathf.Clamp(targetPosition.x, minBounds.x, maxBounds.x);
        targetPosition.y = Mathf.Clamp(targetPosition.y, minBounds.y, maxBounds.y);
        transform.position = targetPosition;
    }

    private void OnPlayerSpawned(PlayerSpawnedEvent eventData)
    {
        target = eventData.Player != null ? eventData.Player.transform : null;
    }
}
