using System.Collections.Generic;

public static class WeaponPropsCalculator
{
    private const int maxLevel = 6;

    public static List<PropEntry> GetPropEntries(WeaponDataSO weaponData, int level)
    {
        float multiplier = 1 + (float)level / maxLevel;

        List<PropEntry> calculatedProps = new();
        foreach (var propEntry in weaponData.GetPropsList())
        {
            if (weaponData.WeaponPrefab.GetType() != typeof(RangeWeapon) && propEntry.propType == PropType.Range)
            {
                calculatedProps.Add(new PropEntry(propEntry.propType, propEntry.modifierType, propEntry.value));
                continue;
            }

            calculatedProps.Add(new PropEntry(propEntry.propType, propEntry.modifierType, propEntry.value * multiplier));
        }

        return calculatedProps;
    }

    public static Dictionary<PropType, float> GetProps(WeaponDataSO weaponData, int level)
    {
        Dictionary<PropType, float> calculatedProps = new();
        List<PropEntry> entries = GetPropEntries(weaponData, level);
        for (int i = 0; i < entries.Count; i++)
        {
            PropEntry entry = entries[i];
            calculatedProps[entry.propType] = entry.value;
        }

        return calculatedProps;
    }
}
