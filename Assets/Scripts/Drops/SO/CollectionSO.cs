using UnityEngine;

[CreateAssetMenu(fileName = "Collection", menuName = "Collections/Collection")]
public class CollectionSO : ScriptableObject
{
    public Collection prefab;
    public CollectionAnimationConfig AnimationConfig;
}
