using UnityEngine;

[CreateAssetMenu(fileName = "Spread Projectile Attack Definition", menuName = "SO/Attack/Spread Projectile Attack Definition", order = 2)]
public class SpreadProjectileAttackDefinitionSO : ProjectileAttackDefinitionSO
{
    public override EnemyProjectileAttackMode AttackMode => EnemyProjectileAttackMode.Spread;
}
