using UnityEngine;

[CreateAssetMenu(fileName = "Collection", menuName = ScriptableObjectMenuPaths.COLLECTION)]
public class CollectionSO : ScriptableObject
{
    public Collection prefab;
    public EntityAnimationConfig AnimationConfig;
    [Tooltip("该掉落物被收集时播放的语义音效。")]
    [SerializeField] private AudioSfxKey collectSfxKey = AudioSfxKey.None;

    public AudioSfxKey CollectSfxKey => collectSfxKey;
}
