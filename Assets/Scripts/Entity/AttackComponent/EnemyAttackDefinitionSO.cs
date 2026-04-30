using UnityEngine;

[CreateAssetMenu(fileName = "Enemy Attack Definition", menuName = ScriptableObjectMenuPaths.ENEMY_ATTACK_DEFINITION)]
public sealed class EnemyAttackDefinitionSO : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string id;
    [SerializeField] private string displayName;

    [Header("Execution")]
    [SerializeField] private EnemyAttackExecutionKind executionKind = EnemyAttackExecutionKind.DirectDamage;
    [SerializeField] private AttackHitShapeSO hitShape;
    [SerializeField] private ProjectileDefinitionSO projectileDefinition;

    [Header("Stats")]
    [SerializeField, Min(0f)] private float cooldown = 1f;
    [SerializeField, Min(0f)] private float damageMultiplier = 1f;
    [SerializeField] private AttackRangeSource rangeSource = AttackRangeSource.AttackRangeProp;
    [SerializeField, Min(0f)] private float fixedRange = 1f;
    [SerializeField, Min(0f)] private float rangeMultiplier = 1f;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public EnemyAttackExecutionKind ExecutionKind => executionKind;
    public AttackHitShapeSO HitShape => hitShape;
    public ProjectileDefinitionSO ProjectileDefinition => projectileDefinition;
    public float Cooldown => Mathf.Max(0f, cooldown);
    public float DamageMultiplier => Mathf.Max(0f, damageMultiplier);
    public AttackRangeSource RangeSource => rangeSource;
    public float FixedRange => Mathf.Max(0f, fixedRange);
    public float RangeMultiplier => Mathf.Max(0f, rangeMultiplier);
    public bool RequiresHitShape => executionKind == EnemyAttackExecutionKind.DirectDamage;
    public bool RequiresProjectile => executionKind == EnemyAttackExecutionKind.Projectile;

    private void OnValidate()
    {
        cooldown = Mathf.Max(0f, cooldown);
        damageMultiplier = Mathf.Max(0f, damageMultiplier);
        fixedRange = Mathf.Max(0f, fixedRange);
        rangeMultiplier = Mathf.Max(0f, rangeMultiplier);
    }
}
