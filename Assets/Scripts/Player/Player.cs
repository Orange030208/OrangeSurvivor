using System;
using UnityEngine;

[RequireComponent(typeof(HealthComponent), typeof(PlayerLevel))]
public class Player : Entity
{
    [Header("组件")]
    private PlayerLevel playerLevel;
    [SerializeField] private new CircleCollider2D collider;

    public override Vector2 Center => (Vector2)transform.position + collider.offset;

    public bool IsLevelUpInCurrentWave => playerLevel.IsLevelUpInCurrentWave;

    public int LevelUpValue => playerLevel.LevelUpValue;

    private void Awake()
    {
        playerLevel = GetComponent<PlayerLevel>();
    }

    public int UseUpgradePoints() => playerLevel.UseUpgradePoints();
}
