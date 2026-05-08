using UnityEngine;

[CreateAssetMenu(fileName = "SkeletonMeteorhammerEnemy", menuName = ScriptableObjectMenuPaths.SKELETON_METEORHAMMER_ENEMY, order = 3)]
public sealed class SkeletonMeteorhammerEnemySO : SkeletonEnemySO
{
    public new const string ATTACK_ACTION_ID = "SkeletonMeteorhammer_Attack";

    [Header("Meteorhammer Attack")]
    [SerializeField, Range(0f, 1f)] private float firstAttackCommitNormalizedTime = 0.35f;
    [SerializeField, Min(0f)] private float firstAttackRangeMultiplier = 1f;
    [SerializeField, Range(0f, 1f)] private float secondAttackCommitNormalizedTime = 0.7f;
    [SerializeField, Min(0f)] private float secondAttackRangeMultiplier = 1.5f;

    public float FirstAttackCommitNormalizedTime => Mathf.Clamp01(firstAttackCommitNormalizedTime);
    public float FirstAttackRangeMultiplier => Mathf.Max(0f, firstAttackRangeMultiplier);
    public float SecondAttackCommitNormalizedTime => Mathf.Clamp01(secondAttackCommitNormalizedTime);
    public float SecondAttackRangeMultiplier => Mathf.Max(0f, secondAttackRangeMultiplier);

    protected override string DefaultAttackActionId => ATTACK_ACTION_ID;

    protected override void OnValidate()
    {
        base.OnValidate();
        firstAttackCommitNormalizedTime = Mathf.Clamp01(firstAttackCommitNormalizedTime);
        firstAttackRangeMultiplier = Mathf.Max(0f, firstAttackRangeMultiplier);
        secondAttackCommitNormalizedTime = Mathf.Clamp01(secondAttackCommitNormalizedTime);
        secondAttackRangeMultiplier = Mathf.Max(0f, secondAttackRangeMultiplier);
    }
}
