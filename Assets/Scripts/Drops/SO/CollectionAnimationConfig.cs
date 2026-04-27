using UnityEngine;

[CreateAssetMenu(fileName = "CollectionAnimationConfig", menuName = ScriptableObjectMenuPaths.COLLECTION_ANIMATION_CONFIG)]
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
