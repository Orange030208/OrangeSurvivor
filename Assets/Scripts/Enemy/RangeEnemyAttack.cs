using UnityEngine;

public class RangeEnemyAttack : MonoBehaviour
{
    [SerializeField] private Transform shootingPoint;
    [SerializeField] private EnemyBullet bulletPrefab;
    [SerializeField] private int damage;
    [SerializeField] private float attackFrequency;

    private Player target;
    private float attackDelay;
    private float attackTimer;

    private void Start()
    {
        attackDelay = 1f / attackFrequency;
        attackTimer = attackDelay;
    }

    public void SetTarget(Player target)
    {
        this.target = target;
    }

    public void AutoAim()
    {
        ManageShoot();
    }

    private void ManageShoot()
    {
        if (target == null)
        {
            return;
        }

        attackTimer += Time.deltaTime;
        if (attackTimer < attackDelay)
        {
            return;
        }

        attackTimer = 0f;
        Shoot();
    }

    private void Shoot()
    {
        if (bulletPrefab == null || shootingPoint == null)
        {
            return;
        }

        Vector2 direction = (target.Center - (Vector2)shootingPoint.position).normalized;
        EnemyBullet enemyBullet = Instantiate(bulletPrefab, shootingPoint.position, Quaternion.identity);
        enemyBullet.Launch(new ProjectileLaunchContext(shootingPoint.position, direction, new ResolvedWeaponHit(damage, false), 0, 0, 0, ProjectileFiringMode.Default));
    }
}
