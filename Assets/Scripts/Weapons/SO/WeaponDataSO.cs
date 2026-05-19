using System;
using System.Collections.Generic;
using UnityEngine;

public enum WeaponTag
{
    Precision = 0,
    Fast = 1,
    Heavy = 2,
    Growth = 3
}

public enum WeaponAttackTimingMode
{
    CompressedIntoAttackInterval = 0,
    FixedSequenceThenCooldown = 1
}

public enum WeaponTargetingMode
{
    DynamicClosest = 0,
    StableLock = 1
}

public readonly struct WeaponSpawnPointPose
{
    public WeaponSpawnPointPose(Vector3 position, Quaternion rotation)
    {
        Position = position;
        Rotation = rotation;
    }

    public Vector3 Position { get; }
    public Quaternion Rotation { get; }
    public Vector3 Forward => Rotation * Vector3.up;
}

[Serializable]
public struct WeaponSpawnPointDefinition
{
    [Tooltip("点位名称，仅用于配置和调试识别，例如 muzzle、left_muzzle、slash_vfx。")]
    [SerializeField] private string id;
    [Tooltip("相对武器根节点的本地位置。")]
    [SerializeField] private Vector2 localPosition;
    [Tooltip("相对武器根节点的 Z 轴角度偏移，用于调试绘制、特效朝向或其他基于点位的表现。")]
    [SerializeField] private float localRotationOffset;

    public string Id => id;
    public Vector2 LocalPosition => localPosition;
    public float LocalRotationOffset => localRotationOffset;
}

[System.Serializable]
public struct WeaponSequenceProjectileDefinition
{
    [Tooltip("使用哪个武器点位。映射到武器数据的生成点位列表；未配置对应点位时使用武器根节点。")]
    [SerializeField] private int spawnPointIndex;
    [Tooltip("直接引用要发射的弹射物定义资源。定义内部持有最终要实例化的弹射物预制体。")]
    [SerializeField] private ProjectileDefinitionSO projectileDefinition;
    [Tooltip("连发分组标识。用于避免同一连发组重复启动。")]
    [SerializeField] private int burstId;
    [Tooltip("本次发射所使用的模式。")]
    [SerializeField] private ProjectileFiringMode firingMode;
    [Tooltip("多弹模式参数。")]
    [SerializeField] private ProjectilePatternConfig patternConfig;

    public int SpawnPointIndex => Mathf.Max(0, spawnPointIndex);
    public ProjectileDefinitionSO ProjectileDefinition => projectileDefinition;
    public int BurstId => Mathf.Max(0, burstId);
    public ProjectileFiringMode FiringMode => firingMode;
    public ProjectilePatternConfig PatternConfig => patternConfig;
}

[System.Serializable]
public struct WeaponSequenceSfxDefinition
{
    [Tooltip("该事件触发时要播放的语义音效。")]
    [SerializeField] private AudioSfxKey sfxKey;

    public AudioSfxKey SfxKey => sfxKey;
}

[System.Serializable]
public struct WeaponSequenceVfxDefinition
{
    [Tooltip("该事件触发时要生成的特效预制体。")]
    [SerializeField] private GameObject vfxPrefab;
    [Tooltip("生成锚点索引。映射到武器数据的生成点位列表；未配置对应点位时使用武器根节点。")]
    [SerializeField] private int spawnPointIndex;
    [Tooltip("相对锚点的局部偏移。")]
    [SerializeField] private Vector3 localOffset;
    [Tooltip("相对锚点的局部旋转补偿。")]
    [SerializeField] private Vector3 localEulerAngles;

    public GameObject VfxPrefab => vfxPrefab;
    public int SpawnPointIndex => Mathf.Max(0, spawnPointIndex);
    public Vector3 LocalOffset => localOffset;
    public Vector3 LocalEulerAngles => localEulerAngles;
}

[Serializable]
public struct WeaponLevelStatData
{
    [SerializeField, Min(1)] private int level;
    [SerializeField, Min(0f)] private float attack;
    [SerializeField, Min(PropValueUtility.MIN_EFFECTIVE_ATTACK_SPEED_POINTS)] private float attackSpeed;
    [SerializeField, Range(0f, 100f)] private float criticalChance;
    [SerializeField, Min(100f)] private float criticalPercent;
    [SerializeField, Min(0f)] private float range;
    [SerializeField, Min(0f)] private float knockbackStrength;
    [Header("属性收益率")]
    [Tooltip("本等级对外部属性的收益率增量。运行时会与武器实例的总收益相加；四类攻击收益率同样按该增量叠加。")]
    [SerializeField] private WeaponBenefitData statBenefits;
    [Header("持有者属性")]
    [Tooltip("装备该武器且处于本等级时，临时添加到武器持有者身上的属性。")]
    [SerializeField] private List<PropModifierData> holderModifiers;

