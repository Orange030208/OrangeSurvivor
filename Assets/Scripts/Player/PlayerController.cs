using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Player))]
public class PlayerController : MonoBehaviour
{
    private Rigidbody2D _rb;
    [SerializeField] private float speed;

    private PropertiesManager propertiesManager;
    private Player player;
    private Vector2 moveDirection;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        propertiesManager = GetComponent<PropertiesManager>();
        player = GetComponent<Player>();
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
        player.ApplyMoveDirection(moveDirection);
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
}
