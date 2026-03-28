using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Collector : MonoBehaviour
{
    public enum CollectorStatus
    {
        Static,
        MovingToTarget
    }

    public CollectorStatus Status { get; protected set; } = CollectorStatus.Static;

    private IEntity _target;

    protected float _collectRadius = 0.8f;

    public virtual void StartCollect(IEntity target)
    {
        Status = CollectorStatus.MovingToTarget;
        StartCoroutine(MoveTowardsPlayer(target));
    }

    public virtual bool CanCollect(IEntity source)
    {
        return Status == CollectorStatus.Static;
    }

    protected IEnumerator MoveTowardsPlayer(IEntity target)
    {
        float timer = 0;
        Vector2 initPosition = transform.position;
        while (timer < 1)
        {
            if (Vector2.Distance(target.Center, transform.position) <= _collectRadius)
            {
                break;
            }
            transform.position = Vector2.Lerp(initPosition, target.Center, timer);
            timer += Time.deltaTime;
            yield return null;
        }

        Collected();
    }

    private void Collected()
    {
        Debug.Log("Collected");
        OnCollected();
        Destroy(gameObject);
    }

    protected virtual void OnCollected()
    {
        
    }
}