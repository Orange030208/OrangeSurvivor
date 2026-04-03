using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour,IPlayerStatusDependency
{
    private Rigidbody2D _rb;
    [SerializeField] private float baseSpeed;
    [SerializeField] private float speed;
    [SerializeField] private MobileJoystick playerJoystick;

    private void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
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

    public void UpdateStatus(PropertiesManager propertiesManager)
    {
        speed = baseSpeed + propertiesManager.GetPropValue(PropType.MoveSpeed);
    }
}