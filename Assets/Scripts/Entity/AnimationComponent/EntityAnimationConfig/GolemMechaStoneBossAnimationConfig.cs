using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "GolemMechaStoneBossAnimationConfig",
    menuName = ScriptableObjectMenuPaths.GOLEM_MECHA_STONE_BOSS_ANIMATION_CONFIG)]
public sealed class GolemMechaStoneBossAnimationConfig : EntityAnimationConfig
{
    [Header("机械石魔首领状态")]
    public string Melee = "Melee";
    public string Shoot = "Shoot";
    public string LaserCast = "LaserCast";
    public string ShieldCast = "ShieldCast";
    public string Immune = "Immune";

    [NonSerialized] public int MeleeHash;
    [NonSerialized] public int ShootHash;
    [NonSerialized] public int LaserCastHash;
    [NonSerialized] public int ShieldCastHash;
    [NonSerialized] public int ImmuneHash;

    protected override void RefreshHashes()
    {
        base.RefreshHashes();

        MeleeHash = Animator.StringToHash(Melee);
        ShootHash = Animator.StringToHash(Shoot);
        LaserCastHash = Animator.StringToHash(LaserCast);
        ShieldCastHash = Animator.StringToHash(ShieldCast);
        ImmuneHash = Animator.StringToHash(Immune);
    }
}