    public int Level => Mathf.Max(WeaponLevelHelper.MinLevel, level);
    public float Attack => Mathf.Max(0f, attack);
    public float AttackSpeed => PropValueUtility.ClampEffectiveAttackSpeedPoints(attackSpeed);
    public float CriticalChance => Mathf.Clamp(criticalChance, 0f, 100f);
    public float CriticalPercent => Mathf.Max(100f, criticalPercent);
    public float Range => Mathf.Max(0f, range);
    public float KnockbackStrength => Mathf.Max(0f, knockbackStrength);
    public WeaponBenefitData StatBenefits => statBenefits.Validated();
    public IReadOnlyList<PropModifierData> HolderModifiers => holderModifiers != null
        ? holderModifiers
        : Array.Empty<PropModifierData>();

    public WeaponLevelStatData(
        int level,
        float attack,
        float attackSpeed,
        float criticalChance,
        float criticalPercent,
        float range,
        float knockbackStrength,
        IReadOnlyList<PropModifierData> holderModifiers = null,
        WeaponBenefitData? statBenefits = null)
    {
        this.level = Mathf.Max(WeaponLevelHelper.MinLevel, level);
        this.attack = Mathf.Max(0f, attack);
        this.attackSpeed = PropValueUtility.ClampEffectiveAttackSpeedPoints(attackSpeed);
        this.criticalChance = Mathf.Clamp(criticalChance, 0f, 100f);
        this.criticalPercent = Mathf.Max(100f, criticalPercent);
        this.range = Mathf.Max(0f, range);
        this.knockbackStrength = Mathf.Max(0f, knockbackStrength);
        this.statBenefits = statBenefits.HasValue ? statBenefits.Value.Validated() : WeaponBenefitData.Full;
        this.holderModifiers = holderModifiers != null
            ? new List<PropModifierData>(holderModifiers)
            : new List<PropModifierData>();
    }

    public WeaponLevelStatData ValidatedForLevel(int expectedLevel)
    {
        return new WeaponLevelStatData(
            expectedLevel,
            Attack,
            AttackSpeed,
            CriticalChance,
            CriticalPercent,
            Range,
            KnockbackStrength,
            HolderModifiers,
            StatBenefits);
    }

}

[Serializable]
public struct WeaponBenefitData
{
    public static WeaponBenefitData Full => new(100f, 100f, 100f, 100f, 100f, 0f, 0f, 0f, 0f);
    public static WeaponBenefitData Zero => new(0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f);

    [Tooltip("攻速收益率，百分比点口径：100 表示完整吃到玩家攻速属性收益，50 表示只吃到一半。")]
    [SerializeField, Min(0f)] private float attackSpeedBenefitPercent;
    [Tooltip("暴击率收益率，百分比点口径：100 表示完整吃到玩家暴击率收益。")]
    [SerializeField, Min(0f)] private float criticalChanceBenefitPercent;
    [Tooltip("暴击伤害收益率，百分比点口径：100 表示完整吃到玩家暴击伤害收益。")]
    [SerializeField, Min(0f)] private float criticalPercentBenefitPercent;
    [Tooltip("攻击范围收益率，百分比点口径：100 表示完整吃到玩家攻击范围属性收益。")]
    [SerializeField, Min(0f)] private float rangeBenefitPercent;
    [Tooltip("击退收益率，百分比点口径：100 表示完整吃到玩家击退属性收益。")]
    [SerializeField, Min(0f)] private float knockbackStrengthBenefitPercent;
    [Tooltip("近战攻击收益率，百分比点口径：20 表示吃到玩家近战攻击属性的 20%。")]
    [SerializeField, Min(0f)] private float meleeAttackUsagePercent;
    [Tooltip("远程攻击收益率，百分比点口径：20 表示吃到玩家远程攻击属性的 20%。")]
    [SerializeField, Min(0f)] private float rangedAttackUsagePercent;
    [Tooltip("魔法攻击收益率，百分比点口径：20 表示吃到玩家魔法攻击属性的 20%。")]
    [SerializeField, Min(0f)] private float magicAttackUsagePercent;
    [Tooltip("召唤攻击收益率，百分比点口径：20 表示吃到玩家召唤攻击属性的 20%。")]
    [SerializeField, Min(0f)] private float summonAttackUsagePercent;

