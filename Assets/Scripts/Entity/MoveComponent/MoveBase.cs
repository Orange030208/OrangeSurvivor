using UnityEngine;

public abstract class MoveBase : EntityComponentBase, IMovable
{
    protected Rigidbody2D rb;
    protected float speed;
    protected bool movementDisabled;
    protected Vector2 moveDirection;
    protected PropertiesManager propertiesManager;

    public float Speed => speed;
    public Vector2 MoveDirection => moveDirection;
    public bool IsMoving => !movementDisabled && moveDirection.sqrMagnitude > Mathf.Epsilon;
    public PropertiesManager PropertiesManager => propertiesManager;

    public override void Initialize(Entity owner)
    {
        rb = owner.GetComponent<Rigidbody2D>();
        propertiesManager = owner.GetComponent<PropertiesManager>();
        movementDisabled = false;

        RefreshMoveSpeed();
    }

    public override void OnEnableComponent()
    {
        BindProperties();
    }

    public override void OnDisableComponent()
    {
        UnbindProperties();
    }

    public virtual void EnableMovement()
    {
        movementDisabled = false;
    }

    public virtual void DisableMovement()
    {
        movementDisabled = true;
        moveDirection = Vector2.zero;
    }

    public virtual void MoveTo(Vector2 position)
    {
        moveDirection = (position - Owner.Center).normalized;
        rb.velocity = moveDirection * (Time.fixedDeltaTime * speed);
    }

    public virtual void StopMoving()
    {
        moveDirection = Vector2.zero;
        rb.velocity = Vector2.zero;
    }

    public override int Priority => EntityComponentBase.PriorityPreset.RelyOthers;

    protected virtual void OnPropertyChanged(PropType propType, float _)
    {
        if (propType == PropType.MoveSpeed)
        {
            RefreshMoveSpeed();
        }
    }

    protected virtual void OnAllPropertiesChanged()
    {
        RefreshMoveSpeed();
    }

    protected void RefreshMoveSpeed()
    {
        speed = propertiesManager.GetPropValue(PropType.MoveSpeed);
    }

    private void BindProperties()
    {
        UnbindProperties();
        propertiesManager.OnAllPropertiesChanged += OnAllPropertiesChanged;
        propertiesManager.OnPropertyChanged += OnPropertyChanged;
    }

    private void UnbindProperties()
    {
        propertiesManager.OnAllPropertiesChanged -= OnAllPropertiesChanged;
        propertiesManager.OnPropertyChanged -= OnPropertyChanged;
    }
}
