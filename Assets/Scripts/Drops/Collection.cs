using System.Collections;
using UnityEngine;

public abstract class Collection : Entity
{
    [SerializeField] protected float contactRadius = 0.8f;
    protected Coroutine _collectRoutine;

    public virtual void TryCollect(IEntity target)
    {
        if (target == null || _collectRoutine != null) return;

        _collectRoutine = StartCoroutine(MoveTowardsPlayer(target));
    }

    protected IEnumerator MoveTowardsPlayer(IEntity target)
    {
        float timer = 0;
        Vector2 initPosition = transform.position;

        while (timer < 1)
        {
            if (target == null)
            {
                break;
            }

            if (target.Distance(this) < contactRadius)
            {
                break;
            }
            transform.position = Vector2.Lerp(initPosition, target.Center, timer);
            timer += Time.deltaTime;
            yield return null;
        }

        Collect(target);
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