    public float AttackSpeedBenefitPercent => Mathf.Max(0f, attackSpeedBenefitPercent);
    public float CriticalChanceBenefitPercent => Mathf.Max(0f, criticalChanceBenefitPercent);
    public float CriticalPercentBenefitPercent => Mathf.Max(0f, criticalPercentBenefitPercent);
    public float RangeBenefitPercent => Mathf.Max(0f, rangeBenefitPercent);
    public float KnockbackStrengthBenefitPercent => Mathf.Max(0f, knockbackStrengthBenefitPercent);
    public float MeleeAttackUsagePercent => Mathf.Max(0f, meleeAttackUsagePercent);
    public float RangedAttackUsagePercent => Mathf.Max(0f, rangedAttackUsagePercent);
    public float MagicAttackUsagePercent => Mathf.Max(0f, magicAttackUsagePercent);
    public float SummonAttackUsagePercent => Mathf.Max(0f, summonAttackUsagePercent);
    public bool HasAnyUsage => MeleeAttackUsagePercent > 0f ||
                               RangedAttackUsagePercent > 0f ||
                               MagicAttackUsagePercent > 0f ||
                               SummonAttackUsagePercent > 0f;
    public bool HasAnyStatBenefit => AttackSpeedBenefitPercent > 0f ||
                                     CriticalChanceBenefitPercent > 0f ||
                                     CriticalPercentBenefitPercent > 0f ||
                                     RangeBenefitPercent > 0f ||
                                     KnockbackStrengthBenefitPercent > 0f;
    public bool HasAnyBenefit => HasAnyStatBenefit || HasAnyUsage;

    public WeaponBenefitData(
        float attackSpeedBenefitPercent,
        float criticalChanceBenefitPercent,
        float criticalPercentBenefitPercent,
        float rangeBenefitPercent,
        float knockbackStrengthBenefitPercent,
        float meleeAttackUsagePercent,
        float rangedAttackUsagePercent,
        float magicAttackUsagePercent,
        float summonAttackUsagePercent)
    {
        this.attackSpeedBenefitPercent = Mathf.Max(0f, attackSpeedBenefitPercent);
        this.criticalChanceBenefitPercent = Mathf.Max(0f, criticalChanceBenefitPercent);
        this.criticalPercentBenefitPercent = Mathf.Max(0f, criticalPercentBenefitPercent);
        this.rangeBenefitPercent = Mathf.Max(0f, rangeBenefitPercent);
        this.knockbackStrengthBenefitPercent = Mathf.Max(0f, knockbackStrengthBenefitPercent);
        this.meleeAttackUsagePercent = Mathf.Max(0f, meleeAttackUsagePercent);
        this.rangedAttackUsagePercent = Mathf.Max(0f, rangedAttackUsagePercent);
        this.magicAttackUsagePercent = Mathf.Max(0f, magicAttackUsagePercent);
        this.summonAttackUsagePercent = Mathf.Max(0f, summonAttackUsagePercent);
    }

    public WeaponBenefitData Validated()
    {
        return new WeaponBenefitData(
            AttackSpeedBenefitPercent,
            CriticalChanceBenefitPercent,
            CriticalPercentBenefitPercent,
            RangeBenefitPercent,
            KnockbackStrengthBenefitPercent,
            MeleeAttackUsagePercent,
            RangedAttackUsagePercent,
            MagicAttackUsagePercent,
            SummonAttackUsagePercent);
    }

    public WeaponBenefitData GetAttackUsageOnly()
    {
        return new WeaponBenefitData(
            0f,
            0f,
            0f,
            0f,
            0f,
            MeleeAttackUsagePercent,
            RangedAttackUsagePercent,
            MagicAttackUsagePercent,
            SummonAttackUsagePercent);
    }

    public WeaponBenefitData Add(WeaponBenefitData other)
    {
        return new WeaponBenefitData(
            AttackSpeedBenefitPercent + other.AttackSpeedBenefitPercent,
            CriticalChanceBenefitPercent + other.CriticalChanceBenefitPercent,
            CriticalPercentBenefitPercent + other.CriticalPercentBenefitPercent,
            RangeBenefitPercent + other.RangeBenefitPercent,
            KnockbackStrengthBenefitPercent + other.KnockbackStrengthBenefitPercent,
            MeleeAttackUsagePercent + other.MeleeAttackUsagePercent,
            RangedAttackUsagePercent + other.RangedAttackUsagePercent,
            MagicAttackUsagePercent + other.MagicAttackUsagePercent,
            SummonAttackUsagePercent + other.SummonAttackUsagePercent);
    }

    public static WeaponBenefitData operator +(WeaponBenefitData left, WeaponBenefitData right)
    {
        return left.Add(right);
    }

