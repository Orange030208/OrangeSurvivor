using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour,IMovement
{
    private Rigidbody2D _rb;
    [SerializeField] private float speed;

    private bool moveDisabled = false;

    private PropertiesManager propertiesManager;
    private Vector2 moveDirection;

    public Vector2 MoveDirection => moveDirection;
    public bool IsMoving => moveDirection.sqrMagnitude > 0.0001f;
    public float Speed => speed;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        propertiesManager = GetComponent<PropertiesManager>();
    }

    private void OnEnable()
    {
        GameEventBus.Subscribe<PlayerMoveInputChangedEvent>(OnMoveInputChanged);
        if (propertiesManager != null)
        {
            propertiesManager.OnAllPropertiesChanged += UpdateSpeed;
            propertiesManager.OnPropertyChanged += OnPropertyChanged;
        }
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<PlayerMoveInputChangedEvent>(OnMoveInputChanged);
        if (propertiesManager != null)
        {
            propertiesManager.OnAllPropertiesChanged -= UpdateSpeed;
            propertiesManager.OnPropertyChanged -= OnPropertyChanged;
        }
    }

    private void Start()
    {
        UpdateSpeed();
    }

    private void FixedUpdate()
    {
        if (!GameSimulation.IsRunning)
        {
            moveDirection = Vector2.zero;
            _rb.velocity = Vector2.zero;
            return;
        }

        Move();
    }

    private void Move()
    {
        if (moveDisabled) return;
        _rb.velocity = moveDirection * Time.deltaTime * speed;
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
        if (propertiesManager == null) return;
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
