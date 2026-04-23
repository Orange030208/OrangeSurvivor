using UnityEngine;

[CreateAssetMenu(fileName = "Enemy", menuName = "SO/Enemies/Enemy", order = 0)]
public sealed class EnemySO : ScriptableObject
{
    private const float MIN_MAX_HEALTH = 1f;
    private const float MIN_MOVE_SPEED = 0f;
    private const float MIN_DETECTION_RADIUS = 0f;

    [Header("Identity")]
    [SerializeField] private string enemyId = "Enemy_001";
    [SerializeField] private string displayName = "Enemy";
    [SerializeField] private EnemyRole role = EnemyRole.Normal;
    [SerializeField] private Enemy enemyPrefab;

    [Header("Base Stats")]
    [SerializeField] private float maxHealth = 10f;
    [SerializeField] private float baseMoveSpeed = 2f;
    [SerializeField] private float baseDetectionRadius = 8f;

    [Header("Behavior")]
    [SerializeField] private BehaviorSetSO behaviorSet;
    [SerializeField] private BtConfigSO btConfig;

    public string EnemyId => enemyId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? enemyId : displayName;
    public EnemyRole Role => role;
    public Enemy EnemyPrefab => enemyPrefab;
    public float MaxHealth => Mathf.Max(MIN_MAX_HEALTH, maxHealth);
    public float BaseMoveSpeed => Mathf.Max(MIN_MOVE_SPEED, baseMoveSpeed);
    public float BaseDetectionRadius => Mathf.Max(MIN_DETECTION_RADIUS, baseDetectionRadius);
    public BehaviorSetSO BehaviorSet => behaviorSet;
    public BtConfigSO BtConfig => btConfig;

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
        baseMoveSpeed = Mathf.Max(MIN_MOVE_SPEED, baseMoveSpeed);
        baseDetectionRadius = Mathf.Max(MIN_DETECTION_RADIUS, baseDetectionRadius);
    }
}
