using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public enum WeaponTag
{
    Melee = 0,
    Ranged = 1,
    Projectile = 2,
    AreaDamage = 3,
    Critical = 4,
    Fast = 5,
    Heavy = 6
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
    [Tooltip("相对武器根节点的Z轴角度偏移，用于调试绘制、VFX朝向或其他基于点位的表现。")]
    [SerializeField] private float localRotationOffset;

    public string Id => id;
    public Vector2 LocalPosition => localPosition;
    public float LocalRotationOffset => localRotationOffset;
}

[System.Serializable]
public struct WeaponSequenceProjectileDefinition
{
    [Tooltip("使用哪个武器点位。映射到 WeaponDataSO 的 Spawn Points；未配置对应点位时使用武器根节点。")]
    [SerializeField] private int spawnPointIndex;
    [Tooltip("直接引用要发射的弹射物定义资源。定义内部持有最终要实例化的弹射物预制体。")]
    [SerializeField] private ProjectileDefinitionSO projectileDefinition;
    [Tooltip("Burst 分组 id。用于避免同一 burst 重复启动。")]
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
    [Tooltip("生成锚点索引。映射到 WeaponDataSO 的 Spawn Points；未配置对应点位时使用武器根节点。")]
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
    [SerializeField, Min(0.01f)] private float attackSpeed;
    [SerializeField, Range(0f, 100f)] private float criticalChance;
    [SerializeField, Min(100f)] private float criticalPercent;
    [SerializeField, Min(0f)] private float range;
    [SerializeField, Min(0f)] private float knockbackStrength;

    public int Level => Mathf.Max(WeaponLevelHelper.MinLevel, level);
    public float Attack => Mathf.Max(0f, attack);
    public float AttackSpeed => Mathf.Max(0.01f, attackSpeed);
    public float CriticalChance => Mathf.Clamp(criticalChance, 0f, 100f);
    public float CriticalPercent => Mathf.Max(100f, criticalPercent);
    public float Range => Mathf.Max(0f, range);
    public float KnockbackStrength => Mathf.Max(0f, knockbackStrength);

    public WeaponLevelStatData(
        int level,
        float attack,
        float attackSpeed,
        float criticalChance,
        float criticalPercent,
        float range,
        float knockbackStrength)
    {
        this.level = Mathf.Max(WeaponLevelHelper.MinLevel, level);
        this.attack = Mathf.Max(0f, attack);
        this.attackSpeed = Mathf.Max(0.01f, attackSpeed);
        this.criticalChance = Mathf.Clamp(criticalChance, 0f, 100f);
        this.criticalPercent = Mathf.Max(100f, criticalPercent);
        this.range = Mathf.Max(0f, range);
        this.knockbackStrength = Mathf.Max(0f, knockbackStrength);
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
            KnockbackStrength);
    }

}

[Serializable]
public struct WeaponAttackUsageData
{
    public static WeaponAttackUsageData Zero => new(0f, 0f, 0f, 0f);

    [Tooltip("近战攻击使用区，百分比点口径：20 表示吃到玩家近战攻击属性的 20%。")]
    [SerializeField, Min(0f)] private float meleeAttackUsagePercent;
    [Tooltip("远程攻击使用区，百分比点口径：20 表示吃到玩家远程攻击属性的 20%。")]
    [SerializeField, Min(0f)] private float rangedAttackUsagePercent;
    [Tooltip("魔法攻击使用区，百分比点口径：20 表示吃到玩家魔法攻击属性的 20%。")]
    [SerializeField, Min(0f)] private float magicAttackUsagePercent;
    [Tooltip("召唤攻击使用区，百分比点口径：20 表示吃到玩家召唤攻击属性的 20%。")]
    [SerializeField, Min(0f)] private float summonAttackUsagePercent;

    public float MeleeAttackUsagePercent => Mathf.Max(0f, meleeAttackUsagePercent);
    public float RangedAttackUsagePercent => Mathf.Max(0f, rangedAttackUsagePercent);
    public float MagicAttackUsagePercent => Mathf.Max(0f, magicAttackUsagePercent);
    public float SummonAttackUsagePercent => Mathf.Max(0f, summonAttackUsagePercent);
    public bool HasAnyUsage => MeleeAttackUsagePercent > 0f ||
                               RangedAttackUsagePercent > 0f ||
                               MagicAttackUsagePercent > 0f ||
                               SummonAttackUsagePercent > 0f;

    public WeaponAttackUsageData(
        float meleeAttackUsagePercent,
        float rangedAttackUsagePercent,
        float magicAttackUsagePercent,
        float summonAttackUsagePercent)
    {
        this.meleeAttackUsagePercent = Mathf.Max(0f, meleeAttackUsagePercent);
        this.rangedAttackUsagePercent = Mathf.Max(0f, rangedAttackUsagePercent);
        this.magicAttackUsagePercent = Mathf.Max(0f, magicAttackUsagePercent);
        this.summonAttackUsagePercent = Mathf.Max(0f, summonAttackUsagePercent);
    }

    public WeaponAttackUsageData Validated()
    {
        return new WeaponAttackUsageData(
            MeleeAttackUsagePercent,
            RangedAttackUsagePercent,
            MagicAttackUsagePercent,
            SummonAttackUsagePercent);
    }

