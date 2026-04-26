using UnityEngine;

[CreateAssetMenu(fileName = "EnemyAnimationConfig", menuName = "Entity/Component/Animation/EnemyAnimationConfig")]
public class EnemyAnimationConfig : EntityAnimationConfig
{
    [Header("通用基础状态")] 
    public string Idle = "Idle";
    public string Move = "Move";
    public string Death = "Death";
    
    [System.NonSerialized] public int IdleHash;
    [System.NonSerialized] public int MoveHash;
    [System.NonSerialized] public int DeathHash;

    protected virtual void OnValidate()
    {
        // 预计算哈希ID，避免运行时字符串开销
        IdleHash = Animator.StringToHash(Idle);
        MoveHash = Animator.StringToHash(Move);
        DeathHash = Animator.StringToHash(Death);
    }
}