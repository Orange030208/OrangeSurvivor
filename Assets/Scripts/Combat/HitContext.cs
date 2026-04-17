using UnityEngine;

//计算链工作台
public sealed class HitContext
{
    public HitRequest Request { get; }
    public float Damage { get; set; }
    public float CritChance { get; set; }
    public float CritMultiplier { get; set; }
    public float DodgeChance { get; set; }
    public float DamageReduction { get; set; }
    public bool IsCritical { get; set; }
    public bool IsDodged { get; set; }
    public bool IsBlocked { get; set; }
    public bool IsCancelled { get; set; }

    public HitContext(HitRequest request)
    {
        Request = request;
        Damage = request.Spec.BaseDamage;
        CritChance = request.Spec.CritChance;
        CritMultiplier = request.Spec.CritMultiplier;
        DodgeChance = 0f;
        DamageReduction = 0f;
        IsCritical = false;
        IsDodged = false;
        IsBlocked = false;
        IsCancelled = false;
    }
}