    public float ApplyToResolvedStat(PropType propType, float weaponBaseValue, float resolvedValue)
    {
        float externalContribution = resolvedValue - weaponBaseValue;
        return weaponBaseValue + ApplyToExternalValue(propType, externalContribution);
    }

    public float ApplyToExternalValue(PropType propType, float externalValue)
    {
        return externalValue * PropValueUtility.PercentPointsToRatio(GetBenefitPercent(propType));
    }

    public float GetBenefitPercent(PropType propType)
    {
        return propType switch
        {
            PropType.AttackSpeed => AttackSpeedBenefitPercent,
            PropType.CriticalChance => CriticalChanceBenefitPercent,
            PropType.CriticalPercent => CriticalPercentBenefitPercent,
            PropType.AttackRange => RangeBenefitPercent,
            PropType.KnockbackStrength => KnockbackStrengthBenefitPercent,
            _ => 100f
        };
    }
}

[CreateAssetMenu(fileName = "Weapon Data", menuName = ScriptableObjectMenuPaths.WEAPON_DATA, order = 0)]
public class WeaponDataSO : ItemDataSO, IDescribable
{
    [Header("标识")]
    [SerializeField] private string weaponId;

    [Header("分类")]
    [SerializeField] private WeaponTag[] tags = System.Array.Empty<WeaponTag>();

    [Header("运行时")]
    [SerializeField] private AttackSequenceDefinitionSO attackSequence;

    [Header("攻击表现")]
    [SerializeField] private float visualForwardAngle = 45f;
    [Tooltip("启用后，当武器冷却结束且已经对准当前目标时，会保持当前瞄准方向，不再继续跟随目标转动，直到本次攻击开始。一般推荐没有前摇的武器使用。")]
    [SerializeField] private bool holdAimWhenAttackReady = true;
    [Range(0.1f, 1f)]
    [SerializeField] private float attackSequenceOccupancy = 0.85f;
    [Tooltip("攻击序列和攻速冷却之间的关系。远程武器通常使用压缩模式；近战武器通常使用固定动画后冷却。")]
    [SerializeField] private WeaponAttackTimingMode attackTimingMode = WeaponAttackTimingMode.CompressedIntoAttackInterval;
    [Tooltip("自动索敌策略。远程武器通常使用动态最近目标；近战武器通常使用稳定锁定。")]
    [SerializeField] private WeaponTargetingMode targetingMode = WeaponTargetingMode.DynamicClosest;
    [SerializeField] private WeaponSpawnPointDefinition[] spawnPoints = System.Array.Empty<WeaponSpawnPointDefinition>();
    [SerializeField] private WeaponSequenceProjectileDefinition[] sequenceProjectileList;
    [SerializeField] private WeaponSequenceSfxDefinition[] sequenceSfxList;
    [SerializeField] private WeaponSequenceVfxDefinition[] sequenceVfxList;
    [Tooltip("启用后，攻击序列中的 OpenHitWindow / CloseHitWindow 事件才会产生碰撞盒检测。")]
    [SerializeField] private bool enableHitBox;
    [SerializeField] private GameObject hitVfxPrefab;
    [SerializeField] private Vector2 hitBoxSize = new(1f, 1f);
    [SerializeField] private Vector2 hitBoxOffset;

    [Header("属性等级表")]
    [SerializeField] private List<WeaponLevelStatData> levelStats = new();

    public string WeaponId => string.IsNullOrWhiteSpace(weaponId) ? name : weaponId;
    public IReadOnlyList<WeaponTag> Tags => tags;
    public IReadOnlyList<WeaponLevelStatData> LevelStats => levelStats;
    public AttackSequenceDefinitionSO AttackSequence => attackSequence;
    public float VisualForwardAngle => visualForwardAngle;
    public bool HoldAimWhenAttackReady => holdAimWhenAttackReady;
    public float AttackSequenceOccupancy => Mathf.Clamp(attackSequenceOccupancy, 0.1f, 1f);
    public WeaponAttackTimingMode AttackTimingMode => attackTimingMode;
    public WeaponTargetingMode TargetingMode => targetingMode;
    public IReadOnlyList<WeaponSpawnPointDefinition> SpawnPoints => spawnPoints;
    public IReadOnlyList<WeaponSequenceProjectileDefinition> SequenceProjectileList => sequenceProjectileList;
    public IReadOnlyList<WeaponSequenceSfxDefinition> SequenceSfxList => sequenceSfxList;
    public IReadOnlyList<WeaponSequenceVfxDefinition> SequenceVfxList => sequenceVfxList;
    public bool EnableHitBox => enableHitBox;
    public GameObject HitVfxPrefab => hitVfxPrefab;
    public Vector2 HitBoxSize => hitBoxSize;
    public Vector2 HitBoxOffset => hitBoxOffset;
    public override string Description => BuildDescriptionForLevel(WeaponLevelHelper.MinLevel);

