using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Buff Data", menuName = ScriptableObjectMenuPaths.BUFF, order = 0)]
public class BuffDataSO : ScriptableObject, IDescribable
{
    private const string BUFF_ID_PREFIX = "Buff_";
    private const float MIN_DURATION_SECONDS = 0.01f;
    private const int MIN_STACK_COUNT = 1;

    [SerializeField] private string buffId = BUFF_ID_PREFIX;
    [SerializeField] private string displayName;
    [SerializeField] private Sprite icon;
    [SerializeField] private string description;

    [Header("分类")]
    [SerializeField] private BuffPolarity polarity = BuffPolarity.Positive;

    [Header("持续时间")]
    [SerializeField] private BuffDurationPolicy durationPolicy = BuffDurationPolicy.Timed;
    [SerializeField] private float durationSeconds = 5f;

    [Header("叠层规则")]
    [SerializeField] private int maxStackCount = MIN_STACK_COUNT;
    [SerializeField] private BuffRefreshMode refreshMode = BuffRefreshMode.RefreshNewestStack;
    [SerializeField] private BuffOverflowMode overflowMode = BuffOverflowMode.RefreshDurationOnly;

    [Header("属性修饰")]
    [Tooltip("按照属性语义填写。倍率统一使用 0~1 表示 0%~100%。")]
    [SerializeField] private List<PropModifierData> propertyModifiers = new();

    [Header("特殊能力")]
    [SerializeReference] private List<FeatureEffectBase> specialFeatures = new();

    public string BuffId => buffId;
    public string DisplayName => displayName;
    public string Title => displayName;
    public Sprite Icon => icon;
    public string Description => description;
    public IEnumerable<DescriptorInfo> GetExtraInfos()
    {
        return null;
    }

    public BuffPolarity Polarity => polarity;
    public BuffDurationPolicy DurationPolicy => durationPolicy;
    public float DurationSeconds => durationPolicy == BuffDurationPolicy.Timed ? durationSeconds : 0f;
    public int MaxStackCount => maxStackCount;
    public BuffRefreshMode RefreshMode => refreshMode;
    public BuffOverflowMode OverflowMode => overflowMode;
    public IReadOnlyList<PropModifierData> PropertyModifiers => propertyModifiers;
    
    public IReadOnlyList<FeatureEffectBase> SpecialFeatures => specialFeatures;

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(buffId))
        {
            buffId = BUFF_ID_PREFIX;
        }
        else if (!buffId.StartsWith(BUFF_ID_PREFIX))
        {
            buffId = $"{BUFF_ID_PREFIX}{buffId}";
        }

        durationSeconds = Mathf.Max(MIN_DURATION_SECONDS, durationSeconds);
        maxStackCount = Mathf.Max(MIN_STACK_COUNT, maxStackCount);
    }
}
