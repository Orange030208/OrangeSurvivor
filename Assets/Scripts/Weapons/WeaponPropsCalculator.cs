using System.Collections.Generic;

public static class WeaponPropsCalculator
{
    private const int MaxLevel = 6;

    public static List<PropEntry> GetPropEntries(WeaponDataSO weaponData, int level)
    {
        float multiplier = 1f + (float)level / MaxLevel;

        List<PropEntry> calculatedProps = new();
        foreach (PropEntry propEntry in weaponData.GetPropsList())
        {
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
