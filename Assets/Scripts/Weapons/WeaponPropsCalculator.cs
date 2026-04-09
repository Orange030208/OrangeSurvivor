using System.Collections.Generic;

public static class WeaponPropsCalculator
{
    private const int maxLevel = 6;

    public static Dictionary<PropType, float> GetProps(WeaponDataSO weaponData, int level)
    {
        float multiplier = 1 + (float)level / maxLevel;

        Dictionary<PropType, float> calculatedProps = new();
        foreach (var propEntry in weaponData.GetPropsList())
        {
            if (weaponData.WeaponPrefab.GetType() != typeof(RangeWeapon) && propEntry.propType == PropType.Range)
                calculatedProps.Add(propEntry.propType, propEntry.value);
            else
                calculatedProps.Add(propEntry.propType, propEntry.value * multiplier);
        }

        return calculatedProps;
    }
}
