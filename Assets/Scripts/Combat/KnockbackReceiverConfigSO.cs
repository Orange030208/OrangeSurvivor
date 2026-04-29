using UnityEngine;

[CreateAssetMenu(fileName = "Knockback Receiver Config", menuName = ScriptableObjectMenuPaths.KNOCKBACK_RECEIVER_CONFIG, order = 0)]
public class KnockbackReceiverConfigSO : ScriptableObject
{
    [Header("Runtime")]
    [SerializeField] private float duration = 0.12f;
    [SerializeField] private float forceMultiplier = 1f;
    [SerializeField] private float maxVelocity = 12f;
    [SerializeField] private bool disableMovementWhileKnockback = true;
    [SerializeField] private AnimationCurve velocityCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    public float Duration => Mathf.Max(0.01f, duration);
    public float ForceMultiplier => Mathf.Max(0f, forceMultiplier);
    public float MaxVelocity => Mathf.Max(0.01f, maxVelocity);
    public bool DisableMovementWhileKnockback => disableMovementWhileKnockback;
    public AnimationCurve VelocityCurve => velocityCurve;

    private void OnValidate()
    {
        duration = Mathf.Max(0.01f, duration);
        forceMultiplier = Mathf.Max(0f, forceMultiplier);
        maxVelocity = Mathf.Max(0.01f, maxVelocity);

        if (velocityCurve == null || velocityCurve.length == 0)
        {
            velocityCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        }
    }
}
