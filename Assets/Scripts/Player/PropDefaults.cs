using System;
using System.Collections.Generic;

public static class PropDefaults
{
    public static Dictionary<PropType, float> CreateBaseProps()
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
}
