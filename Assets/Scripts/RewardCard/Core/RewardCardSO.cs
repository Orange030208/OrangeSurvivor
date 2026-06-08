using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Reward Card", menuName = ScriptableObjectMenuPaths.UPGRADE_CARD, order = 0)]
public class RewardCardSO : ScriptableObject, IInfoDocumentSource
{
    public const int UNLIMITED_PICK_COUNT = 0;

    [Header("基础")]
    [SerializeField] private string id;
    [SerializeField] private string title;
    [SerializeField] private ContentTier tier = ContentTier.Common;
    [SerializeField] private CardTag tags = CardTag.None;
    [SerializeField] private Sprite icon;

    [Header("描述")]
    [TextArea]
    [SerializeField] private string description;

    [Header("卡片能力")]
    [SerializeReference] private List<FeatureBase> grantedAbilities = new();

    public string Id => id;
    public string Title => title;
    public Sprite Icon => icon;
    public string ManualDescription => description;
    public string Description => BuildDescription();
    public ContentTier Tier => tier;
    public CardTag Tags => tags;
    public CardTag[] TagList => ToTagArray(tags);
    public IReadOnlyList<FeatureBase> GrantedAbilities => grantedAbilities;

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            id = Guid.NewGuid().ToString("N")[..8];
        }

        grantedAbilities ??= new List<FeatureBase>();
    }

    public bool HasAnyEffect()
    {
        return grantedAbilities.Count > 0;
    }

    public void InitializeRuntime(
        string runtimeId,
        string runtimeTitle,
        ContentTier runtimeTier,
        IReadOnlyList<CardTag> runtimeTags,
        Sprite runtimeIcon,
        string runtimeDescription,
        IReadOnlyList<FeatureBase> runtimeGrantedAbilities = null)
    {
        id = string.IsNullOrWhiteSpace(runtimeId) ? Guid.NewGuid().ToString("N")[..8] : runtimeId;
        title = runtimeTitle;
        tier = runtimeTier;
        tags = ToTagMask(runtimeTags);
        icon = runtimeIcon;
        description = runtimeDescription;
        grantedAbilities = runtimeGrantedAbilities != null
            ? new List<FeatureBase>(runtimeGrantedAbilities)
            : new List<FeatureBase>();
    }

    public bool HasTag(CardTag tag)
    {
        return tag != CardTag.None && (tags & tag) != 0;
    }

    public RewardCardOptionViewData CreateOptionViewData(int pickCount, int maxPickCount)
    {
        bool hasPickLimit = maxPickCount > UNLIMITED_PICK_COUNT;
        return new RewardCardOptionViewData(
            Id,
            Title,
            Icon,
            BuildDescription(),
            Tier,
            TagList,
            pickCount,
            maxPickCount,
            hasPickLimit);
    }

    public InfoDocument BuildInfoDocument()
    {
        return new RewardCardInfoBuilder().Build(this);
    }

    private string BuildDescription()
    {
        InfoDocument document = BuildInfoDocument();
        string formatted = InfoDocumentTextFormatter.ToPlainText(document, includeHeader: false);
        return string.IsNullOrWhiteSpace(formatted) ? "获得一项奖励。" : formatted;
    }

    private static CardTag ToTagMask(IReadOnlyList<CardTag> source)
    {
        CardTag mask = CardTag.None;
        if (source == null)
        {
            return mask;
        }

        for (int i = 0; i < source.Count; i++)
        {
            mask |= source[i];
        }

        return mask;
    }

    private static CardTag[] ToTagArray(CardTag mask)
    {
        if (mask == CardTag.None)
        {
            return Array.Empty<CardTag>();
        }

        List<CardTag> result = new();
        foreach (CardTag tag in Enum.GetValues(typeof(CardTag)))
        {
            if (tag == CardTag.None || (mask & tag) == 0)
            {
                continue;
            }

            result.Add(tag);
        }

        return result.ToArray();
    }
}
