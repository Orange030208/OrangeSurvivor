using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Knockback Receiver Config", menuName = ScriptableObjectMenuPaths.KNOCKBACK_RECEIVER_CONFIG, order = 0)]
public class KnockbackReceiverConfigSO : ScriptableObject
{
    [Header("Runtime")]
    [SerializeField] private float duration = 0.12f;
    [Tooltip("Distance scalar for received knockback. With 1, every 10 KnockbackStrength is roughly 1 world unit before resistance, collision, and max velocity limits.")]
    [FormerlySerializedAs("forceMultiplier")]
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
