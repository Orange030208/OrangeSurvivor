using UnityEngine;

[CreateAssetMenu(fileName = "Enemy BT Config", menuName = "SO/Enemies/Enemy BT Config", order = 2)]
public sealed class BtConfigSO : ScriptableObject
{
    [Header("Preset Ids")]
    [SerializeField] private string meleeMovePresetId;
    [SerializeField] private string meleeAttackPresetId;
    [SerializeField] private string orbitMovePresetId;
    [SerializeField] private string orbitAttackPresetId;
    [SerializeField] private string retreatMovePresetId;
    [SerializeField] private string retreatAttackPresetId;

    [Header("Distance Thresholds")]
    [SerializeField] private float meleeEnterDistance = 2f;
    [SerializeField] private float meleeExitDistance = 2.5f;
    [SerializeField] private float orbitEnterDistance = 5f;
    [SerializeField] private float orbitExitDistance = 6f;
    [SerializeField] private float retreatDesiredDistance = 8f;

    [Header("Health Thresholds")]
    [SerializeField] [Range(0f, 1f)] private float retreatHealthRatio = 0.3f;
    [SerializeField] [Range(0f, 1f)] private float enragedHealthRatio = 0.5f;

    [Header("Timing")]
    [SerializeField] private float styleSwitchCooldown = 0.25f;
    [SerializeField] private float burstWindow = 2f;
    [SerializeField] private float orbitDuration = 3f;

    [Header("Sensing")]
    [SerializeField] private float targetLostDelay = 1f;
    [SerializeField] private bool requireLineOfSightForRanged = false;

    public string MeleeMovePresetId => meleeMovePresetId;
    public string MeleeAttackPresetId => meleeAttackPresetId;
    public string OrbitMovePresetId => orbitMovePresetId;
    public string OrbitAttackPresetId => orbitAttackPresetId;
    public string RetreatMovePresetId => retreatMovePresetId;
    public string RetreatAttackPresetId => retreatAttackPresetId;
    public float MeleeEnterDistance => Mathf.Max(0f, meleeEnterDistance);
    public float MeleeExitDistance => Mathf.Max(MeleeEnterDistance, meleeExitDistance);
    public float OrbitEnterDistance => Mathf.Max(0f, orbitEnterDistance);
    public float OrbitExitDistance => Mathf.Max(OrbitEnterDistance, orbitExitDistance);
    public float RetreatDesiredDistance => Mathf.Max(0f, retreatDesiredDistance);
    public float RetreatHealthRatio => Mathf.Clamp01(retreatHealthRatio);
    public float EnragedHealthRatio => Mathf.Clamp01(enragedHealthRatio);
    public float StyleSwitchCooldown => Mathf.Max(0f, styleSwitchCooldown);
    public float BurstWindow => Mathf.Max(0f, burstWindow);
    public float OrbitDuration => Mathf.Max(0f, orbitDuration);
    public float TargetLostDelay => Mathf.Max(0f, targetLostDelay);
    public bool RequireLineOfSightForRanged => requireLineOfSightForRanged;
}
