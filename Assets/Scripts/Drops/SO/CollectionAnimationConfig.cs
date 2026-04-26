using UnityEngine;

[CreateAssetMenu(fileName = "CollectionAnimationConfig", menuName = "Entity/Component/Animation/CollectionAnimationConfig")]
public class CollectionAnimationConfig : EntityAnimationConfig
{
    [Header("Collection Animation States")]
    public string Idle = "Idle";
    public string Float = "Float";

    [System.NonSerialized] public int IdleHash;
    [System.NonSerialized] public int FloatHash;

    protected virtual void OnValidate()
    {
        IdleHash = Animator.StringToHash(Idle);
        FloatHash = Animator.StringToHash(Float);
    }
}
