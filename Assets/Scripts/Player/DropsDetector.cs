using System;
using UnityEngine;

[RequireComponent(typeof(IEntity))]
public class DropsDetector : MonoBehaviour
{
    [SerializeField] private float detectRadius = 5;
    [SerializeField] private float timeToDetect = 0.1f;
    private int collectLayer;
    private float detectTimer;
    private IEntity _entity;

    private void Start()
    {
        detectTimer = timeToDetect;
        collectLayer =  LayerMask.NameToLayer("Collector");
        _entity =  GetComponent<IEntity>();
    }

    private void Update()
    {
        detectTimer -= Time.deltaTime;
        if (detectTimer <= 0)
        {
            Detect();
            detectTimer =  timeToDetect;
        }
    }

    private void Detect()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, detectRadius,1 << collectLayer);
        foreach (var collider in colliders)
        {
            if (collider.TryGetComponent(out Collector collector))
            {
                if (collector.CanCollect(_entity))
                {
                    collector.StartCollect(_entity);
                }
            }
        }
    }
}