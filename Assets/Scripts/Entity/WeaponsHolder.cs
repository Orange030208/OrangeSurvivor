using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
/// <summary>
/// 玩家武器容器：
/// - 管理所有武器槽位；
/// - 负责添加/移除/合并武器；
/// - 对外提供当前已装备武器快照。
/// UI 或业务层通常不直接操作 WeaponPosition，而是通过 WeaponsHolder 完成装备变更。
/// </summary>
public class WeaponsHolder : EntityComponentBase
{
    private const float DEFAULT_WEAPON_SLOT_RADIUS = 1.5f;
    private const float CIRCLE_START_ANGLE_DEGREES = 90f;
    private const float FULL_CIRCLE_DEGREES = 360f;

    [Header("检视面板")]
    [Tooltip("玩家身上的武器槽位列表。每个槽位对应一个武器位置组件。")]
    [SerializeField] private WeaponPosition[] weaponPositions;
    [Tooltip("武器槽位围绕实体中心点排布的半径。")]
    [SerializeField, Min(0f)] private float weaponSlotRadius = DEFAULT_WEAPON_SLOT_RADIUS;
    [Tooltip("槽位父节点。留空时会复用现有槽位的父节点，仍为空则使用当前对象。")]
    [SerializeField] private Transform weaponSlotsRoot;
    [Tooltip("由武器容器显式决定这些武器默认攻击哪些层。")]
    [SerializeField] private LayerMask targetLayerMask;

    private readonly List<Weapon> equippedWeapons = new();
    private readonly List<WeaponPosition> weaponPositionPool = new();
    private readonly Dictionary<string, WeaponBenefitData> weaponBenefitModifierSources = new();
    private WeaponBenefitData currentWeaponBenefitBonus = WeaponBenefitData.Zero;
    private bool runtimeWeaponsDisabled;

    public event Action OnWeaponBenefitBonusChanged;
    public event Action OnWeaponsChanged;
    public IReadOnlyList<Weapon> EquippedWeapons => equippedWeapons.AsReadOnly();
    public int WeaponSlotCount => weaponPositions?.Length ?? 0;
    public WeaponBenefitData CurrentWeaponBenefitBonus => currentWeaponBenefitBonus;

    private Entity owner;
    private PropertiesManager propertiesManager;

    public override Entity Owner => owner;
    public override int Priority => EntityComponentBase.PriorityPreset.RelyOthers;

    public override void Initialize(Entity owner)
    {
        if (owner == null)
        {
            throw new ArgumentNullException(nameof(owner));
        }

        UnsubscribeFromPropertiesManager();
        this.owner = owner;
        propertiesManager = owner.GetComponent<PropertiesManager>();

        BuildWeaponPositionPool();
        ResizeWeaponSlotsFromProperties();
        RebuildEquippedWeaponsCache();
        AddInitialWeapons();
        SubscribeToPropertiesManager();
    }

    private void AddInitialWeapons()
    {
        if (!owner.TryGetComponent(out IInitialWeaponProvider initialWeaponProvider))
        {
            return;
        }

        IReadOnlyList<WeaponEntry> initialWeapons = initialWeaponProvider.InitialWeapons;
        if (initialWeapons == null)
        {
            return;
        }

        for (int i = 0; i < initialWeapons.Count; i++)
        {
            WeaponEntry weapon = initialWeapons[i];
            AddWeapon(weapon.weaponData, weapon.level, false);
        }
    }

    public override void OnTick(float deltaTime)
    {
        if (runtimeWeaponsDisabled || weaponPositions == null)
        {
            return;
        }

        for (int i = 0; i < weaponPositions.Length; i++)
        {
            weaponPositions[i]?.Weapon?.OnTick(deltaTime);
        }
    }

    public override void OnDisableComponent()
    {
        UnsubscribeFromPropertiesManager();
        ClearWeaponBenefitModifiers();
        DisableRuntimeWeapons();
        runtimeWeaponsDisabled = false;
    }

    public void DisableWeaponsForWaveCleanup()
    {
        if (runtimeWeaponsDisabled)
        {
            return;
        }

        runtimeWeaponsDisabled = true;
        StopRuntimeWeaponsForWaveCleanup();
    }

