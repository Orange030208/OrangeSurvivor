using UnityEngine;

[CreateAssetMenu(
    fileName = "Content Tier Weight Profile",
    menuName = ScriptableObjectMenuPaths.CONTENT_TIER_WEIGHT_PROFILE,
    order = 0)]
public sealed class ContentTierWeightProfileSO : ScriptableObject
{
    private const float DEFAULT_WEIGHT = 1f;

    [SerializeField, Min(0f)] private float commonWeight = DEFAULT_WEIGHT;
    [SerializeField, Min(0f)] private float rareWeight = DEFAULT_WEIGHT;
    [SerializeField, Min(0f)] private float epicWeight = DEFAULT_WEIGHT;
    [SerializeField, Min(0f)] private float legendaryWeight = DEFAULT_WEIGHT;

    public float CommonWeight => Mathf.Max(0f, commonWeight);
    public float RareWeight => Mathf.Max(0f, rareWeight);
    public float EpicWeight => Mathf.Max(0f, epicWeight);
    public float LegendaryWeight => Mathf.Max(0f, legendaryWeight);

    public float GetWeight(ContentTier tier)
    {
        return ContentTierResolver.FromQualityValue((int)tier) switch
        {
            ContentTier.Rare => RareWeight,
            ContentTier.Epic => EpicWeight,
            ContentTier.Legendary => LegendaryWeight,
            _ => CommonWeight
        };
    }

    private void OnValidate()
    {
        commonWeight = Mathf.Max(0f, commonWeight);
        rareWeight = Mathf.Max(0f, rareWeight);
        epicWeight = Mathf.Max(0f, epicWeight);
        legendaryWeight = Mathf.Max(0f, legendaryWeight);
    }
}
