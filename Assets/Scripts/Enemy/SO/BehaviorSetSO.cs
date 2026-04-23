using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Behavior Set", menuName = "SO/Behavior Set", order = 1)]
public sealed class BehaviorSetSO : ScriptableObject
{
    [SerializeField] private MovePresetSO[] movementPresets = Array.Empty<MovePresetSO>();
    [SerializeField] private AttackPresetSO[] attackPresets = Array.Empty<AttackPresetSO>();

    [Header("Compatibility")]
    [SerializeField] private string defaultMovementId;
    [SerializeField] private string defaultAttackId;
    [SerializeField] private bool preloadAllMovementComponents = true;
    [SerializeField] private bool preloadAllAttackComponents = true;

    public MovePresetSO[] MovementPresets => movementPresets;
    public AttackPresetSO[] AttackPresets => attackPresets;
    public string DefaultMovementId => defaultMovementId;
    public string DefaultAttackId => defaultAttackId;
    public bool PreloadAllMovementComponents => preloadAllMovementComponents;
    public bool PreloadAllAttackComponents => preloadAllAttackComponents;

    public MovePresetSO GetDefaultMovementPreset()
    {
        return GetMovementPreset(defaultMovementId);
    }

    public AttackPresetSO GetDefaultAttackPreset()
    {
        return GetAttackPreset(defaultAttackId);
    }

    public MovePresetSO GetMovementPreset(string moveId)
    {
        TryGetMovementPreset(moveId, out MovePresetSO preset);
        return preset;
    }

    public AttackPresetSO GetAttackPreset(string attackId)
    {
        TryGetAttackPreset(attackId, out AttackPresetSO preset);
        return preset;
    }

    public bool TryGetMovementPreset(string moveId, out MovePresetSO preset)
    {
        return TryFindPreset(movementPresets, moveId, static item => item != null ? item.MoveId : null, out preset);
    }

    public bool TryGetAttackPreset(string attackId, out AttackPresetSO preset)
    {
        return TryFindPreset(attackPresets, attackId, static item => item != null ? item.AttackId : null, out preset);
    }

    private static bool TryFindPreset<TPreset>(TPreset[] presets, string presetId, Func<TPreset, string> idSelector, out TPreset preset)
        where TPreset : UnityEngine.Object
    {
        if (presets == null || presets.Length == 0 || string.IsNullOrWhiteSpace(presetId))
        {
            preset = null;
            return false;
        }

        for (int i = 0; i < presets.Length; i++)
        {
            TPreset candidate = presets[i];
            if (candidate == null)
            {
                continue;
            }

            if (string.Equals(idSelector(candidate), presetId, StringComparison.Ordinal))
            {
                preset = candidate;
                return true;
            }
        }

        preset = null;
        return false;
    }
}
