using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : EntityComponentBase, IMovable, IMovementLockable, IPlayerMoveInputReceiver
{
    private float speed = 0;

    private Rigidbody2D rb;
    private Player owner;
    private AttributeManager attributeManager;
    private Vector2 moveDirection;
    private readonly HashSet<object> movementLocks = new();
    public override Entity Owner => owner;
    public AttributeManager AttributeManager => attributeManager;
    public bool IsMovementLocked => movementLocks.Count > 0;
    public override void Initialize(Entity owner)
    {
        this.owner = owner as Player;
        this.rb = this.owner.Rb;
        attributeManager = this.owner.AttributeManager;
        movementLocks.Clear();

        attributeManager.SubscribeAttributeChanged(PropType.MoveSpeed, OnMoveSpeedChanged);

        UpdateSpeed();
    }

    public override int Priority => EntityComponentBase.PriorityPreset.RelyOthers;

    public Vector2 MoveDirection => moveDirection;
    public bool IsMoving => !IsMovementLocked && moveDirection.sqrMagnitude > 0.0001f;
    public void MoveTo(Vector2 position)
    {
        if (IsMovementLocked) return;
        rb.velocity = (position - Owner.Center).normalized * speed;
    }

    public void StopMoving()
    {
        rb.velocity = Vector2.zero;
    }

    public float Speed => speed;

    public override void OnDisableComponent()
    {
        if (attributeManager != null)
        {
            attributeManager.UnsubscribeAttributeChanged(PropType.MoveSpeed, OnMoveSpeedChanged);
        }
    }

    public override void OnFixedTick(float deltaTime)
    {
        Move(deltaTime);
    }

    private void Move(float deltaTime)
    {
        if (IsMovementLocked) return;
        rb.velocity = moveDirection.normalized * speed;
    }

    public void SetMoveInput(Vector2 moveDirection)
    {
        this.moveDirection = moveDirection;
    }

    private void OnMoveSpeedChanged(int newValue)
    {
        UpdateSpeed();
    }

    private void UpdateSpeed()
    {
        speed = PropValueUtility.DistancePointsToWorldUnits(attributeManager.GetAttributeValue(PropType.MoveSpeed));
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
