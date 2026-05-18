#if UNITY_EDITOR
using System;
using System.Collections.Generic;

[Serializable]
public sealed class AccessoryJsonFile
{
    public List<AccessoryJsonAccessory> accessories = new();
}

[Serializable]
public sealed class AccessoryJsonAccessory
{
    public string accessoryId;
    public string itemName;
    public int itemPrice;
    public string itemDescription;
    public int recyclePrice;
    public string rarity;
    public int maxOwnedCount;
    public List<AccessoryJsonPropModifier> propertyModifiers = new();
    public List<AccessoryJsonFeature> specialFeatures = new();
}

[Serializable]
public sealed class AccessoryJsonPropModifier
{
    public string propType;
    public string modifierType;
    public float value;
}

[Serializable]
public sealed class AccessoryJsonFeature
{
    public string type;
    public AccessoryJsonPropModifier modifier;
    public AccessoryJsonWeaponBenefit benefitBonus;
}

[Serializable]
public sealed class AccessoryJsonWeaponBenefit
{
    public float attackSpeedBenefitPercent;
    public float criticalChanceBenefitPercent;
    public float criticalPercentBenefitPercent;
    public float rangeBenefitPercent;
    public float knockbackStrengthBenefitPercent;
    public float meleeAttackUsagePercent;
    public float rangedAttackUsagePercent;
    public float magicAttackUsagePercent;
    public float summonAttackUsagePercent;
}
#endif
