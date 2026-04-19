using UnityEngine;

public abstract class AttackDefinitionSO : ScriptableObject
{
    private const float MIN_ATTACK_FREQUENCY = 0.01f;

    [Header("Identity")]
    [SerializeField] private string attackId = "Attack_Default";

    [Header("Stats")]
    [SerializeField] private float damage = 1f;
    [SerializeField] private float attackFrequency = 1f;

    public string AttackId => attackId;
    public float Damage => Mathf.Max(0f, damage);
    public float AttackFrequency => Mathf.Max(MIN_ATTACK_FREQUENCY, attackFrequency);
    public float AttackInterval => 1f / AttackFrequency;

    public abstract AttackType Type { get; }
}
