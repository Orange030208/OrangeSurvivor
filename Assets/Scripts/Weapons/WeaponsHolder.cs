using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家武器容器：
/// - 管理所有武器槽位；
/// - 负责添加/移除/合并武器；
/// - 对外提供当前已装备武器快照。
/// UI 或业务层通常不直接操作 WeaponPosition，而是通过 WeaponsHolder 完成装备变更。
/// </summary>
public class WeaponsHolder : MonoBehaviour
{
    [Header("Inspector")]
    [Tooltip("玩家身上的武器槽位列表。每个槽位对应一个 WeaponPosition。")]
    [SerializeField] private WeaponPosition[] weaponPositions;
    [Tooltip("由武器容器显式决定这些武器默认攻击哪些层。")]
    [SerializeField] private LayerMask targetLayerMask;

    private readonly List<EquippedWeaponInfo> equippedWeapons = new();

    public event Action OnWeaponsChanged;
    public IReadOnlyList<EquippedWeaponInfo> EquippedWeapons => equippedWeapons.AsReadOnly();

    private void Awake()
    {
        RebuildEquippedWeaponsCache();
    }

    /// <summary>
    /// 往第一个空槽位添加一把武器。
    /// 运行时可变配置如 targetLayerMask 必须在武器实例化后立即下发，
    /// 不应依赖武器在 Awake 阶段缓存旧值。
    /// </summary>
    public bool AddWeapon(WeaponDataSO weaponData, int level)
    {
        if (weaponData == null)
        {
            return false;
        }

        WeaponPosition emptyPosition = GetEmptyWeaponPosition();
        if (emptyPosition == null)
        {
            Debug.LogWarning("No empty weapon position available.");
            return false;
        }

        Weapon runtimeWeapon = emptyPosition.AssignWeapon(weaponData, WeaponLevelHelper.ClampLevel(level));
        if (runtimeWeapon == null)
        {
            return false;
        }

        runtimeWeapon.SetTargetLayerMask(targetLayerMask);

        RebuildEquippedWeaponsCache();
        OnWeaponsChanged?.Invoke();
        return true;
    }

    public bool RemoveWeapon(Weapon weapon)
    {
        if (weapon == null || weaponPositions == null)
        {
            return false;
        }

        bool removed = false;
        for (int i = 0; i < weaponPositions.Length; i++)
        {
            if (weaponPositions[i] != null && weaponPositions[i].RemoveWeapon(weapon))
            {
                removed = true;
                break;
            }
        }

        if (!removed)
        {
            return false;
        }

        RebuildEquippedWeaponsCache();
        OnWeaponsChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 合并两把同类型同等级武器，生成更高一级的新武器。
    /// 当前实现采用“先移除两把，再在目标槽位生成合并后武器”的方式。
    /// </summary>
    public bool MergeWeapon(Weapon sourceWeapon, Weapon targetWeapon)
    {
        if (sourceWeapon == null || targetWeapon == null || sourceWeapon == targetWeapon)
        {
            return false;
        }

        if (sourceWeapon.WeaponData != targetWeapon.WeaponData || sourceWeapon.Level != targetWeapon.Level)
        {
            return false;
        }

        if (!WeaponLevelHelper.TryGetMergedLevel(sourceWeapon.Level, out int mergedLevel))
        {
            return false;
        }

        WeaponPosition sourcePosition = FindWeaponPosition(sourceWeapon);
        WeaponPosition targetPosition = FindWeaponPosition(targetWeapon);
        if (sourcePosition == null || targetPosition == null)
        {
            return false;
        }

        WeaponDataSO weaponData = sourceWeapon.WeaponData;

        if (!sourcePosition.RemoveWeapon(sourceWeapon))
        {
            return false;
        }

        if (!targetPosition.RemoveWeapon(targetWeapon))
        {
            sourcePosition.AssignWeapon(weaponData, sourceWeapon.Level);
            if (sourcePosition.Weapon != null)
            {
                sourcePosition.Weapon.SetTargetLayerMask(targetLayerMask);
            }
            RebuildEquippedWeaponsCache();
            OnWeaponsChanged?.Invoke();
            return false;
        }

        Weapon mergedWeapon = targetPosition.AssignWeapon(weaponData, mergedLevel);
        if (mergedWeapon != null)
        {
            mergedWeapon.SetTargetLayerMask(targetLayerMask);
        }

        RebuildEquippedWeaponsCache();
        OnWeaponsChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 强制刷新一次对外快照。
    /// 常用于外部数据和实际武器实例发生了重新同步后，通知 UI 更新。
    /// </summary>
    public void RefreshSnapshot()
    {
        RebuildEquippedWeaponsCache();
        OnWeaponsChanged?.Invoke();
    }

    private WeaponPosition GetEmptyWeaponPosition()
    {
        if (weaponPositions == null)
        {
            return null;
        }

        for (int i = 0; i < weaponPositions.Length; i++)
        {
            if (weaponPositions[i] != null && weaponPositions[i].Weapon == null)
            {
                return weaponPositions[i];
            }
        }

        return null;
    }

    private WeaponPosition FindWeaponPosition(Weapon weapon)
    {
        if (weaponPositions == null || weapon == null)
        {
            return null;
        }

        for (int i = 0; i < weaponPositions.Length; i++)
        {
            if (weaponPositions[i] != null && weaponPositions[i].Weapon == weapon)
            {
                return weaponPositions[i];
            }
        }

        return null;
    }

    private void RebuildEquippedWeaponsCache()
    {
        equippedWeapons.Clear();

        if (weaponPositions == null)
        {
            return;
        }

        foreach (WeaponPosition weaponPosition in weaponPositions)
        {
            if (weaponPosition == null || weaponPosition.Weapon == null)
            {
                continue;
            }

            Weapon weapon = weaponPosition.Weapon;
            if (weapon.WeaponData == null)
            {
                continue;
            }

            equippedWeapons.Add(new EquippedWeaponInfo(weapon.WeaponData, weapon.Level, weapon));
        }
    }
}

/// <summary>
/// 已装备武器快照。
/// UI、背包面板或调试面板可以读取它，而不直接依赖具体的 WeaponPosition。
/// </summary>
public readonly struct EquippedWeaponInfo
{
    public WeaponDataSO WeaponData { get; }
    public int Level { get; }
    public Weapon RuntimeWeapon { get; }

    public EquippedWeaponInfo(WeaponDataSO weaponData, int level, Weapon runtimeWeapon)
    {
        WeaponData = weaponData;
        Level = level;
        RuntimeWeapon = runtimeWeapon;
    }
}
