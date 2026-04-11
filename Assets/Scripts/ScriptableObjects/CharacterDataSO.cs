using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Character Data", menuName = "SO/CharacterData", order = 0)]
public class CharacterDataSO : ScriptableObject, IDescriptionSource, IRuntimeFeatureSource
{
    [field: SerializeField] public string CharacterName { get; private set; }
    [field: SerializeField] public Sprite CharacterIcon { get; private set; }
    [field: SerializeField] public int PurchasePrice { get; private set; }

    [Header("角色额外属性")]
    [Tooltip("所有角色会先拥有全部 PropType 的默认值。这里配置的属性会在默认值基础上额外叠加，可用于后续自动描述。概率/比例统一使用 0~1，倍率类通常使用 1 代表 100%。")]
    [SerializeField] private List<PropEntry> extraProps = new();

    [Header("角色特殊能力")]
    [SerializeReference] private List<FeatureEffectBase> specialFeatures = new();

    [Space(8)]
    [Header("初始装备")]
    [SerializeField] private List<WeaponLevelEntry> initialWeapons = new();
    [SerializeField] private List<AccessoryDataSO> initialAccessories = new();

    public IReadOnlyList<PropEntry> ExtraProps => extraProps;
    public IReadOnlyList<WeaponLevelEntry> InitialWeapons => initialWeapons;
    public IReadOnlyList<AccessoryDataSO> InitialAccessories => initialAccessories;

    public Dictionary<PropType, float> GetBaseProps()
    {
        return CreateSharedBaseProps();
    }

    public List<PropEntry> GetCharacterModifiers()
    {
        return new List<PropEntry>(extraProps);
    }

    public IReadOnlyList<string> GetDescriptions()
    {
        List<string> descriptions = new(extraProps.Count + specialFeatures.Count + initialWeapons.Count + initialAccessories.Count);
        FeatureDescriptionBuilder.AddPropDescriptions(descriptions, extraProps);

        for (int i = 0; i < initialWeapons.Count; i++)
        {
            WeaponLevelEntry entry = initialWeapons[i];
            if (entry.weaponData == null)
            {
                continue;
            }

            descriptions.Add(FeatureDescriptionBuilder.BuildInitialWeaponDescription(entry.weaponData, entry.level));
        }

        for (int i = 0; i < initialAccessories.Count; i++)
        {
            AccessoryDataSO accessory = initialAccessories[i];
            if (accessory == null)
            {
                continue;
            }

            descriptions.Add(FeatureDescriptionBuilder.BuildAccessoryOwnedDescription(accessory));
        }

        FeatureDescriptionBuilder.AddFeatureDescriptions(descriptions, specialFeatures);
        return descriptions;
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

    public static Dictionary<PropType, float> CreateSharedBaseProps()
    {
        Dictionary<PropType, float> props = new();
        Array values = Enum.GetValues(typeof(PropType));

        for (int i = 0; i < values.Length; i++)
        {
            PropType propType = (PropType)values.GetValue(i);
            props[propType] = propType.GetDefaultValue();
        }

        return props;
    }

    private void OnValidate()
    {
    }
}
