using UnityEngine;

[CreateAssetMenu(fileName = "Enemy Presentation Config", menuName = "SO/Enemies/Enemy Presentation Config", order = 3)]
public sealed class PresentationConfigSO : ScriptableObject
{
    [Header("Animation")]
    [SerializeField] private RuntimeAnimatorController animatorController;

    [Header("FX")]
    [SerializeField] private ParticleSystem spawnFx;
    [SerializeField] private ParticleSystem deathFx;

    [Header("Audio")]
    [SerializeField] private string spawnSfxKey;
    [SerializeField] private string hitSfxKey;
    [SerializeField] private string deathSfxKey;
}