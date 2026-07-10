using UnityEngine;

/// <summary>
/// Carries the exact HeartSteel stack gain that just happened, so follow-up effects can react
/// without re-reading weapon hit state or duplicating HeartSteel rules.
/// </summary>
public readonly struct HeartSteelStackGainContext
{
    public HeartSteelStackGainContext(
        Entity owner,
        Weapon triggerWeapon,
        Entity triggerTarget,
        HitResult triggerHitResult,
        int oldStacks,
        int newStacks,
        int gainedStacks,
        float currentMaxHealth)
    {
        Owner = owner;
        TriggerWeapon = triggerWeapon;
        TriggerTarget = triggerTarget;
        TriggerHitResult = triggerHitResult;
        OldStacks = Mathf.Max(0, oldStacks);
        NewStacks = Mathf.Max(0, newStacks);
        GainedStacks = Mathf.Max(0, gainedStacks);
        CurrentMaxHealth = Mathf.Max(0f, currentMaxHealth);
    }

    public Entity Owner { get; }
    public Weapon TriggerWeapon { get; }
    public Entity TriggerTarget { get; }
    public HitResult TriggerHitResult { get; }
    public int OldStacks { get; }
    public int NewStacks { get; }
    public int GainedStacks { get; }
    public float CurrentMaxHealth { get; }

    public string WeaponId => TriggerWeapon != null && TriggerWeapon.WeaponData != null
        ? TriggerWeapon.WeaponData.WeaponId
        : string.Empty;
}