    private void OnValidate()
    {
        itemType = ItemType.Weapon;
        NormalizeTags();
        spawnPoints ??= System.Array.Empty<WeaponSpawnPointDefinition>();
        EnsureLevelStatsTable();
        attackSequenceOccupancy = Mathf.Clamp(attackSequenceOccupancy, 0.1f, 1f);
        hitBoxSize.x = Mathf.Max(0.01f, hitBoxSize.x);
        hitBoxSize.y = Mathf.Max(0.01f, hitBoxSize.y);
    }

    public bool HasTag(WeaponTag tag)
    {
        if (tags == null)
        {
            return false;
        }

        for (int i = 0; i < tags.Length; i++)
        {
            if (tags[i] == tag)
            {
                return true;
            }
        }

        return false;
    }

    public WeaponLevelStatData GetLevelStats(int level)
    {
        int clampedLevel = WeaponLevelHelper.ClampLevel(level);
        if (levelStats == null || levelStats.Count == 0)
        {
            throw new InvalidOperationException(
                $"{nameof(WeaponDataSO)} '{name}' requires a configured {nameof(levelStats)} table.");
        }

        int index = clampedLevel - WeaponLevelHelper.MinLevel;
        if (index < 0 || index >= levelStats.Count)
        {
            throw new InvalidOperationException(
                $"{nameof(WeaponDataSO)} '{name}' is missing stats for weapon level {clampedLevel}.");
        }

        return levelStats[index].ValidatedForLevel(clampedLevel);
    }

    public bool TryGetSequenceProjectile(int eventKey, out WeaponSequenceProjectileDefinition definition)
    {
        definition = default;
        if (eventKey < 0 || sequenceProjectileList == null || eventKey >= sequenceProjectileList.Length)
        {
            return false;
        }

        definition = sequenceProjectileList[eventKey];
        return true;
    }

    public bool TryGetSpawnPoint(int spawnPointIndex, out WeaponSpawnPointDefinition definition)
    {
        definition = default;
        if (spawnPointIndex < 0 || spawnPoints == null || spawnPointIndex >= spawnPoints.Length)
        {
            return false;
        }

        definition = spawnPoints[spawnPointIndex];
        return true;
    }

    public bool TryGetSpawnPointPose(int spawnPointIndex, Transform anchor, out WeaponSpawnPointPose pose)
    {
        pose = default;
        if (anchor == null || !TryGetSpawnPoint(spawnPointIndex, out WeaponSpawnPointDefinition definition))
        {
            return false;
        }

        Vector3 worldPosition = anchor.TransformPoint(definition.LocalPosition);
        Quaternion worldRotation = anchor.rotation * Quaternion.Euler(0f, 0f, definition.LocalRotationOffset);
        pose = new WeaponSpawnPointPose(worldPosition, worldRotation);
        return true;
    }

    public bool TryGetSequenceSfx(int eventKey, out WeaponSequenceSfxDefinition definition)
    {
        definition = default;
        if (eventKey < 0 || sequenceSfxList == null || eventKey >= sequenceSfxList.Length)
        {
            return false;
        }

        definition = sequenceSfxList[eventKey];
        return true;
    }

    public bool TryGetSequenceVfx(int eventKey, out WeaponSequenceVfxDefinition definition)
    {
        definition = default;
        if (eventKey < 0 || sequenceVfxList == null || eventKey >= sequenceVfxList.Length)
        {
            return false;
        }

        definition = sequenceVfxList[eventKey];
        return true;
    }

    public string BuildDescriptionForLevel(int level)
    {
        WeaponLevelStatData stats = GetLevelStats(level);
        return ItemDescriptionUtility.BuildDetailedDescription(
            itemDescription,
            null,
            null,
            BuildWeaponDescriptionLines(stats),
            "一把可装备武器。");
    }

