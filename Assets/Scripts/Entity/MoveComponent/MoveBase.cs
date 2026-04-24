using UnityEngine;

public abstract class MoveBase : EntityComponentBase, IMovable
{
    protected Rigidbody2D rb;
    protected float speed;
    protected bool movementDisabled;
    protected Vector2 moveDirection;

    public float Speed => speed;
    public Vector2 MoveDirection => moveDirection;
    public bool IsMoving => !movementDisabled && moveDirection.sqrMagnitude > Mathf.Epsilon;

    public override void Initialize(Entity owner)
    {
        rb = owner.GetComponent<Rigidbody2D>();
        movementDisabled = false;
    }

    public virtual void Enable()
    {
        movementDisabled = false;
    }

    public virtual void Disable()
    {
        movementDisabled = true;
        moveDirection = Vector2.zero;
    }
    
    public virtual void MoveTo(Vector2 position)
    {
        rb.velocity = (position - Owner.Center).normalized * Time.deltaTime * speed;
    }

    public virtual void StopMoving()
    {
        rb.velocity = Vector2.zero;
    }
}
