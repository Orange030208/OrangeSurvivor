using System;
using UnityEngine;

public class Chest : Collection
{
    // public override void TryCollect(IEntity target)
    // {
    //     if (target == null) return;
    //     if (target.Distance(this) > contactRadius) return;
    //
    //     Collect(target);
    // }

    protected override void OnCollected(IEntity entity)
    {
        WaveTransitionManager.Instance.CollectChest();
    }
}
