using System.Collections.Generic;
using UnityEngine;

public class CharacterDataSO : ScriptableObject, IRuntimeFeatureSource,IDescribable
{
    [field: SerializeField] public string CharacterName { get; private set; }
    [field: SerializeField] public Sprite CharacterIcon { get; private set; }
    [field:SerializeField] public string CharacterDescription { get; private set; }
    [field: SerializeField] public RuntimeAnimatorController CharacterAnimatorController { get; private set; }
    [Header("角色额外属性")]
    [Tooltip("所有角色会先拥有全部 PropType 的默认值。这里配置的属性会在默认值基础上额外叠加，可用于后续自动描述。概率/比例统一使用 0~1，倍率类通常使用 1 代表 100%。")]
    [SerializeField] private List<PropEntry> extraProps = new();

    [Header("角色特殊能力")]
    [SerializeReference] private List<FeatureEffectBase> specialFeatures = new();

    [Space(8)]
    [Header("初始装备")]
    [SerializeField] private List<WeaponLevelEntry> initialWeapons = new();
    [SerializeField] private List<AccessoryDataSO> initialAccessories = new();
    
    public string Title => CharacterName;
    public Sprite Icon => CharacterIcon;
    public string Description => CharacterDescription;
    public IEnumerable<DescriptorInfo> GetExtraInfos()
    {
        List<DescriptorInfo> infos = new();
        foreach (PropEntry propEntry in extraProps)
        {
            infos.Add(new DescriptorInfo(propEntry.GetDisplayName(),
                $"额外{propEntry.propType.GetIconRichTextWithVOffset()}{propEntry.GetDisplayName()}{propEntry.value}"));
        }

        foreach (var weapon in initialWeapons)
        {
            infos.Add(new DescriptorInfo(weapon.weaponData.ItemName,
                $"初始有{ColorHelper.WrapRichTextColor(weapon.weaponData.ItemName, ColorHelper.GetColorByLevel(weapon.level))}"));
        }
        
        foreach (var accessory in initialAccessories)
        {
            infos.Add(new DescriptorInfo(accessory.ItemName,
                $"初始有{ColorHelper.WrapRichTextColor(accessory.ItemName, ColorHelper.GetColorByRarity(accessory.Rarity))}"));
        }

        foreach (var feature in specialFeatures)
        {
            infos.Add(new DescriptorInfo(feature.Title, feature.Description));
        }
        return infos;
    }

    public IReadOnlyList<PropEntry> ExtraProps => extraProps;
    public IReadOnlyList<WeaponLevelEntry> InitialWeapons => initialWeapons;
    public IReadOnlyList<AccessoryDataSO> InitialAccessories => initialAccessories;

    public List<PropEntry> GetCharacterModifiers()
    {
        return new List<PropEntry>(extraProps);
    }

    public IReadOnlyList<FeatureEffectBase> CreateRuntimeFeatureEffects(string runtimeSourceId)
    {
        List<FeatureEffectBase> effects = new(extraProps.Count + specialFeatures.Count);

        for (int i = 0; i < extraProps.Count; i++)
        {
            PropEntry modifier = extraProps[i];
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
