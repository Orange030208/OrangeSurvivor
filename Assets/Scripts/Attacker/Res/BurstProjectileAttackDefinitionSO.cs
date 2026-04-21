using UnityEngine;

[CreateAssetMenu(fileName = "Burst Projectile Attack Definition", menuName = "SO/Attack/Burst Projectile Attack Definition", order = 3)]
public class BurstProjectileAttackDefinitionSO : ProjectileAttackDefinitionSO
{
    public override EnemyProjectileAttackMode AttackMode => EnemyProjectileAttackMode.Burst;
}
