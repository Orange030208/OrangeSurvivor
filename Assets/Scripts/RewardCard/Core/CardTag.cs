[System.Flags]
public enum CardTag
{
    None = 0,
    Attack = 1 << 0,
    Defense = 1 << 1,
    Critical = 1 << 2,
    AttackSpeed = 1 << 3,
    MoveSpeed = 1 << 4,
    Pickup = 1 << 5,
    Economy = 1 << 6,
    Weapon = 1 << 7,
    Melee = 1 << 8,
    Ranged = 1 << 9,
    Projectile = 1 << 10,
    Recovery = 1 << 11,
    LowHealth = 1 << 12,
    AreaDamage = 1 << 13
}
