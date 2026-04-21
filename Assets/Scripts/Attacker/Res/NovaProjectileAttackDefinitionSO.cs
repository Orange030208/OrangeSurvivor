using UnityEngine;

[CreateAssetMenu(fileName = "Nova Projectile Attack Definition", menuName = "SO/Attack/Nova Projectile Attack Definition", order = 4)]
public class NovaProjectileAttackDefinitionSO : ProjectileAttackDefinitionSO
{
    public override EnemyProjectileAttackMode AttackMode => EnemyProjectileAttackMode.Nova;
}
