using System;
using UnityEngine;

[RequireComponent(typeof(HealthComponent), typeof(PlayerLevel))]
public class Player : Entity
{
    [Header("组件")]
    private PlayerLevel playerLevel;
    [SerializeField] private new CircleCollider2D collider;

    private Vector2 currentFacingDirection = Vector2.up;
    private bool isMoving;

    public override Vector2 Center => (Vector2)transform.position + collider.offset;
    public override bool IsMoving => isMoving;
    public override Vector2 CurrentFacingDirection => currentFacingDirection;

    public bool IsLevelUpInCurrentWave => playerLevel.IsLevelUpInCurrentWave;

    public int LevelUpValue => playerLevel.LevelUpValue;

    private void Awake()
    {
        playerLevel = GetComponent<PlayerLevel>();
    }

    public void ApplyMoveDirection(Vector2 direction)
    {
        isMoving = direction.sqrMagnitude > 0.0001f;
        if (isMoving)
        {
            currentFacingDirection = direction.normalized;
        }
    }

    public int UseUpgradePoints() => playerLevel.UseUpgradePoints();
}
