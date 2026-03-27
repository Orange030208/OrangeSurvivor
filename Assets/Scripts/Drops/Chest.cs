using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chest : Collector
{
    public static Action OnCollect;

    public override bool CanCollect(IEntity source)
    {
        return Vector2.Distance(transform.position, source.Center) <= _collectRadius;
    }

    public override void StartCollect(IEntity target)
    {
        Debug.Log($"Starting collect for {target}");
        OnCollect?.Invoke();
        Destroy(gameObject);
    }
}
