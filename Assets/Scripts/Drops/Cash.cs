using UnityEngine;

public class Cash : Collection
{
    protected override void OnCollected(IEntity entity)
    {
        print("收集了钱");
    }
}