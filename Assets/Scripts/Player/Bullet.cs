using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    private Rigidbody2D _rb;
    private Collider2D _collider;
    protected float _moveSpeed = 5;
    protected float _damage;
    protected bool _isCritical;
    [SerializeField]protected LayerMask targetsLayerMask;

    private void Awake()
    {
        _collider =  GetComponent<Collider2D>();
        _rb = GetComponent<Rigidbody2D>();
    }

    public void Shoot(Vector2 direction, float damage,bool isCritical)
    {
        _damage = damage;
        _isCritical = isCritical;
        transform.right = direction;
        _rb.velocity = direction * _moveSpeed;
    }

    public void Configure()
    {
        
    }

    protected virtual void OnTrigger(Collider2D collider)
    {
        //TODO:后续修改，子弹不可以同时命中多个目标
        if (IsInLayerMask(collider.gameObject.layer, targetsLayerMask))
        {
            Attack(collider.GetComponent<Enemy>());
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        OnTrigger(collider);
    }

    private void Attack(Enemy enemy)
    {
        enemy.TakeDamage(new DamageInfo(_damage,enemy.transform.position,_isCritical));
    }

    private bool IsInLayerMask(int layer, LayerMask layerMask)
    {
        return (layerMask.value & (1 << layer)) != 0;
    }
}
