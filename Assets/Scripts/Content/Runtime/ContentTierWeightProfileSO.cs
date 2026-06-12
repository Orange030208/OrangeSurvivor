using UnityEngine;
using UnityEngine.Serialization;

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

    [Header("幸运加权")]
    [FormerlySerializedAs("commonLuckCoefficient")]
    [Tooltip("Common 每点幸运值对应的权重增减值。")]
    [SerializeField] private float commonWeightPerLuckPoint;
    [FormerlySerializedAs("rareLuckCoefficient")]
    [Tooltip("Rare 每点幸运值对应的权重增减值。")]
    [SerializeField] private float rareWeightPerLuckPoint;
    [FormerlySerializedAs("epicLuckCoefficient")]
    [Tooltip("Epic 每点幸运值对应的权重增减值。")]
    [SerializeField] private float epicWeightPerLuckPoint;
    [FormerlySerializedAs("legendaryLuckCoefficient")]
    [Tooltip("Legendary 每点幸运值对应的权重增减值。")]
    [SerializeField] private float legendaryWeightPerLuckPoint;

    public float CommonWeight => Mathf.Max(0f, commonWeight);
    public float RareWeight => Mathf.Max(0f, rareWeight);
    public float EpicWeight => Mathf.Max(0f, epicWeight);
    public float LegendaryWeight => Mathf.Max(0f, legendaryWeight);

    public float CommonWeightPerLuckPoint => commonWeightPerLuckPoint;
    public float RareWeightPerLuckPoint => rareWeightPerLuckPoint;
    public float EpicWeightPerLuckPoint => epicWeightPerLuckPoint;
    public float LegendaryWeightPerLuckPoint => legendaryWeightPerLuckPoint;

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

    public float GetWeightPerLuckPoint(ContentTier tier)
    {
        return ContentTierResolver.FromQualityValue((int)tier) switch
        {
            ContentTier.Rare => RareWeightPerLuckPoint,
            ContentTier.Epic => EpicWeightPerLuckPoint,
            ContentTier.Legendary => LegendaryWeightPerLuckPoint,
            _ => CommonWeightPerLuckPoint
        };
    }

    private void OnValidate()
    {
        commonWeight = Mathf.Max(0f, commonWeight);
        rareWeight = Mathf.Max(0f, rareWeight);
        epicWeight = Mathf.Max(0f, epicWeight);
        legendaryWeight = Mathf.Max(0f, legendaryWeight);
        commonWeightPerLuckPoint = NormalizeWeightPerLuckPoint(commonWeightPerLuckPoint);
        rareWeightPerLuckPoint = NormalizeWeightPerLuckPoint(rareWeightPerLuckPoint);
        epicWeightPerLuckPoint = NormalizeWeightPerLuckPoint(epicWeightPerLuckPoint);
        legendaryWeightPerLuckPoint = NormalizeWeightPerLuckPoint(legendaryWeightPerLuckPoint);
    }

    private static float NormalizeWeightPerLuckPoint(float value)
    {
        return float.IsNaN(value) || float.IsInfinity(value) ? 0f : value;
    }
}
