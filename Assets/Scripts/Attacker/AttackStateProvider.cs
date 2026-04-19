using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Enemy))]
public sealed class AttackStateProvider : MonoBehaviour, IAttackStateProvider
{
    private Enemy enemy;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    public bool CanAttack(in AttackStateContext context)
    {
        return enemy != null && enemy.CanExecuteAttack;
    }
}
