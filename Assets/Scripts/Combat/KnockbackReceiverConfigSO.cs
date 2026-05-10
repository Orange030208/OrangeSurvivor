using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Knockback Receiver Config", menuName = ScriptableObjectMenuPaths.KNOCKBACK_RECEIVER_CONFIG, order = 0)]
public class KnockbackReceiverConfigSO : ScriptableObject
{
    [Header("运行时")]
    [SerializeField] private float duration = 0.12f;
    [Tooltip("受到击退时的距离倍率。值为 1 时，每 10 点击退强度在抗性、碰撞和最大速度限制前约等于 1 个世界单位。")]
    [SerializeField] private float distanceMultiplier = 1f;
    [SerializeField] private float maxVelocity = 12f;
    [SerializeField] private AnimationCurve velocityCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    public float Duration => Mathf.Max(0.01f, duration);
    public float DistanceMultiplier => Mathf.Max(0f, distanceMultiplier);
    public float MaxVelocity => Mathf.Max(0.01f, maxVelocity);
    public AnimationCurve VelocityCurve => velocityCurve;

    private void OnValidate()
    {
        duration = Mathf.Max(0.01f, duration);
        distanceMultiplier = Mathf.Max(0f, distanceMultiplier);
        maxVelocity = Mathf.Max(0.01f, maxVelocity);

        if (velocityCurve == null || velocityCurve.length == 0)
        {
            velocityCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        }
    }
}
