using System.Collections;
using UnityEngine;

public abstract class AbsorbableCollection : Collection
{
    [SerializeField] private float initialCollectSpeed = 2f;
    [SerializeField] private float maxCollectSpeed = 10f;
    [SerializeField] private float collectAcceleration = 20f;

    public override void TryCollect(IEntity target)
    {
        if (target == null || isCollecting)
        {
            return;
        }

        isCollecting = true;
        StartCoroutine(MoveTowardsPlayer(target));
    }

    private IEnumerator MoveTowardsPlayer(IEntity target)
    {
        float currentSpeed = initialCollectSpeed;

        while (target != null)
        {
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

        isCollecting = false;
    }
}
