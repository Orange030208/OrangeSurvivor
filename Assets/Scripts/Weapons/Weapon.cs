using UnityEngine;

public class Weapon:MonoBehaviour
{
    [SerializeField] protected float attackDelay;
    protected float attackTimer;
    [SerializeField] protected int damage = 1;
    [SerializeField] protected float aimLerp;
    [SerializeField] protected LayerMask enemyLayerMask;
    [SerializeField] protected float range;
    [SerializeField] protected Animator _animator;
    
    protected Enemy GetClosestEnemy()
    {
        Enemy closestEnemy = null;

        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, range, enemyLayerMask);
        
        if (colliders.Length <= 0)
        {
            return null;
        }
        
        float minDistance = range;
        for (int i = 0; i < colliders.Length; i++)
        {
            Enemy enemyChecked = colliders[i].GetComponent<Enemy>();
        
            float distanceToEnemy = Vector2.Distance(transform.position, enemyChecked.transform.position);
        
            if (distanceToEnemy < minDistance)
            {
                closestEnemy = enemyChecked;
                minDistance = distanceToEnemy;
            }
        }

        return closestEnemy;
    }

    protected int GetDamage(out bool isCriticalHit)
    {
        isCriticalHit = false;

        int rand = Random.Range(0, 101);
        if (rand <= 50)
        {
            isCriticalHit = true;
            return damage * 2;
        }
        
        return damage;
    }
}