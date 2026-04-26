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
    [Tooltip("生成锚点索引。近战武器会忽略该索引并使用 hitDetectionTransform；远程武器会映射到对应枪口。")] 
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

[CreateAssetMenu(fileName = "Weapon Data", menuName = "SO/WeaponData", order = 0)]
public class WeaponDataSO : ItemDataSO, IDescribable
{
    [Header("Runtime")]
    [SerializeField] protected Weapon weaponPrefab;
    [SerializeField] private WeaponConstructionScheme constructionScheme = WeaponConstructionScheme.Default;
    [SerializeField] private AttackSequenceDefinitionSO attackSequence;

    [Header("Attack Presentation")]
    [SerializeField] private float visualForwardAngle = 45f;
    [SerializeField] private bool stopAimingWhenAttackReady = true;
    [Range(0.1f, 1f)]
    [SerializeField] private float attackSequenceOccupancy = 0.85f;
    [SerializeField] private WeaponSequenceProjectileDefinition[] sequenceProjectileList;
    [SerializeField] private WeaponSequenceSfxDefinition[] sequenceSfxList;
    [SerializeField] private WeaponSequenceVfxDefinition[] sequenceVfxList;
    [SerializeField] private AudioSfxKey hitSfxKey = AudioSfxKey.None;
    [SerializeField] private GameObject meleeHitVfxPrefab;
    [SerializeField] private Vector2 meleeHitBoxSize = new(1f, 1f);
    [SerializeField] private Vector2 meleeHitOffset;

    [Header("属性")]
    [SerializeField] protected float attack;
    [SerializeField] protected float attackSpeed = 1f;
    [SerializeField] protected float criticalChance;
    [SerializeField] protected float criticalPercent = 2f;
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
    public GameObject MeleeHitVfxPrefab => meleeHitVfxPrefab;
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

    public List<PropModifierData> GetPropsList()
    {
        return new List<PropModifierData>
        {
            new(PropType.Attack, PropModifierType.Add, attack),
            new(PropType.AttackSpeed, PropModifierType.Add, attackSpeed),
            new(PropType.CriticalChance, PropModifierType.Add, criticalChance),
            new(PropType.CriticalPercent, PropModifierType.Add, criticalPercent),
            new(PropType.Range, PropModifierType.Add, range)
        };
    }

    public List<PropModifierData> GetPropEntriesByLevel(int level)
    {
        float multiplier = 1f + (float)level / WeaponLevelScaling.MaxLevel;
        List<PropModifierData> calculatedProps = new();
        foreach (PropModifierData propEntry in GetPropsList())
        {
            calculatedProps.Add(new PropModifierData(propEntry.propType, propEntry.modifierType, propEntry.value * multiplier));
        }

        return calculatedProps;
    }

    public Dictionary<PropType, float> GetPropsByLevel(int level)
    {
        Dictionary<PropType, float> dictionary = new();
        List<PropModifierData> entries = GetPropEntriesByLevel(level);
        for (int i = 0; i < entries.Count; i++)
        {
            PropModifierData entry = entries[i];
            dictionary[entry.propType] = entry.value;
        }

        return dictionary;
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

    public override IEnumerable<DescriptorInfo> GetExtraInfos()
    {
        List<DescriptorInfo> infos = new();
        infos.Add(new DescriptorInfo("攻击力", $"{PropType.Attack.GetIconRichTextWithVOffset()}{attack}"));
        infos.Add(new DescriptorInfo("攻速", $"{PropType.AttackSpeed.GetIconRichTextWithVOffset()}{attackSpeed}"));
        infos.Add(new DescriptorInfo("描述", Description));
        return infos;
    }
}

internal static class WeaponLevelScaling
{
    public const int MaxLevel = 6;
}