    public IEnumerable<DescriptorInfo> GetExtraInfosForLevel(int level)
    {
        WeaponLevelStatData stats = GetLevelStats(level);
        WeaponBenefitData displayedBenefits = stats.StatBenefits;
        List<DescriptorInfo> infos = new();
        string description = ItemDescriptionUtility.NormalizeManualDescription(itemDescription);
        if (!string.IsNullOrWhiteSpace(description))
        {
            infos.Add(new DescriptorInfo(string.Empty, description));
        }

        infos.Add(new DescriptorInfo(GameContentRuntime.GetPropDisplayName(PropType.Attack), ItemDescriptionUtility.FormatWeaponStatValue(PropType.Attack, stats.Attack)));
        infos.Add(new DescriptorInfo(GameContentRuntime.GetPropDisplayName(PropType.AttackSpeed), ItemDescriptionUtility.FormatWeaponStatValue(PropType.AttackSpeed, stats.AttackSpeed)));
        infos.Add(new DescriptorInfo(GameContentRuntime.GetPropDisplayName(PropType.CriticalChance), ItemDescriptionUtility.FormatWeaponStatValue(PropType.CriticalChance, stats.CriticalChance)));
        infos.Add(new DescriptorInfo(GameContentRuntime.GetPropDisplayName(PropType.CriticalPercent), ItemDescriptionUtility.FormatWeaponStatValue(PropType.CriticalPercent, stats.CriticalPercent)));
        infos.Add(new DescriptorInfo(GameContentRuntime.GetPropDisplayName(PropType.AttackRange), ItemDescriptionUtility.FormatWeaponStatValue(PropType.AttackRange, stats.Range)));
        infos.Add(new DescriptorInfo(GameContentRuntime.GetPropDisplayName(PropType.KnockbackStrength), ItemDescriptionUtility.FormatWeaponStatValue(PropType.KnockbackStrength, stats.KnockbackStrength)));
        AddAttackUsageInfos(infos, displayedBenefits);
        AddHolderModifierInfos(infos, stats.HolderModifiers);
        AddStatBenefitInfos(infos, displayedBenefits);
        if (tags != null && tags.Length > 0)
        {
            infos.Add(new DescriptorInfo("标签", ItemDescriptionUtility.JoinWeaponTags(tags)));
        }

        return infos;
    }

    public override IEnumerable<DescriptorInfo> GetExtraInfos()
    {
        return GetExtraInfosForLevel(WeaponLevelHelper.MinLevel);
    }

    private IEnumerable<ItemDescriptionLine> BuildWeaponDescriptionLines(WeaponLevelStatData stats)
    {
        WeaponBenefitData displayedBenefits = stats.StatBenefits;
        if (tags != null && tags.Length > 0)
        {
            yield return new ItemDescriptionLine(
                "标签",
                ItemDescriptionUtility.JoinWeaponTags(tags),
                ItemDescriptionLineKind.Meta);
        }

        yield return new ItemDescriptionLine(
            GameContentRuntime.GetPropDisplayName(PropType.Attack),
            ItemDescriptionUtility.FormatWeaponStatValue(PropType.Attack, stats.Attack),
            ItemDescriptionLineKind.Property);
        yield return new ItemDescriptionLine(
            GameContentRuntime.GetPropDisplayName(PropType.AttackSpeed),
            ItemDescriptionUtility.FormatWeaponStatValue(PropType.AttackSpeed, stats.AttackSpeed),
            ItemDescriptionLineKind.Property);
        yield return new ItemDescriptionLine(
            GameContentRuntime.GetPropDisplayName(PropType.CriticalChance),
            ItemDescriptionUtility.FormatWeaponStatValue(PropType.CriticalChance, stats.CriticalChance),
            ItemDescriptionLineKind.Property);
        yield return new ItemDescriptionLine(
            GameContentRuntime.GetPropDisplayName(PropType.CriticalPercent),
            ItemDescriptionUtility.FormatWeaponStatValue(PropType.CriticalPercent, stats.CriticalPercent),
            ItemDescriptionLineKind.Property);
        yield return new ItemDescriptionLine(
            GameContentRuntime.GetPropDisplayName(PropType.AttackRange),
            ItemDescriptionUtility.FormatWeaponStatValue(PropType.AttackRange, stats.Range),
            ItemDescriptionLineKind.Property);
        yield return new ItemDescriptionLine(
            GameContentRuntime.GetPropDisplayName(PropType.KnockbackStrength),
            ItemDescriptionUtility.FormatWeaponStatValue(PropType.KnockbackStrength, stats.KnockbackStrength),
            ItemDescriptionLineKind.Property);

        foreach (ItemDescriptionLine line in BuildAttackUsageDescriptionLines(displayedBenefits))
        {
            yield return line;
        }

        foreach (ItemDescriptionLine line in BuildHolderModifierDescriptionLines(stats.HolderModifiers))
        {
            yield return line;
        }

        foreach (ItemDescriptionLine line in BuildStatBenefitDescriptionLines(displayedBenefits))
        {
            yield return line;
        }
    }

