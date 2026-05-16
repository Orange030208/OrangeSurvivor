using System.Collections.Generic;
using UnityEngine;

public class CharacterDataSO : ScriptableObject, IDescribable
{
    [field: SerializeField] public string CharacterName { get; private set; }
    [field: SerializeField] public Sprite CharacterIcon { get; private set; }
    [field: SerializeField] public string CharacterDescription { get; private set; }
    [field: SerializeField] public RuntimeAnimatorController CharacterAnimatorController { get; private set; }

    [Header("基础属性")] [SerializeField] private BasePropGroupSO basePropsAsset;

    [Header("角色额外属性")] [Tooltip("按照属性语义填写。百分比属性与所有乘区统一使用百分比点：1 表示 1%，10 表示 10%。点数属性仍按属性单位填写。")] [SerializeField]
    private List<PropModifierData> extraProps = new();

    [Header("角色特殊能力")] [SerializeReference]
    private List<FeatureBase> specialFeatures = new();

    [Space(8)] [Header("初始装备")] [SerializeField]
    private List<WeaponEntry> initialWeapons = new();

    [SerializeField] private List<AccessoryDataSO> initialAccessories = new();

    public string Title => CharacterName;
    public Sprite Icon => CharacterIcon;
    public string Description => CharacterDescription;
    public BasePropGroupSO BasePropsAsset => basePropsAsset;
    
    public IReadOnlyList<PropModifierData> ExtraProps => GetReadOnlyListOrEmpty(extraProps);
    public IReadOnlyList<FeatureBase> SpecialFeatures => GetReadOnlyListOrEmpty(specialFeatures);
    public IReadOnlyList<WeaponEntry> InitialWeapons => GetReadOnlyListOrEmpty(initialWeapons);
    public IReadOnlyList<AccessoryDataSO> InitialAccessories => GetReadOnlyListOrEmpty(initialAccessories);

    private void OnValidate()
    {
        extraProps ??= new List<PropModifierData>();
        specialFeatures ??= new List<FeatureBase>();
        initialWeapons ??= new List<WeaponEntry>();
        initialAccessories ??= new List<AccessoryDataSO>();

        for (int i = 0; i < initialWeapons.Count; i++)
        {
            initialWeapons[i] = initialWeapons[i].Validated();
        }
    }

    public IEnumerable<DescriptorInfo> GetExtraInfos()
    {
        List<DescriptorInfo> infos = new();
        foreach (PropModifierData modifier in ExtraProps)
        {
            infos.Add(new DescriptorInfo(modifier.GetDisplayName(), modifier.GetDisplayValueText()));
        }

        foreach (WeaponEntry weapon in InitialWeapons)
        {
            WeaponDataSO weaponData = weapon.weaponData;
            if (weaponData == null)
            {
                continue;
            }

            infos.Add(new DescriptorInfo(weaponData.ItemName,
                $"初始有{ColorHelper.WrapRichTextColor(weaponData.ItemName, ColorHelper.GetColorByLevel(weapon.level))}"));
        }

        foreach (AccessoryDataSO accessory in InitialAccessories)
        {
            if (accessory == null)
            {
                continue;
            }

            infos.Add(new DescriptorInfo(accessory.ItemName,
                $"初始有{ColorHelper.WrapRichTextColor(accessory.ItemName, ColorHelper.GetColorByRarity(accessory.Rarity))}"));
        }

        foreach (FeatureBase feature in SpecialFeatures)
        {
            if (feature == null)
            {
                continue;
            }

            infos.Add(new DescriptorInfo(feature.Title, feature.Description));
        }

        return infos;
    }

    public List<PropModifierData> GetCharacterModifiers()
    {
        return new List<PropModifierData>(ExtraProps);
    }

    private static IReadOnlyList<T> GetReadOnlyListOrEmpty<T>(List<T> source)
    {
        if (source != null)
        {
            return source;
        }

        return System.Array.Empty<T>();
    }
}
