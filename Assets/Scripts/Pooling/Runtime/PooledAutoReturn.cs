using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PooledAutoReturn : MonoBehaviour, IPoolable
{
    private const float MinimumDelay = 0.01f;

    [SerializeField] private bool autoReturnOnRent;
    [Min(MinimumDelay)] [SerializeField] private float defaultDelay = 1f;

    private Coroutine returnRoutine;
    private PooledObjectHandle handle;

    private void Awake()
    {
        handle = GetComponent<PooledObjectHandle>();
    }

    public void ScheduleReturn(float delay)
    {
        CancelScheduledReturn();
        returnRoutine = StartCoroutine(ReturnAfterDelay(Mathf.Max(MinimumDelay, delay)));
    }

    public void ReturnNow()
    {
        CancelScheduledReturn();
        EnsureHandle();
        if (handle == null || !handle.ReturnToPool())
        {
            Destroy(gameObject);
        }
    }

    public void OnRentFromPool()
    {
        if (autoReturnOnRent)
        {
            ScheduleReturn(defaultDelay);
        }
    }

    public void OnReturnToPool()
    {
        CancelScheduledReturn();
    }

    public void OnDiscardFromPool()
    {
        CancelScheduledReturn();
    }

    private IEnumerator ReturnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        returnRoutine = null;
        ReturnNow();
    }

    private void CancelScheduledReturn()
    {
        if (returnRoutine == null)
        {
            return;
        }

        StopCoroutine(returnRoutine);
        returnRoutine = null;
    }

    private void EnsureHandle()
    {
        if (handle == null)
        {
            handle = GetComponent<PooledObjectHandle>();
        }
    }
}
