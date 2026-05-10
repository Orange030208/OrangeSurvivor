using UnityEngine;
using System;

[CreateAssetMenu(fileName = "EntityAnimationConfig", menuName = ScriptableObjectMenuPaths.ENTITY_ANIMATION_CONFIG)]
/// <summary>
/// 动画状态映射配置
/// </summary>
public class EntityAnimationConfig : ScriptableObject
{
    public enum FacingDirection
    {
        Right = 0,
        Left = 1
    }

    public RuntimeAnimatorController AnimatorController;

    [Header("朝向")]
    public FacingDirection DefaultFacingDirection = FacingDirection.Right;
    
    [Header("通用基础状态")] 
    public string Idle = "Idle";
    public string Move = "Move";

    [Header("收集动画状态")]
    public string Float = "Float";
    public string Open = "Open";
    
    public string Attack = "Attack";
    public string Attack1 = "Attack1";
    public string Attack2 = "Attack2";
    public string Attack3 = "Attack3";

    public string MeleeAttack = "MeleeAttack";
    public string MeleeAttack1 = "MeleeAttack1";
    public string MeleeAttack2 = "MeleeAttack2";
    public string MeleeAttack3 = "MeleeAttack3";
    
    public string RangeAttack = "RangeAttack";
    public string RangeAttack1 = "RangeAttack1";
    public string RangeAttack2 = "RangeAttack2";
    public string RangeAttack3 = "RangeAttack3";
    
    public string Death = "Death";
    
    [NonSerialized] public int IdleHash;
    [NonSerialized] public int MoveHash;
    [NonSerialized] public int FloatHash;
    [NonSerialized] public int OpenHash;
    [NonSerialized] public int AttackHash;
    [NonSerialized] public int Attack1Hash;
    [NonSerialized] public int Attack2Hash;
    [NonSerialized] public int Attack3Hash;

    [NonSerialized] public int MeleeAttackHash;
    [NonSerialized] public int MeleeAttack1Hash;
    [NonSerialized] public int MeleeAttack2Hash;
    [NonSerialized] public int MeleeAttack3Hash;

    [NonSerialized] public int RangeAttackHash;
    [NonSerialized] public int RangeAttack1Hash;
    [NonSerialized] public int RangeAttack2Hash;
    [NonSerialized] public int RangeAttack3Hash;

    [NonSerialized] public int DeathHash;

    protected virtual void OnEnable()
    {
        RefreshHashes();
    }

    protected virtual void OnValidate()
    {
        RefreshHashes();
    }

    protected virtual void RefreshHashes()
    {
        IdleHash = Animator.StringToHash(Idle);
        MoveHash = Animator.StringToHash(Move);
        FloatHash = Animator.StringToHash(Float);
        OpenHash = Animator.StringToHash(Open);
        AttackHash = Animator.StringToHash(Attack);
        Attack1Hash = Animator.StringToHash(Attack1);
        Attack2Hash = Animator.StringToHash(Attack2);
        Attack3Hash = Animator.StringToHash(Attack3);

        MeleeAttackHash = Animator.StringToHash(MeleeAttack);
        MeleeAttack1Hash = Animator.StringToHash(MeleeAttack1);
        MeleeAttack2Hash = Animator.StringToHash(MeleeAttack2);
        MeleeAttack3Hash = Animator.StringToHash(MeleeAttack3);

        RangeAttackHash = Animator.StringToHash(RangeAttack);
        RangeAttack1Hash = Animator.StringToHash(RangeAttack1);
        RangeAttack2Hash = Animator.StringToHash(RangeAttack2);
        RangeAttack3Hash = Animator.StringToHash(RangeAttack3);

        DeathHash = Animator.StringToHash(Death);
    }
}
