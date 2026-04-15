using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector2 minXY;
    [SerializeField] private Vector2 maxXY;

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

        Vector3 targetPosition = target.position;
        targetPosition.z = -10;
        targetPosition.x = Mathf.Clamp(targetPosition.x, minXY.x, maxXY.x);
        targetPosition.y = Mathf.Clamp(targetPosition.y, minXY.y, maxXY.y);
        transform.position = targetPosition;
    }

    private void OnPlayerSpawned(PlayerSpawnedEvent eventData)
    {
        target = eventData.Player != null ? eventData.Player.transform : null;
    }
}
