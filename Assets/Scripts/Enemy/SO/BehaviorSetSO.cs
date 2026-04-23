using UnityEngine;

[CreateAssetMenu(fileName = "Enemy Behavior Set", menuName = "SO/Enemies/Enemy Behavior Set", order = 1)]
public sealed class BehaviorSetSO : ScriptableObject
{
    [Header("Movement Presets")]
    [SerializeField] private MovePresetSO[] movementPresets;

    [Header("Attack Presets")]
    [SerializeField] private AttackPresetSO[] attackPresets;

    [Header("Defaults")]
    [SerializeField] private string defaultMovementId;
    [SerializeField] private string defaultAttackId;

    [Header("Runtime Assembly")]
    [SerializeField] private bool preloadAllMovementComponents = true;
    [SerializeField] private bool preloadAllAttackComponents = true;
}