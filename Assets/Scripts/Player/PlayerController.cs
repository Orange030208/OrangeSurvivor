using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    private Rigidbody2D _rb;
    [SerializeField] private float speed;
    [SerializeField] private MobileJoystick playerJoystick;

    private PropertiesManager propertiesManager;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        propertiesManager = GetComponent<PropertiesManager>();
    }

    private void OnEnable()
    {
        if (propertiesManager != null)
        {
            propertiesManager.OnAllPropertiesChanged += UpdateSpeed;
            propertiesManager.OnPropertyChanged += OnPropertyChanged;
        }
    }

    private void OnDisable()
    {
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
        Move();
    }

    private void Move()
    {
        Vector2 moveDirection = playerJoystick.GetMoveVector();
        _rb.velocity = moveDirection * Time.deltaTime * speed;
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
