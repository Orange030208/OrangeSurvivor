using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : EntityComponentBase, IMovable, IMovementLockable
{
    private float speed = 0;

    private Rigidbody2D rb;
    private Player owner;
    private PropertiesManager propertiesManager;
    private Vector2 moveDirection;
    private readonly HashSet<object> movementLocks = new();
    public override Entity Owner => owner;
    public PropertiesManager PropertiesManager => propertiesManager;
    public bool IsMovementLocked => movementLocks.Count > 0;
    public override void Initialize(Entity owner)
    {
        this.owner = owner as Player;
        this.rb = this.owner.Rb;
        this.propertiesManager = this.owner.PropertiesManager;
        movementLocks.Clear();

        GameEventBus.Subscribe<PlayerMoveInputChangedEvent>(OnMoveInputChanged);
        propertiesManager.OnAllPropertiesChanged += UpdateSpeed;
        propertiesManager.OnPropertyChanged += OnPropertyChanged;

        UpdateSpeed();
    }

    public override int Priority => EntityComponentBase.PriorityPreset.RelyOthers;

    public Vector2 MoveDirection => moveDirection;
    public bool IsMoving => !IsMovementLocked && moveDirection.sqrMagnitude > 0.0001f;
    public void MoveTo(Vector2 position)
    {
        if (IsMovementLocked) return;
        rb.velocity = (position - Owner.Center).normalized * Time.deltaTime * speed;
    }

    public void StopMoving()
    {
        rb.velocity = Vector2.zero;
    }

    public float Speed => speed;

    public override void OnDisableComponent()
    {
        GameEventBus.Unsubscribe<PlayerMoveInputChangedEvent>(OnMoveInputChanged);
        if (propertiesManager != null)
        {
            propertiesManager.OnAllPropertiesChanged -= UpdateSpeed;
            propertiesManager.OnPropertyChanged -= OnPropertyChanged;
        }
    }

    public override void OnFixedTick(float deltaTime)
    {
        Move(deltaTime);
    }

    private void Move(float deltaTime)
    {
        if (IsMovementLocked) return;
        rb.velocity = moveDirection.normalized * deltaTime * speed;
    }

    private void OnMoveInputChanged(PlayerMoveInputChangedEvent eventData)
    {
        moveDirection = eventData.MoveDirection;
    }

    private void OnPropertyChanged(PropType propType, float newValue)
    {
        if (propType == PropType.MoveSpeed)
        {
            UpdateSpeed();
        }
    }

    private void UpdateSpeed()
    {
        speed = propertiesManager.GetPropValue(PropType.MoveSpeed);
    }

    public void EnableMovement()
    {
        movementLocks.Clear();
    }

    public void DisableMovement()
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
}