    private static void AddStatBenefitInfos(List<DescriptorInfo> infos, WeaponBenefitData statBenefits)
    {
        if (infos == null)
        {
            return;
        }

        AddStatBenefitInfo(infos, statBenefits, PropType.AttackSpeed);
        AddStatBenefitInfo(infos, statBenefits, PropType.CriticalChance);
        AddStatBenefitInfo(infos, statBenefits, PropType.CriticalPercent);
        AddStatBenefitInfo(infos, statBenefits, PropType.AttackRange);
        AddStatBenefitInfo(infos, statBenefits, PropType.KnockbackStrength);
    }

    private static void AddStatBenefitInfo(List<DescriptorInfo> infos, WeaponBenefitData statBenefits, PropType propType)
    {
        float benefitPercent = statBenefits.GetBenefitPercent(propType);
        if (Mathf.Approximately(benefitPercent, 100f))
        {
            return;
        }

        infos.Add(new DescriptorInfo(
            $"{GameContentRuntime.GetPropDisplayName(propType)}收益率",
            FormatBenefitPercent(benefitPercent)));
    }

    private static void AddAttackUsageInfos(List<DescriptorInfo> infos, WeaponBenefitData attackUsage)
    {
        if (infos == null || !attackUsage.HasAnyUsage)
        {
            return;
        }

        AddAttackUsageInfo(infos, PropType.MeleeAttack, attackUsage.MeleeAttackUsagePercent);
        AddAttackUsageInfo(infos, PropType.RangedAttack, attackUsage.RangedAttackUsagePercent);
        AddAttackUsageInfo(infos, PropType.MagicAttack, attackUsage.MagicAttackUsagePercent);
        AddAttackUsageInfo(infos, PropType.SummonAttack, attackUsage.SummonAttackUsagePercent);
    }

    private static void AddAttackUsageInfo(List<DescriptorInfo> infos, PropType propType, float usagePercent)
    {
        if (usagePercent <= 0f)
        {
            return;
        }

        infos.Add(new DescriptorInfo(
            $"{GameContentRuntime.GetPropDisplayName(propType)}收益率",
            FormatAttackUsagePercent(usagePercent)));
    }

    private static IEnumerable<ItemDescriptionLine> BuildStatBenefitDescriptionLines(WeaponBenefitData statBenefits)
    {
        foreach (ItemDescriptionLine line in BuildStatBenefitLine(statBenefits, PropType.AttackSpeed))
        {
            yield return line;
        }

        foreach (ItemDescriptionLine line in BuildStatBenefitLine(statBenefits, PropType.CriticalChance))
        {
            yield return line;
        }

        foreach (ItemDescriptionLine line in BuildStatBenefitLine(statBenefits, PropType.CriticalPercent))
        {
            yield return line;
        }

        foreach (ItemDescriptionLine line in BuildStatBenefitLine(statBenefits, PropType.AttackRange))
        {
            yield return line;
        }

        foreach (ItemDescriptionLine line in BuildStatBenefitLine(statBenefits, PropType.KnockbackStrength))
        {
            yield return line;
        }
    }

    private static IEnumerable<ItemDescriptionLine> BuildStatBenefitLine(WeaponBenefitData statBenefits, PropType propType)
    {
        float benefitPercent = statBenefits.GetBenefitPercent(propType);
        if (Mathf.Approximately(benefitPercent, 100f))
        {
            yield break;
        }

        yield return new ItemDescriptionLine(
            $"{GameContentRuntime.GetPropDisplayName(propType)}收益率",
            FormatBenefitPercent(benefitPercent),
            ItemDescriptionLineKind.Property);
    }

    private static void AddHolderModifierInfos(List<DescriptorInfo> infos, IReadOnlyList<PropModifierData> holderModifiers)
    {
        if (infos == null || holderModifiers == null)
        {
            return;
        }

        for (int i = 0; i < holderModifiers.Count; i++)
        {
            PropModifierData modifier = holderModifiers[i];
            infos.Add(new DescriptorInfo($"持有者{modifier.GetDisplayName()}", modifier.GetDisplayValueText()));
        }
    }

    private static IEnumerable<ItemDescriptionLine> BuildHolderModifierDescriptionLines(IReadOnlyList<PropModifierData> holderModifiers)
    {
        if (holderModifiers == null)
        {
            yield break;
        }

        for (int i = 0; i < holderModifiers.Count; i++)
        {
            PropModifierData modifier = holderModifiers[i];
            yield return new ItemDescriptionLine(
                $"持有者{modifier.GetDisplayName()}",
                modifier.GetDisplayValueText(),
                ItemDescriptionLineKind.Property);
        }
    }

