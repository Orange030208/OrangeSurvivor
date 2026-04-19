using System.Collections.Generic;
using UnityEngine;

public enum WeaponConstructionScheme
{
    Default = 0
}

[System.Serializable]
public struct WeaponSequenceProjectileDefinition
{
    [Tooltip("使用哪个发射点。0 通常表示默认枪口。")]
    [SerializeField] private int spawnPointIndex;
    [Tooltip("直接引用要发射的弹射物定义资源。")]
    [SerializeField] private ProjectileDefinitionSO projectileDefinition;
    [Tooltip("Burst 分组 id。用于避免同一 burst 重复启动。")]
    [SerializeField] private int burstId;
    [Tooltip("本次发射所使用的模式。")]
    [SerializeField] private ProjectileFiringMode firingMode;
    [Tooltip("多弹模式参数。")]
    [SerializeField] private ProjectilePatternConfig patternConfig;

    public ProjectileSpawnPayload ToPayload()
    {
        return new ProjectileSpawnPayload(Mathf.Max(0, spawnPointIndex), projectileDefinition, Mathf.Max(0, burstId), firingMode, patternConfig);
    }
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
    [Tooltip("生成锚点索引。近战武器会忽略该索引并使用 hitDetectionTransform；远程武器会映射到对应枪口。")]
    [SerializeField] private int spawnPointIndex;
    [Tooltip("相对锚点的局部偏移。")]
    [SerializeField] private Vector3 localOffset;
    [Tooltip("相对锚点的局部旋转补偿。")]
    [SerializeField] private Vector3 localEulerAngles;
    [Tooltip("特效实例的自动销毁时间。")]
    [SerializeField] private float vfxLifetime;

    public GameObject VfxPrefab => vfxPrefab;
    public int SpawnPointIndex => Mathf.Max(0, spawnPointIndex);
    public Vector3 LocalOffset => localOffset;
    public Vector3 LocalEulerAngles => localEulerAngles;
    public float VfxLifetime => Mathf.Max(0.01f, vfxLifetime);
}

/// <summary>
/// 武器数据资源：
/// - 提供武器 prefab；
/// - 提供武器基础属性；
/// - 可选提供默认攻击序列；
/// - 提供武器攻击表现层的基础配置；
/// - 作为 UI 展示层的描述来源。
/// 当前它只承担“数据来源”与“描述来源”职责，
/// 运行时攻击逻辑仍然在 Weapon / MeleeWeapon / RangeWeapon 中。
/// </summary>
[CreateAssetMenu(fileName = "Weapon Data", menuName = "SO/WeaponData", order = 0)]
public class WeaponDataSO : ItemDataSO, IDescriptionSource
{
    [Header("Runtime")]
    [Tooltip("运行时实例化的武器 prefab。")]
    [SerializeField] protected Weapon weaponPrefab;
    [Tooltip("武器运行时构造方案。当前默认方案会按 WeaponDataSO 下发 icon、默认前向角度与近战命中盒等基础配置；后续若要扩展特殊装配路径，可在这里新增枚举值。")]
    [SerializeField] private WeaponConstructionScheme constructionScheme = WeaponConstructionScheme.Default;
    [Tooltip("可选默认攻击序列。为空时武器运行时会自行生成代码级默认序列。")]
    [SerializeField] private AttackSequenceDefinitionSO attackSequence;

    [Header("Attack Presentation")]
    [Tooltip("武器美术默认前向角度。贴图本身朝上时填 0；当前项目若贴图横向摆放，通常填 45，使 EntityRenderer 对应物体转正到以 transform.up 为武器前向。")]
    [SerializeField] private float visualForwardAngle = 45f;
    [Tooltip("是否在攻击准备完成后停止继续追踪瞄准。开启后，武器进入可攻击状态或攻击中时会锁定当前朝向，避免动作播放时还在持续转向导致动画变形。")]
    [SerializeField] private bool stopAimingWhenAttackReady = true;
    [Tooltip("一次攻击序列最多占用本次攻击间隔的百分比。1 代表动画可以刚好占满整个攻击间隔；0.8 代表会压缩到 80%，留 20% 给下次攻击前的准备/停顿。")]
    [Range(0.1f, 1f)]
    [SerializeField] private float attackSequenceOccupancy = 0.85f;

    [Tooltip("该武器可使用的弹射物定义列表。序列里的 ProjectileVariantIndex 会映射到这里；近战武器也可以为命中后生成弹射物预留这些定义。")]
    [SerializeField] private ProjectileDefinitionSO[] projectileDefinitions;

    [Tooltip("攻击序列对应的弹射物配置列表，SpawnProjectile 事件会使用 eventKey 作为这里的列表下标。")]
    [SerializeField] private WeaponSequenceProjectileDefinition[] sequenceProjectileList;

