using System.Collections.Generic;
using UnityEngine;

public abstract class MoveBase : EntityComponentBase, IMovable, IMovementLockable
{
    protected Rigidbody2D rb;
    protected float speed;
    protected Vector2 moveDirection;
    protected PropertiesManager propertiesManager;
    private readonly HashSet<object> movementLocks = new();

    public float Speed => speed;
    public Vector2 MoveDirection => moveDirection;
    public bool IsMoving => !IsMovementLocked && moveDirection.sqrMagnitude > Mathf.Epsilon;
    public PropertiesManager PropertiesManager => propertiesManager;
    public bool IsMovementLocked => movementLocks.Count > 0;

    public override void Initialize(Entity owner)
    {
        rb = owner.GetComponent<Rigidbody2D>();
        propertiesManager = owner.GetComponent<PropertiesManager>();
        movementLocks.Clear();

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
        movementLocks.Clear();
    }

    public virtual void DisableMovement()
    {
        AddMovementLock(this);
    }

    public void AddMovementLock(object source)
    {
        if (source == null)
        {
            return;
        }

        movementLocks.Add(source);
        moveDirection = Vector2.zero;
    }

    public void RemoveMovementLock(object source)
    {
        if (source == null)
        {
            return;
        }

        movementLocks.Remove(source);
    }

    public virtual void MoveTo(Vector2 position)
    {
        if (IsMovementLocked || rb == null)
        {
            return;
        }

        moveDirection = (position - Owner.Center).normalized;
        rb.velocity = moveDirection * (Time.fixedDeltaTime * speed);
    }

    public virtual void StopMoving()
    {
        moveDirection = Vector2.zero;
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }
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
