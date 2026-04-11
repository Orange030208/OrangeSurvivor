using UnityEngine;

public readonly struct ResolvedWeaponHit
{
    public float Damage { get; }
    public bool IsCritical { get; }

    public ResolvedWeaponHit(float damage, bool isCritical)
    {
        Damage = damage;
        IsCritical = isCritical;
    }

    public DamageInfo ToDamageInfo(Vector2 position)
    {
        return new DamageInfo(Damage, position, IsCritical);
    }
}
