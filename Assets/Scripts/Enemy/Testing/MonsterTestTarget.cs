using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(HealthComponent))]
public sealed class MonsterTestTarget : Entity
{
    [SerializeField] private CircleCollider2D circleCollider;
    [SerializeField] private EntityRenderer entityRenderer;

    public override Vector2 Center
    {
        get
        {
            if (circleCollider == null)
            {
                return transform.position;
            }

            return (Vector2)transform.position + circleCollider.offset;
        }
    }

    public override EntityRenderer EntityRenderer => entityRenderer;

    private void Awake()
    {
        if (circleCollider == null)
        {
            circleCollider = GetComponent<CircleCollider2D>();
        }

        if (entityRenderer == null)
        {
            entityRenderer = GetComponentInChildren<EntityRenderer>();
        }
    }
}
