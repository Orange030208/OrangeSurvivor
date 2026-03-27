using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

public class EnemyMovement : MonoBehaviour
{
    private Player _target;
    [SerializeField] private float moveSpeed;

    public void FollowPlayer()
    {
        Vector2 direction = (_target.transform.position - transform.position).normalized;
        Vector2 targetPos = (Vector2)transform.position + direction * moveSpeed * Time.deltaTime;
        transform.position = targetPos;
    }

    public void SetTarget(Player target)
    {
        _target = target;
    }
}