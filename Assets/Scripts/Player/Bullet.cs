using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] protected float moveSpeed = 5f;
    [SerializeField] protected LayerMask targetsLayerMask;
    [SerializeField] protected float maxLifetime = 5f;

    private Rigidbody2D rb;
    private float lifetimeTimer;
    protected ProjectileLaunchContext launchContext;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    protected virtual void OnEnable()
    {
        lifetimeTimer = 0f;
    }

    protected virtual void Update()
    {
        lifetimeTimer += Time.deltaTime;
        if (lifetimeTimer >= maxLifetime)
        {
            Destroy(gameObject);
        }
    }

    public virtual void Launch(ProjectileLaunchContext context)
    {
        launchContext = context;
        transform.position = context.SpawnPosition;
        transform.right = context.Direction;
        rb.velocity = context.Direction * moveSpeed;
        OnLaunched(context);
    }

    protected virtual void OnLaunched(ProjectileLaunchContext context)
    {
    }

    protected virtual void OnTriggerEnter2D(Collider2D collider)
    {
        if (!IsInLayerMask(collider.gameObject.layer, targetsLayerMask))
        {
            return;
        }

        if (!collider.TryGetComponent(out HealthComponent healthComponent))
        {
            return;
        }

        ApplyImpact(healthComponent);
        Destroy(gameObject);
    }

    protected virtual void ApplyImpact(HealthComponent healthComponent)
    {
        healthComponent.TakeDamage(launchContext.Hit.ToDamageInfo(healthComponent.transform.position));
    }

    private bool IsInLayerMask(int layer, LayerMask layerMask)
    {
        return (layerMask.value & (1 << layer)) != 0;
    }
}
