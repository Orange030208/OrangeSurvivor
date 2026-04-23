using UnityEngine;

[CreateAssetMenu(fileName = "Enemy BT Config", menuName = "SO/Enemies/Enemy BT Config", order = 2)]
public sealed class BtConfigSO : ScriptableObject
{
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
}