    public void EnableWeaponsAfterWaveCleanup()
    {
        if (!runtimeWeaponsDisabled)
        {
            return;
        }

        runtimeWeaponsDisabled = false;
    }

    private void DisableRuntimeWeapons()
    {
        if (weaponPositions == null)
        {
            return;
        }

        for (int i = 0; i < weaponPositions.Length; i++)
        {
            weaponPositions[i]?.Weapon?.OnDisableComponent();
        }
    }

    private void StopRuntimeWeaponsForWaveCleanup()
    {
        if (weaponPositions == null)
        {
            return;
        }

        for (int i = 0; i < weaponPositions.Length; i++)
        {
            weaponPositions[i]?.Weapon?.StopForWaveCleanup();
        }
    }

    /// <summary>
    /// 往第一个空槽位添加一把武器。
    /// 运行时可变配置如 targetLayerMask 必须在武器实例化后立即下发，
    /// 不应依赖武器在 Awake 阶段缓存旧值。
    /// </summary>
    public bool AddWeapon(WeaponDataSO weaponData, int level, bool playSfx = true)
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

        Weapon runtimeWeapon = emptyPosition.AssignWeapon(
            owner,
            weaponData,
            WeaponLevelHelper.ClampLevel(level),
            currentWeaponBenefitBonus);

        if (runtimeWeapon == null)
        {
            return false;
        }

        runtimeWeapon.SetTargetLayerMask(targetLayerMask);

