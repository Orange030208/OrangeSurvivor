using UnityEngine.Serialization;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Item Quality Visual Config", menuName = ScriptableObjectMenuPaths.ITEM_QUALITY_VISUAL_CONFIG, order = 0)]
public class ItemQualityVisualConfigSO : ScriptableObject
{
    [Header("武器等级样式")]
    [SerializeField] private List<ItemQualityVisualStyle> weaponLevelStyles = new();

    [Header("饰品品质样式")]
    [FormerlySerializedAs("accessoryRarityStyles")]
    [SerializeField] private List<ItemQualityVisualStyle> accessoryTierStyles = new();

    public IReadOnlyList<ItemQualityVisualStyle> WeaponLevelStyles => weaponLevelStyles;
    public IReadOnlyList<ItemQualityVisualStyle> AccessoryTierStyles => accessoryTierStyles;

    public bool TryGetWeaponLevelStyle(int level, out ItemQualityVisualStyle style)
    {
        return TryGetStyle(weaponLevelStyles, WeaponLevelHelper.ClampLevel(level), out style);
    }

    public bool TryGetAccessoryTierStyle(ContentTier tier, out ItemQualityVisualStyle style)
    {
        return TryGetStyle(accessoryTierStyles, Mathf.Clamp((int)tier, (int)ContentTier.Common, (int)ContentTier.Legendary), out style);
    }

    private void OnValidate()
    {
        RemoveUnsupportedWeaponLevelStyles();
        weaponLevelStyles.Sort(CompareStyle);
        accessoryTierStyles.Sort(CompareStyle);
    }

    [NaughtyAttributes.Button("使用默认样式")]
    private void FillWithDefaultStyles()
    {
        weaponLevelStyles.Clear();
        for (int level = WeaponLevelHelper.MinLevel; level <= WeaponLevelHelper.MaxLevel; level++)
        {
            weaponLevelStyles.Add(ItemQualityVisualResolver.GetDefaultWeaponLevelStyle(level));
        }

        accessoryTierStyles.Clear();
        for (int rarity = (int)ContentTier.Common; rarity <= (int)ContentTier.Legendary; rarity++)
        {
            accessoryTierStyles.Add(ItemQualityVisualResolver.GetDefaultAccessoryTierStyle((ContentTier)rarity));
        }
    }

    private static int CompareStyle(ItemQualityVisualStyle left, ItemQualityVisualStyle right)
    {
        return left.QualityValue.CompareTo(right.QualityValue);
    }

    private void RemoveUnsupportedWeaponLevelStyles()
    {
        weaponLevelStyles ??= new List<ItemQualityVisualStyle>();
        weaponLevelStyles.RemoveAll(style =>
            style.QualityValue < WeaponLevelHelper.MinLevel ||
            style.QualityValue > WeaponLevelHelper.MaxLevel);
    }

    private static bool TryGetStyle(List<ItemQualityVisualStyle> styles, int qualityValue, out ItemQualityVisualStyle style)
    {
        for (int i = 0; i < styles.Count; i++)
        {
            if (styles[i].QualityValue != qualityValue)
            {
                continue;
            }

            style = styles[i];
            return true;
        }

        style = default;
        return false;
    }
}