    private static IEnumerable<ItemDescriptionLine> BuildAttackUsageDescriptionLines(WeaponBenefitData attackUsage)
    {
        if (!attackUsage.HasAnyUsage)
        {
            yield break;
        }

        foreach (ItemDescriptionLine line in BuildAttackUsageLine(PropType.MeleeAttack, attackUsage.MeleeAttackUsagePercent))
        {
            yield return line;
        }

        foreach (ItemDescriptionLine line in BuildAttackUsageLine(PropType.RangedAttack, attackUsage.RangedAttackUsagePercent))
        {
            yield return line;
        }

        foreach (ItemDescriptionLine line in BuildAttackUsageLine(PropType.MagicAttack, attackUsage.MagicAttackUsagePercent))
        {
            yield return line;
        }

        foreach (ItemDescriptionLine line in BuildAttackUsageLine(PropType.SummonAttack, attackUsage.SummonAttackUsagePercent))
        {
            yield return line;
        }
    }

    private static IEnumerable<ItemDescriptionLine> BuildAttackUsageLine(PropType propType, float usagePercent)
    {
        if (usagePercent <= 0f)
        {
            yield break;
        }

        yield return new ItemDescriptionLine(
            $"{GameContentRuntime.GetPropDisplayName(propType)}收益率",
            FormatAttackUsagePercent(usagePercent),
            ItemDescriptionLineKind.Property);
    }

    private static string FormatAttackUsagePercent(float usagePercent)
    {
        return $"{Mathf.Max(0f, usagePercent):0.##}%";
    }

    private static string FormatBenefitPercent(float benefitPercent)
    {
        return $"{Mathf.Max(0f, benefitPercent):0.##}%";
    }

    private void NormalizeTags()
    {
        if (tags == null || tags.Length == 0)
        {
            tags = System.Array.Empty<WeaponTag>();
            return;
        }

        List<WeaponTag> normalizedTags = new();
        for (int i = 0; i < tags.Length; i++)
        {
            WeaponTag tag = tags[i];
            if (!Enum.IsDefined(typeof(WeaponTag), tag) || normalizedTags.Contains(tag))
            {
                continue;
            }

            normalizedTags.Add(tag);
        }

        tags = normalizedTags.Count > 0 ? normalizedTags.ToArray() : System.Array.Empty<WeaponTag>();
    }

    private void EnsureLevelStatsTable()
    {
        levelStats ??= new List<WeaponLevelStatData>();

        if (levelStats.Count == 0)
        {
            for (int level = WeaponLevelHelper.MinLevel; level <= WeaponLevelHelper.MaxLevel; level++)
            {
                levelStats.Add(CreateDefaultLevelStats(level));
            }

            return;
        }

        while (levelStats.Count < WeaponLevelHelper.MaxLevel)
        {
            int nextLevel = levelStats.Count + WeaponLevelHelper.MinLevel;
            WeaponLevelStatData template = levelStats[levelStats.Count - 1];
            levelStats.Add(template.ValidatedForLevel(nextLevel));
        }

        if (levelStats.Count > WeaponLevelHelper.MaxLevel)
        {
            levelStats.RemoveRange(WeaponLevelHelper.MaxLevel, levelStats.Count - WeaponLevelHelper.MaxLevel);
        }

        for (int i = 0; i < levelStats.Count; i++)
        {
            int expectedLevel = i + WeaponLevelHelper.MinLevel;
            levelStats[i] = levelStats[i].ValidatedForLevel(expectedLevel);
        }
    }

    private static WeaponLevelStatData CreateDefaultLevelStats(int level)
    {
        return new WeaponLevelStatData(
            level,
            0f,
            100f,
            0f,
            100f,
            0f,
            0f,
            Array.Empty<PropModifierData>(),
            WeaponBenefitData.Full);
    }
}

public sealed class WeaponLevelDescribable : IDescribable
{
    private readonly WeaponDataSO weaponData;
    private readonly int level;

    public WeaponLevelDescribable(WeaponDataSO weaponData, int level)
    {
        this.weaponData = weaponData;
        this.level = WeaponLevelHelper.ClampLevel(level);
    }

    public string Title => weaponData != null ? weaponData.Title : string.Empty;
    public Sprite Icon => weaponData != null ? weaponData.Icon : null;
    public string Description => weaponData != null ? weaponData.BuildDescriptionForLevel(level) : string.Empty;

    public IEnumerable<DescriptorInfo> GetExtraInfos()
    {
        return weaponData != null
            ? weaponData.GetExtraInfosForLevel(level)
            : System.Array.Empty<DescriptorInfo>();
    }
}
