using UnityEngine;

[CreateAssetMenu(fileName = "SkeletonMeteorhammer2Enemy", menuName = ScriptableObjectMenuPaths.SKELETON_METEORHAMMER2_ENEMY, order = 4)]
public sealed class SkeletonMeteorhammer2EnemySO : SkeletonEnemySO
{
    public new const string ATTACK_ACTION_ID = "SkeletonMeteorhammer2_Attack";
    private const float MIN_RECTANGLE_WIDTH = 0.01f;

    [Header("流星锤二型攻击")]
    [SerializeField, Range(0f, 1f)] private float firstAttackCommitNormalizedTime = 0.33f;
    [SerializeField, Min(0f)] private float firstAttackDamageMultiplier = 1f;
    [SerializeField, Min(0f)] private float firstAttackRangeMultiplier = 1f;
    [SerializeField, Range(0f, 1f)] private float secondAttackCommitNormalizedTime = 0.66f;
    [SerializeField, Min(0f)] private float secondAttackDamageMultiplier = 1f;
    [SerializeField, Min(0f)] private float secondAttackLengthMultiplier = 1.5f;
    [SerializeField, Min(MIN_RECTANGLE_WIDTH)] private float secondAttackWidth = 0.8f;

    public float FirstAttackCommitNormalizedTime => Mathf.Clamp01(firstAttackCommitNormalizedTime);
    public float FirstAttackDamageMultiplier => Mathf.Max(0f, firstAttackDamageMultiplier);
    public float FirstAttackRangeMultiplier => Mathf.Max(0f, firstAttackRangeMultiplier);
    public float SecondAttackCommitNormalizedTime => Mathf.Clamp01(secondAttackCommitNormalizedTime);
    public float SecondAttackDamageMultiplier => Mathf.Max(0f, secondAttackDamageMultiplier);
    public float SecondAttackLengthMultiplier => Mathf.Max(0f, secondAttackLengthMultiplier);
    public float SecondAttackWidth => Mathf.Max(MIN_RECTANGLE_WIDTH, secondAttackWidth);

    protected override string DefaultAttackActionId => ATTACK_ACTION_ID;

    protected override void OnValidate()
    {
        base.OnValidate();
        firstAttackCommitNormalizedTime = Mathf.Clamp01(firstAttackCommitNormalizedTime);
        firstAttackDamageMultiplier = Mathf.Max(0f, firstAttackDamageMultiplier);
        firstAttackRangeMultiplier = Mathf.Max(0f, firstAttackRangeMultiplier);
        secondAttackCommitNormalizedTime = Mathf.Clamp01(secondAttackCommitNormalizedTime);
        secondAttackDamageMultiplier = Mathf.Max(0f, secondAttackDamageMultiplier);
        secondAttackLengthMultiplier = Mathf.Max(0f, secondAttackLengthMultiplier);
        secondAttackWidth = Mathf.Max(MIN_RECTANGLE_WIDTH, secondAttackWidth);
    }
}
