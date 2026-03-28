using System;
using UnityEngine;

public class Candy : Collector
{
    public static event Action<Candy> onCollected;

    protected override void OnCollected()
    {
        onCollected?.Invoke(this);
    }
}
