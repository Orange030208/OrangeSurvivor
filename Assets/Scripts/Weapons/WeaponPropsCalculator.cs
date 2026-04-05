using System.Collections.Generic;

public static class WeaponPropsCalculator
{
    private const int maxLevel = 6;
    public static Dictionary<PropType, float> GetProps(WeaponDataSO weaponData,int level)
    {
        float multiplier = 1 + (float)level / maxLevel;

        Dictionary<PropType, float> calculatedProps = new();
        foreach (var kvp in weaponData.GetBaseProps())
        {
            if(weaponData.WeaponPrefab.GetType() != typeof(RangeWeapon) && kvp.Key == PropType.Range)
                calculatedProps.Add(kvp.Key, kvp.Value);
            else
                calculatedProps.Add(kvp.Key, kvp.Value * multiplier);
        }
        
        return calculatedProps;
    }
}