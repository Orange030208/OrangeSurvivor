using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// 特效生命周期控制器：
/// - FixedTime：实例化后按固定时长自动销毁；
/// - Manual：由动画事件或外部代码显式调用 Release。
/// </summary>
public sealed class VfxLifetime : MonoBehaviour
{
    public enum Mode
    {
        FixedTime = 0,
        Manual = 1
    }

    private const float MinimumLifetime = 0.05f;

    [SerializeField] private Mode mode = Mode.FixedTime;
    [SerializeField] private float fixedLifetime = 1f;

    private bool isScheduled;

    public Mode LifetimeMode => mode;
    public float FixedLifetime => Mathf.Max(MinimumLifetime, fixedLifetime);

    public void Activate(float overrideLifetime = -1f)
    {
        if (isScheduled)
        {
            return;
        }

        if (overrideLifetime > 0f)
        {
            ScheduleDestroy(overrideLifetime);
            return;
        }

        if (mode == Mode.FixedTime)
        {
            ScheduleDestroy(fixedLifetime);
        }
    }

    public void Release()
    {
        if (this == null || gameObject == null)
        {
            return;
        }

        ScheduleDestroy(0f);
    }

    private void ScheduleDestroy(float delay)
    {
        if (isScheduled)
        {
            return;
        }

        isScheduled = true;
        Object.Destroy(gameObject, Mathf.Max(0f, delay));
    }
}
