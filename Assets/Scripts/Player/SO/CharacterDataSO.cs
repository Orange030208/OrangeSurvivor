using System.Collections.Generic;
using UnityEngine;

public class CharacterDataSO : ScriptableObject, IDescribable
{
    [field: SerializeField] public string CharacterName { get; private set; }
    [field: SerializeField] public Sprite CharacterIcon { get; private set; }
    [field: SerializeField] public string CharacterDescription { get; private set; }
    [field: SerializeField] public RuntimeAnimatorController CharacterAnimatorController { get; private set; }

    [Header("基础属性")] [SerializeField] private BasePropGroupSO basePropsAsset;

    [Header("角色额外属性")] [Tooltip("配置角色提供的属性修饰。倍率统一使用 0~1 表示 0%~100%。")] [SerializeField]
    private List<PropModifierData> extraProps = new();

    [Header("角色特殊能力")] [SerializeReference]
    private List<FeatureEffectBase> specialFeatures = new();

    [Space(8)] [Header("初始装备")] [SerializeField]
    private List<WeaponEntry> initialWeapons = new();

    [SerializeField] private List<AccessoryDataSO> initialAccessories = new();

    public string Title => CharacterName;
    public Sprite Icon => CharacterIcon;
    public string Description => CharacterDescription;
    public BasePropGroupSO BasePropsAsset => basePropsAsset;
    
    public IReadOnlyList<PropModifierData> ExtraProps => extraProps;
    public IReadOnlyList<FeatureEffectBase> SpecialFeatures => specialFeatures;
    public IReadOnlyList<WeaponEntry> InitialWeapons => initialWeapons;
    public IReadOnlyList<AccessoryDataSO> InitialAccessories => initialAccessories;

    public IEnumerable<DescriptorInfo> GetExtraInfos()
    {
        List<DescriptorInfo> infos = new();
        foreach (PropModifierData modifier in extraProps)
        {
            infos.Add(new DescriptorInfo(modifier.GetDisplayName(),
                $"额外{modifier.propType.GetIconRichTextWithVOffset()}{modifier.GetDisplayName()}{modifier.GetDisplayValueText()}"));
        }

        foreach (WeaponEntry weapon in initialWeapons)
        {
            infos.Add(new DescriptorInfo(weapon.weaponData.ItemName,
                $"初始有{ColorHelper.WrapRichTextColor(weapon.weaponData.ItemName, ColorHelper.GetColorByLevel(weapon.level))}"));
        }

        foreach (AccessoryDataSO accessory in initialAccessories)
        {
            infos.Add(new DescriptorInfo(accessory.ItemName,
                $"初始有{ColorHelper.WrapRichTextColor(accessory.ItemName, ColorHelper.GetColorByRarity(accessory.Rarity))}"));
        }

        foreach (FeatureEffectBase feature in specialFeatures)
        {
            infos.Add(new DescriptorInfo(feature.Title, feature.Description));
        }

        return infos;
    }

    public List<PropModifierData> GetCharacterModifiers()
    {
        return new List<PropModifierData>(extraProps);
    }
}
