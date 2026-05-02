using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Item Quality Visual Config", menuName = ScriptableObjectMenuPaths.ITEM_QUALITY_VISUAL_CONFIG, order = 0)]
public class ItemQualityVisualConfigSO : ScriptableObject
{
    [Header("Weapon Level Styles")]
    [SerializeField] private List<ItemQualityVisualStyle> weaponLevelStyles = new();

    [Header("Accessory Rarity Styles")]
    [SerializeField] private List<ItemQualityVisualStyle> accessoryRarityStyles = new();

    public IReadOnlyList<ItemQualityVisualStyle> WeaponLevelStyles => weaponLevelStyles;
    public IReadOnlyList<ItemQualityVisualStyle> AccessoryRarityStyles => accessoryRarityStyles;

    public bool TryGetWeaponLevelStyle(int level, out ItemQualityVisualStyle style)
    {
        return TryGetStyle(weaponLevelStyles, WeaponLevelHelper.ClampLevel(level), out style);
    }

    public bool TryGetAccessoryRarityStyle(int rarity, out ItemQualityVisualStyle style)
    {
        return TryGetStyle(accessoryRarityStyles, Mathf.Clamp(rarity, 0, (int)AccessoryRarity.Legendary), out style);
    }

    private void OnValidate()
    {
        RemoveUnsupportedWeaponLevelStyles();
        weaponLevelStyles.Sort(CompareStyle);
        accessoryRarityStyles.Sort(CompareStyle);
    }

    [NaughtyAttributes.Button("使用默认样式")]
    private void FillWithDefaultStyles()
    {
        weaponLevelStyles.Clear();
        for (int level = WeaponLevelHelper.MinLevel; level <= WeaponLevelHelper.MaxLevel; level++)
        {
            weaponLevelStyles.Add(ItemQualityVisualResolver.GetDefaultWeaponLevelStyle(level));
        }

        accessoryRarityStyles.Clear();
        for (int rarity = 0; rarity <= (int)AccessoryRarity.Legendary; rarity++)
        {
            accessoryRarityStyles.Add(ItemQualityVisualResolver.GetDefaultAccessoryRarityStyle(rarity));
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
