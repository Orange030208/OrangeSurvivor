using System;
using UnityEngine;

public class Candy : Collection
{
    protected override void OnCollected(IEntity entity)
    {
        var levelComponent = entity.Transform.GetComponent<PlayerLevel>();
        levelComponent.AddXP(1);
    }
}
