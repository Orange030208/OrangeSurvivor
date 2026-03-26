using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector2 minXY;
    [SerializeField] private Vector2 maxXY;

    void LateUpdate()
    {
        Vector3 targetPosition = target.position;
        targetPosition.z = -10;
        targetPosition.x = Mathf.Clamp(targetPosition.x, minXY.x, maxXY.x);
        targetPosition.y = Mathf.Clamp(targetPosition.y, minXY.y, maxXY.y);
        transform.position = targetPosition;
    }
}