    public WeaponAttackUsageData Add(WeaponAttackUsageData other)
    {
        return new WeaponAttackUsageData(
            MeleeAttackUsagePercent + other.MeleeAttackUsagePercent,
            RangedAttackUsagePercent + other.RangedAttackUsagePercent,
            MagicAttackUsagePercent + other.MagicAttackUsagePercent,
            SummonAttackUsagePercent + other.SummonAttackUsagePercent);
    }

    public static WeaponAttackUsageData operator +(WeaponAttackUsageData left, WeaponAttackUsageData right)
    {
        return left.Add(right);
    }
}

[CreateAssetMenu(fileName = "Weapon Data", menuName = ScriptableObjectMenuPaths.WEAPON_DATA, order = 0)]
public class WeaponDataSO : ItemDataSO, IDescribable
{
    [Header("分类")]
    [SerializeField] private WeaponTag[] tags = System.Array.Empty<WeaponTag>();

    [Header("Runtime")]
    [SerializeField] private AttackSequenceDefinitionSO attackSequence;

    [Header("Attack Presentation")]
    [SerializeField] private float visualForwardAngle = 45f;
    [SerializeField] private bool stopAimingWhenAttackReady = true;
    [Range(0.1f, 1f)]
    [SerializeField] private float attackSequenceOccupancy = 0.85f;
    [SerializeField] private WeaponSpawnPointDefinition[] spawnPoints = System.Array.Empty<WeaponSpawnPointDefinition>();
    [SerializeField] private WeaponSequenceProjectileDefinition[] sequenceProjectileList;
    [SerializeField] private WeaponSequenceSfxDefinition[] sequenceSfxList;
    [SerializeField] private WeaponSequenceVfxDefinition[] sequenceVfxList;
    [Tooltip("启用后，攻击序列中的 OpenHitWindow / CloseHitWindow 事件才会产生碰撞盒检测。")]
    [SerializeField] private bool enableHitBox;
    [SerializeField] private GameObject hitVfxPrefab;
    [SerializeField] private Vector2 hitBoxSize = new(1f, 1f);
    [SerializeField] private Vector2 hitBoxOffset;

    [Header("攻击类型使用区")]
    [Tooltip("百分比点口径：20 表示吃到对应玩家攻击属性的 20%。最终伤害 = (武器攻击力 + 各类型攻击贡献) * (1 + Damage / 100)。")]
    [SerializeField] private WeaponAttackUsageData attackUsage;

    [Header("属性等级表")]
    [SerializeField] private List<WeaponLevelStatData> levelStats = new();

    public IReadOnlyList<WeaponTag> Tags => tags;
    public IReadOnlyList<WeaponLevelStatData> LevelStats => levelStats;
    public AttackSequenceDefinitionSO AttackSequence => attackSequence;
    public float VisualForwardAngle => visualForwardAngle;
    public bool StopAimingWhenAttackReady => stopAimingWhenAttackReady;
    public float AttackSequenceOccupancy => Mathf.Clamp(attackSequenceOccupancy, 0.1f, 1f);
    public IReadOnlyList<WeaponSpawnPointDefinition> SpawnPoints => spawnPoints;
    public IReadOnlyList<WeaponSequenceProjectileDefinition> SequenceProjectileList => sequenceProjectileList;
    public IReadOnlyList<WeaponSequenceSfxDefinition> SequenceSfxList => sequenceSfxList;
    public IReadOnlyList<WeaponSequenceVfxDefinition> SequenceVfxList => sequenceVfxList;
    public bool EnableHitBox => enableHitBox;
    public GameObject HitVfxPrefab => hitVfxPrefab;
    public Vector2 HitBoxSize => hitBoxSize;
    public Vector2 HitBoxOffset => hitBoxOffset;
    public WeaponAttackUsageData AttackUsage => attackUsage;
    public override string Description => BuildDescriptionForLevel(WeaponLevelHelper.MinLevel);

    private void OnValidate()
    {
        itemType = ItemType.Weapon;
        tags ??= System.Array.Empty<WeaponTag>();
        spawnPoints ??= System.Array.Empty<WeaponSpawnPointDefinition>();
        EnsureLevelStatsTable();
        attackSequenceOccupancy = Mathf.Clamp(attackSequenceOccupancy, 0.1f, 1f);
        hitBoxSize.x = Mathf.Max(0.01f, hitBoxSize.x);
        hitBoxSize.y = Mathf.Max(0.01f, hitBoxSize.y);
        attackUsage = attackUsage.Validated();
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
        AddAttackUsageInfos(infos);
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

        foreach (ItemDescriptionLine line in BuildAttackUsageDescriptionLines())
        {
            yield return line;
        }
    }

    private void AddAttackUsageInfos(List<DescriptorInfo> infos)
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
            $"{GameContentRuntime.GetPropDisplayName(propType)}使用区",
            FormatAttackUsagePercent(usagePercent)));
    }

    private IEnumerable<ItemDescriptionLine> BuildAttackUsageDescriptionLines()
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
            $"{GameContentRuntime.GetPropDisplayName(propType)}使用区",
            FormatAttackUsagePercent(usagePercent),
            ItemDescriptionLineKind.Property);
    }

    private static string FormatAttackUsagePercent(float usagePercent)
    {
        return $"{Mathf.Max(0f, usagePercent):0.##}%";
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
            1f,
            0f,
            100f,
            0f,
            0f);
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
