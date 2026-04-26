using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : EntityComponentBase, IMovable
{
    private float speed = 0;
    private bool moveDisabled = false;

    private Rigidbody2D rb;
    private Player owner;
    private PropertiesManager propertiesManager;
    private Vector2 moveDirection;
    public override Entity Owner => owner;
    public PropertiesManager PropertiesManager => propertiesManager;
    public override void Initialize(Entity owner)
    {
        this.owner = owner as Player;
        this.rb = this.owner.Rb;
        this.propertiesManager = this.owner.PropertiesManager;

        GameEventBus.Subscribe<PlayerMoveInputChangedEvent>(OnMoveInputChanged);
        propertiesManager.OnAllPropertiesChanged += UpdateSpeed;
        propertiesManager.OnPropertyChanged += OnPropertyChanged;

        UpdateSpeed();
    }

    public override int Priority => EntityComponentBase.PriorityPreset.RelyOthers;

    public Vector2 MoveDirection => moveDirection;
    public bool IsMoving => moveDirection.sqrMagnitude > 0.0001f;
    public void MoveTo(Vector2 position)
    {
        if (moveDisabled) return;
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
        if (!GameSimulation.IsRunning)
        {
            moveDirection = Vector2.zero;
            rb.velocity = Vector2.zero;
            return;
        }

        Move(deltaTime);
    }

    private void Move(float deltaTime)
    {
        if (moveDisabled) return;
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
        moveDisabled = true;
    }

    public void DisableMovement()
    {
        moveDisabled = false;
    }
}