    [Tooltip("攻击序列 PlaySfx 事件会使用事件自身的 eventKey 作为这里的列表下标。")]
    [SerializeField] private WeaponSequenceSfxDefinition[] sequenceSfxList;

    [Tooltip("攻击序列 PlayVfx 事件会使用事件自身的 eventKey 作为这里的列表下标。")]
    [SerializeField] private WeaponSequenceVfxDefinition[] sequenceVfxList;

    [Tooltip("武器命中目标后播放的命中音效。未配置时不播放。")]
    [SerializeField] private AudioSfxKey hitSfxKey = AudioSfxKey.None;

    [Tooltip("近战武器命中盒尺寸。仅近战武器使用；远程武器可忽略此字段。")]
    [SerializeField] private Vector2 meleeHitBoxSize = new(1f, 1f);
    [Tooltip("近战命中参考点的局部偏移。默认构造方案会把这个偏移量应用到 hitDetectionTransform。")]
    [SerializeField] private Vector2 meleeHitOffset;

    [Header("属性")]
    [Tooltip("攻击力：固定值，直接加到武器伤害。")]
    [SerializeField] protected float attack;
    [Tooltip("攻击速度：倍率，1 代表 100% 攻速。")]
    [SerializeField] protected float attackSpeed = 1f;
    [Tooltip("暴击率：概率，使用 0~1 表示，例如 0.05 = 5%。")]
    [SerializeField] protected float criticalChance;
    [Tooltip("武器暴击倍率：2 代表 200% 暴击伤害。")]
    [SerializeField] protected float criticalPercent = 2f;
    [Tooltip("攻击范围：固定值，直接增加武器索敌/攻击范围。")]
    [SerializeField] protected float range;

    public Weapon WeaponPrefab => weaponPrefab;
    public WeaponConstructionScheme ConstructionScheme => constructionScheme;
    public AttackSequenceDefinitionSO AttackSequence => attackSequence;
    public float VisualForwardAngle => visualForwardAngle;
    public bool StopAimingWhenAttackReady => stopAimingWhenAttackReady;
    public float AttackSequenceOccupancy => Mathf.Clamp(attackSequenceOccupancy, 0.1f, 1f);
    public IReadOnlyList<WeaponSequenceProjectileDefinition> SequenceProjectileList => sequenceProjectileList;
    public IReadOnlyList<WeaponSequenceSfxDefinition> SequenceSfxList => sequenceSfxList;
    public IReadOnlyList<WeaponSequenceVfxDefinition> SequenceVfxList => sequenceVfxList;
    public AudioSfxKey HitSfxKey => hitSfxKey;
    public Vector2 MeleeHitBoxSize => meleeHitBoxSize;
    public Vector2 MeleeHitOffset => meleeHitOffset;

    private void OnValidate()
    {
        itemType = ItemType.Weapon;
        attackSpeed = Mathf.Max(0.01f, attackSpeed);
        criticalChance = Mathf.Clamp01(criticalChance);
        criticalPercent = Mathf.Max(1f, criticalPercent);
        range = Mathf.Max(0f, range);
        attackSequenceOccupancy = Mathf.Clamp(attackSequenceOccupancy, 0.1f, 1f);
        meleeHitBoxSize.x = Mathf.Max(0.01f, meleeHitBoxSize.x);
        meleeHitBoxSize.y = Mathf.Max(0.01f, meleeHitBoxSize.y);
    }

    /// <summary>
    /// 返回武器基础属性条目。
    /// 等级修正不在这里做，而是交给 WeaponPropsCalculator 统一处理。
    /// </summary>
    public List<PropEntry> GetPropsList()
    {
        return new List<PropEntry>
        {
            new(PropType.Attack, attack),
            new(PropType.AttackSpeed, attackSpeed),
            new(PropType.CriticalChance, criticalChance),
            new(PropType.CriticalPercent, criticalPercent),
            new(PropType.Range, range)
        };
    }

    public IReadOnlyList<string> GetDescriptions()
    {
        return GetDescriptions(1);
    }

    /// <summary>
    /// 生成指定等级下用于 UI 展示的描述列表。
    /// </summary>
    public List<string> GetDescriptions(int level)
    {
        List<PropEntry> entries = GetPropEntriesByLevel(level);
        return FeatureDescriptionBuilder.BuildPropDescriptions(entries);
    }

    public List<PropEntry> GetPropEntriesByLevel(int level)
    {
        return WeaponPropsCalculator.GetPropEntries(this, level);
    }

    public Dictionary<PropType, float> GetPropsByLevel(int level)
    {
        Dictionary<PropType, float> dictionary = new();
        List<PropEntry> entries = GetPropEntriesByLevel(level);
        for (int i = 0; i < entries.Count; i++)
        {
            PropEntry entry = entries[i];
            dictionary[entry.propType] = entry.value;
        }

        return dictionary;
    }
}
