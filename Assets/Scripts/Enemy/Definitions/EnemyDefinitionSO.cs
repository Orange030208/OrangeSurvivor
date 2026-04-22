using UnityEngine;

[CreateAssetMenu(fileName = "Enemy Definition", menuName = "SO/Enemies/Enemy Definition", order = 0)]
public class EnemyDefinitionSO : ScriptableObject
{
    private const float MIN_MAX_HEALTH = 1f;
    private const float MIN_MOVE_SPEED = 0f;
    private const float MIN_ATTACK_RADIUS = 0.1f;

    [Header("Identity")]
    [SerializeField] private string enemyId = "Enemy_001";
    [SerializeField] private string displayName = "Enemy";
    [SerializeField] private EnemyRole role = EnemyRole.Normal;

    [Header("Template")]
    [SerializeField] private EnemyTemplateKind templateKind = EnemyTemplateKind.Melee;

    [Header("Stats")]
    [SerializeField] private float maxHealth = 10f;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float attackDetectionRadius = 1f;

    [Header("Combat")]
    [SerializeField] private MoveConfigSO moveConfig;
    [SerializeField] private AttackConfigSO attackConfig;

    public string EnemyId => enemyId;
    public string DisplayName => displayName;
    public EnemyRole Role => role;
    public EnemyTemplateKind TemplateKind => templateKind;
    public float MaxHealth => Mathf.Max(MIN_MAX_HEALTH, maxHealth);
    public float MoveSpeed => Mathf.Max(MIN_MOVE_SPEED, moveSpeed);
    public float AttackDetectionRadius => Mathf.Max(MIN_ATTACK_RADIUS, attackDetectionRadius);
    public MoveConfigSO MoveConfig => moveConfig;
    public AttackConfigSO AttackConfig => attackConfig;

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(enemyId))
        {
            enemyId = "Enemy_001";
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = enemyId;
        }

        maxHealth = Mathf.Max(MIN_MAX_HEALTH, maxHealth);
        moveSpeed = Mathf.Max(MIN_MOVE_SPEED, moveSpeed);
        attackDetectionRadius = Mathf.Max(MIN_ATTACK_RADIUS, attackDetectionRadius);
    }
}
