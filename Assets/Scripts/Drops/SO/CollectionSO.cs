using UnityEngine;

[CreateAssetMenu(fileName = "Collection", menuName = ScriptableObjectMenuPaths.COLLECTION)]
public class CollectionSO : ScriptableObject
{
    public Collection prefab;
    public CollectionAnimationConfig AnimationConfig;
}
