using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Buff Data", menuName = "SO/Buff", order = 0)]
public class BuffDataSO : ScriptableObject, IRuntimeFeatureSource
{
    private static readonly FeatureDisplayBuilder featureDisplayBuilder = new();
    private const string BUFF_ID_PREFIX = "Buff_";
    private const float MIN_DURATION_SECONDS = 0.01f;
    private const int MIN_STACK_COUNT = 1;

    [SerializeField] private string buffId = BUFF_ID_PREFIX;
    [SerializeField] private string displayName;
    [SerializeField] private Sprite icon;

    [Header("分类")]
    [SerializeField] private BuffPolarity polarity = BuffPolarity.Positive;

    [Header("持续时间")]
    [SerializeField] private BuffDurationPolicy durationPolicy = BuffDurationPolicy.Timed;
    [SerializeField] private float durationSeconds = 5f;

    [Header("叠层规则")]
    [SerializeField] private int maxStackCount = MIN_STACK_COUNT;
    [SerializeField] private BuffRefreshMode refreshMode = BuffRefreshMode.RefreshNewestStack;
    [SerializeField] private BuffOverflowMode overflowMode = BuffOverflowMode.RefreshDurationOnly;

    [Header("展示描述")]
    [Tooltip("支持占位符：{current_stacks}、{max_stacks}、{remaining_duration}、{total_duration}")]
    [SerializeField] private List<string> descriptionLines = new();

    [Header("属性修饰")]
    [Tooltip("按照 PropType 的语义填写：概率/比例统一使用 0~1，倍率类通常使用 1 代表 100%。")]
    [SerializeField] private List<PropEntry> propertyModifiers = new();

    [Header("特殊能力")]
    [SerializeReference] private List<FeatureEffectBase> specialFeatures = new();

    public string BuffId => buffId;
    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public BuffPolarity Polarity => polarity;
    public BuffDurationPolicy DurationPolicy => durationPolicy;
    public float DurationSeconds => durationPolicy == BuffDurationPolicy.Timed ? durationSeconds : 0f;
    public int MaxStackCount => maxStackCount;
    public BuffRefreshMode RefreshMode => refreshMode;
    public BuffOverflowMode OverflowMode => overflowMode;
    public IReadOnlyList<PropEntry> PropertyModifiers => propertyModifiers;

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

    public DisplayDocument BuildDisplayDocument(BuffDescriptionContext context)
    {
        DisplayDocument document = featureDisplayBuilder.Build(propertyModifiers, specialFeatures, new DisplayContext { IsCompact = true });
        List<TextLineItem> items = new();

        for (int i = 0; i < descriptionLines.Count; i++)
        {
            string line = BuffDescriptionFormatter.Format(descriptionLines[i], context);
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            items.Add(new TextLineItem
            {
                Text = line,
                StyleKey = "default"
            });
        }

        TextListBlock textBlock = document.GetBlock<TextListBlock>();
        if (textBlock != null)
        {
            textBlock.Items = items;
        }
        else
        {
            document.Blocks = new DisplayBlock[]
            {
                new TextListBlock
                {
                    BlockId = "descriptions",
                    Items = items
                }
            };
        }

        document.Id = $"buff_{buffId}";
        document.Title = displayName;
        document.Icon = icon;
        return document;
    }

    public IReadOnlyList<FeatureEffectBase> CreateRuntimeFeatureEffects(string runtimeSourceId)
    {
        List<FeatureEffectBase> effects = new(propertyModifiers.Count + specialFeatures.Count);

        for (int i = 0; i < propertyModifiers.Count; i++)
        {
            PropEntry modifier = propertyModifiers[i];
            string effectId = $"{runtimeSourceId}_{modifier.propType}_{modifier.modifierType}_{i}";
            effects.Add(new PropertyModifierEffect(effectId, effectId, modifier));
        }

        for (int i = 0; i < specialFeatures.Count; i++)
        {
            FeatureEffectBase feature = specialFeatures[i];
            if (feature == null)
            {
                continue;
            }

            feature.RuntimeFeatureId = $"{runtimeSourceId}_FEATURE_{i}";
            effects.Add(feature);
        }

        return effects;
    }
}
