using System.Collections;
using UnityEngine;

public abstract class Collection : Entity
{
    [SerializeField] protected float contactRadius = 0.8f;
    [SerializeField] private float initialCollectSpeed = 2f;
    [SerializeField] private float maxCollectSpeed = 10f;
    [SerializeField] private float collectAcceleration = 20f;

    protected Coroutine _collectRoutine;

    public virtual void TryCollect(IEntity target)
    {
        if (target == null || _collectRoutine != null) return;

        _collectRoutine = StartCoroutine(MoveTowardsPlayer(target));
    }

    protected IEnumerator MoveTowardsPlayer(IEntity target)
    {
        float currentSpeed = initialCollectSpeed;

        while (target != null)
        {
            if (!GameSimulation.IsRunning)
            {
                yield return null;
                continue;
            }

            Vector2 currentPosition = transform.position;
            Vector2 targetPosition = target.Center;
            float distance = Vector2.Distance(currentPosition, targetPosition);

            if (distance <= contactRadius)
            {
                Collect(target);
                yield break;
            }

            currentSpeed = Mathf.MoveTowards(currentSpeed, maxCollectSpeed, collectAcceleration * Time.deltaTime);
            float step = currentSpeed * Time.deltaTime;
            transform.position = Vector2.MoveTowards(currentPosition, targetPosition, step);
            yield return null;
        }

        _collectRoutine = null;
    }

    protected void Collect(IEntity entity)
    {
        _collectRoutine = null;
        OnCollected(entity);
        Destroy(gameObject);
    }

    /// <summary>
    /// 写收集的逻辑
    /// </summary>
    /// <param name="entity">收集自己的实体</param>
    protected abstract void OnCollected(IEntity entity);
}
