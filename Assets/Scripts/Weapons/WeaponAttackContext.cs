using UnityEngine;

public readonly struct WeaponAttackContext
{
    public Weapon Weapon { get; }
    public Transform Origin { get; }
    public Enemy Target { get; }
    public Vector2 AimDirection { get; }
    public WeaponRuntimeStats Stats { get; }
    public ResolvedWeaponHit Hit { get; }

    public WeaponAttackContext(Weapon weapon, Transform origin, Enemy target, Vector2 aimDirection, WeaponRuntimeStats stats, ResolvedWeaponHit hit)
    {
        Weapon = weapon;
        Origin = origin;
        Target = target;
        AimDirection = aimDirection;
        Stats = stats;
        Hit = hit;
    }
}