        RebuildEquippedWeaponsCache();
        OnWeaponsChanged?.Invoke();
        if (playSfx)
        {
            AudioSfxBridge.RequestPlay(AudioSfxKey.ItemEquipped);
        }
        return true;
    }

    public void AddWeaponBenefitModifier(string sourceId, WeaponBenefitData modifier)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
        {
            Debug.LogWarning("[WeaponsHolder] AddWeaponBenefitModifier: sourceId is null or empty.");
            return;
        }

        weaponBenefitModifierSources[sourceId] = modifier.Validated();
        RecalculateWeaponBenefitBonus();
    }

    private void ClearWeaponBenefitModifiers()
    {
        if (weaponBenefitModifierSources.Count == 0)
        {
            return;
        }

        weaponBenefitModifierSources.Clear();
        RecalculateWeaponBenefitBonus();
    }

    public void RemoveWeaponBenefitModifier(string sourceId)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
        {
            return;
        }

        if (!weaponBenefitModifierSources.Remove(sourceId))
        {
            return;
        }

        RecalculateWeaponBenefitBonus();
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
            sourcePosition.AssignWeapon(owner, weaponData, sourceWeapon.Level, currentWeaponBenefitBonus);
            if (sourcePosition.Weapon != null)
            {
                sourcePosition.Weapon.SetTargetLayerMask(targetLayerMask);
            }
            RebuildEquippedWeaponsCache();
            OnWeaponsChanged?.Invoke();
            return false;
        }

        Weapon mergedWeapon = targetPosition.AssignWeapon(owner, weaponData, mergedLevel, currentWeaponBenefitBonus);
        if (mergedWeapon != null)
        {
            mergedWeapon.SetTargetLayerMask(targetLayerMask);
        }

        RebuildEquippedWeaponsCache();
        OnWeaponsChanged?.Invoke();
        return true;
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

    private void BuildWeaponPositionPool()
    {
        weaponPositionPool.Clear();

        if (weaponPositions != null)
        {
            for (int i = 0; i < weaponPositions.Length; i++)
            {
                AddWeaponPositionToPool(weaponPositions[i]);
            }
        }

        WeaponPosition[] childPositions = GetComponentsInChildren<WeaponPosition>(true);
        for (int i = 0; i < childPositions.Length; i++)
        {
            AddWeaponPositionToPool(childPositions[i]);
        }
    }

    private void AddWeaponPositionToPool(WeaponPosition weaponPosition)
    {
        if (weaponPosition == null || weaponPositionPool.Contains(weaponPosition))
        {
            return;
        }

        weaponPositionPool.Add(weaponPosition);
    }

    private void ResizeWeaponSlotsFromProperties()
    {
        int requestedSlotCount = ResolveWeaponSlotCount();
        int minimumSlotCount = GetMinimumSlotCountForEquippedWeapons();
        int resolvedSlotCount = Mathf.Max(requestedSlotCount, minimumSlotCount);

        if (resolvedSlotCount > requestedSlotCount)
        {
            Debug.LogWarning(
                $"[{nameof(WeaponsHolder)}] {owner.name} requested {requestedSlotCount} weapon slots, " +
                $"but {minimumSlotCount} occupied slots must be kept.");
        }

        EnsureWeaponPositionPool(resolvedSlotCount);
        weaponPositions = new WeaponPosition[resolvedSlotCount];
        for (int i = 0; i < resolvedSlotCount; i++)
        {
            weaponPositions[i] = weaponPositionPool[i];
        }

        SetWeaponPositionActiveStates(resolvedSlotCount);
        ApplyWeaponSlotLayout();
    }

    private int ResolveWeaponSlotCount()
    {
        float rawSlotCount = propertiesManager.GetPropValue(PropType.WeaponSlotCount);

        return PropValueUtility.FloatPointsToNonNegativeRoundedInt(rawSlotCount);
    }

    private int GetMinimumSlotCountForEquippedWeapons()
    {
        if (weaponPositions == null)
        {
            return 0;
        }

        int minimumSlotCount = 0;
        for (int i = 0; i < weaponPositions.Length; i++)
        {
            if (weaponPositions[i]?.Weapon != null)
            {
                minimumSlotCount = i + 1;
            }
        }

        return minimumSlotCount;
    }

    private void EnsureWeaponPositionPool(int slotCount)
    {
        Transform slotsRoot = GetWeaponSlotsRoot();
        for (int i = 0; i < slotCount; i++)
        {
            if (i < weaponPositionPool.Count && weaponPositionPool[i] != null)
            {
                continue;
            }

            WeaponPosition createdPosition = CreateWeaponPosition(slotsRoot, i);
            if (i < weaponPositionPool.Count)
            {
                weaponPositionPool[i] = createdPosition;
                continue;
            }

            weaponPositionPool.Add(createdPosition);
        }
    }

    private Transform GetWeaponSlotsRoot()
    {
        if (weaponSlotsRoot != null)
        {
            return weaponSlotsRoot;
        }

        for (int i = 0; i < weaponPositionPool.Count; i++)
        {
            WeaponPosition weaponPosition = weaponPositionPool[i];
            if (weaponPosition != null && weaponPosition.transform.parent != null)
            {
                return weaponPosition.transform.parent;
            }
        }

        return transform;
    }

    private WeaponPosition CreateWeaponPosition(Transform parent, int index)
    {
        GameObject slotObject = new GameObject($"Weapon Position {index + 1}");
        slotObject.layer = parent != null ? parent.gameObject.layer : gameObject.layer;
        slotObject.transform.SetParent(parent, false);
        return slotObject.AddComponent<WeaponPosition>();
    }

    private void SetWeaponPositionActiveStates(int activeSlotCount)
    {
        for (int i = 0; i < weaponPositionPool.Count; i++)
        {
            WeaponPosition weaponPosition = weaponPositionPool[i];
            if (weaponPosition == null)
            {
                continue;
            }

            bool shouldBeActive = i < activeSlotCount;
            if (weaponPosition.gameObject.activeSelf != shouldBeActive)
            {
                weaponPosition.gameObject.SetActive(shouldBeActive);
            }
        }
    }

    private void ApplyWeaponSlotLayout()
    {
        if (owner == null || weaponPositions == null)
        {
            return;
        }

        Transform slotsRoot = GetWeaponSlotsRoot();
        for (int i = 0; i < weaponPositions.Length; i++)
        {
            WeaponPosition weaponPosition = weaponPositions[i];
            if (weaponPosition == null)
            {
                continue;
            }

            Transform slotTransform = weaponPosition.transform;
            if (slotTransform.parent != slotsRoot)
            {
                slotTransform.SetParent(slotsRoot, true);
            }

            Vector2 slotOffset = CalculateWeaponSlotOffset(i, weaponPositions.Length);
            Vector2 slotWorldPosition = owner.Center + slotOffset;
            Vector3 localPosition = slotsRoot.InverseTransformPoint(
                new Vector3(slotWorldPosition.x, slotWorldPosition.y, slotsRoot.position.z));

            localPosition.z = 0f;
            slotTransform.localPosition = localPosition;
            slotTransform.localRotation = Quaternion.identity;
        }
    }

    private Vector2 CalculateWeaponSlotOffset(int slotIndex, int slotCount)
    {
        if (slotIndex == 0)
        {
            return Vector2.left * weaponSlotRadius;
        }

        if (slotIndex == 1)
        {
            return Vector2.right * weaponSlotRadius;
        }

        int circleSlotCount = slotCount - 2;
        if (circleSlotCount <= 1)
        {
            return Vector2.up * weaponSlotRadius;
        }

        // 偶数四分布局会直接撞上左右手槽位，轻微错开能保持环形但避免重叠。
        float startAngle = CIRCLE_START_ANGLE_DEGREES;
        if (circleSlotCount % 4 == 0)
        {
            startAngle += 180f / circleSlotCount;
        }

        float angle = startAngle + FULL_CIRCLE_DEGREES * (slotIndex - 2) / circleSlotCount;
        float radians = angle * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * weaponSlotRadius;
    }

    private void SubscribeToPropertiesManager()
    {
        if (propertiesManager == null)
        {
            return;
        }

        propertiesManager.OnPropertyChanged -= OnPropertyChanged;
        propertiesManager.OnPropertyChanged += OnPropertyChanged;
    }

    private void UnsubscribeFromPropertiesManager()
    {
        if (propertiesManager == null)
        {
            return;
        }

        propertiesManager.OnPropertyChanged -= OnPropertyChanged;
    }

    private void OnPropertyChanged(PropType propType, float newValue)
    {
        if (propType != PropType.WeaponSlotCount)
        {
            return;
        }

        int previousSlotCount = WeaponSlotCount;
        ResizeWeaponSlotsFromProperties();

        if (previousSlotCount == WeaponSlotCount)
        {
            return;
        }

        RebuildEquippedWeaponsCache();
        OnWeaponsChanged?.Invoke();
    }

    private void RecalculateWeaponBenefitBonus()
    {
        WeaponBenefitData previousBonus = currentWeaponBenefitBonus;
        WeaponBenefitData nextBonus = WeaponBenefitData.Zero;
        foreach (WeaponBenefitData modifier in weaponBenefitModifierSources.Values)
        {
            nextBonus += modifier;
        }

        currentWeaponBenefitBonus = nextBonus;
        if (AreSameWeaponBenefits(previousBonus, currentWeaponBenefitBonus))
        {
            return;
        }

        RefreshEquippedWeaponRuntimeStats();
        OnWeaponBenefitBonusChanged?.Invoke();
    }

    private void RefreshEquippedWeaponRuntimeStats()
    {
        if (weaponPositions == null)
        {
            return;
        }

        for (int i = 0; i < weaponPositions.Length; i++)
        {
            Weapon weapon = weaponPositions[i]?.Weapon;
            if (weapon != null)
            {
                weapon.SetBenefits(currentWeaponBenefitBonus);
            }
        }
    }

    private static bool AreSameWeaponBenefits(WeaponBenefitData left, WeaponBenefitData right)
    {
        return Mathf.Approximately(left.AttackSpeedBenefitPercent, right.AttackSpeedBenefitPercent) &&
               Mathf.Approximately(left.CriticalChanceBenefitPercent, right.CriticalChanceBenefitPercent) &&
               Mathf.Approximately(left.CriticalPercentBenefitPercent, right.CriticalPercentBenefitPercent) &&
               Mathf.Approximately(left.RangeBenefitPercent, right.RangeBenefitPercent) &&
               Mathf.Approximately(left.KnockbackStrengthBenefitPercent, right.KnockbackStrengthBenefitPercent) &&
               Mathf.Approximately(left.MeleeAttackUsagePercent, right.MeleeAttackUsagePercent) &&
               Mathf.Approximately(left.RangedAttackUsagePercent, right.RangedAttackUsagePercent) &&
               Mathf.Approximately(left.MagicAttackUsagePercent, right.MagicAttackUsagePercent) &&
               Mathf.Approximately(left.SummonAttackUsagePercent, right.SummonAttackUsagePercent);
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

            equippedWeapons.Add(weapon);
        }
    }
}
