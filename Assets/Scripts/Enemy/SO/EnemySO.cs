using UnityEngine;

[CreateAssetMenu(fileName = "Enemy", menuName = "SO/Enemies/Enemy", order = 0)]
public sealed class EnemySO : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string enemyId = "Enemy_001";
    [SerializeField] private string displayName = "Enemy";
    [SerializeField] private EnemyRole role = EnemyRole.Normal;
    [SerializeField] private EnemyTemplateKind templateKind = EnemyTemplateKind.Melee;

    [Header("Base Stats")]
    [SerializeField] private float maxHealth = 10f;
    [SerializeField] private float baseMoveSpeed = 2f;
    [SerializeField] private float baseDetectionRadius = 8f;

    [Header("Behavior")]
    [SerializeField] private BehaviorSetSO behaviorSet;
    [SerializeField] private BtConfigSO btConfig;

    [Header("Presentation")]
    [SerializeField] private PresentationConfigSO presentationConfig;
}