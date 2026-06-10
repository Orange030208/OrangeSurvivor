using System;
using System.Collections.Generic;

public static class FeatureTypeSelectionUtility
{
    public static IEnumerable<Type> GetSelectableFeatureTypes()
    {
        List<Type> result = new();
        Type featureBaseType = typeof(FeatureBase);

        AppDomain currentDomain = AppDomain.CurrentDomain;
        System.Reflection.Assembly[] assemblies = currentDomain.GetAssemblies();
        for (int assemblyIndex = 0; assemblyIndex < assemblies.Length; assemblyIndex++)
        {
            Type[] types;
            try
            {
                types = assemblies[assemblyIndex].GetTypes();
            }
            catch (System.Reflection.ReflectionTypeLoadException exception)
            {
                types = exception.Types;
            }

            if (types == null)
            {
                continue;
            }

            for (int typeIndex = 0; typeIndex < types.Length; typeIndex++)
            {
                Type type = types[typeIndex];
                if (type == null ||
                    !featureBaseType.IsAssignableFrom(type) ||
                    type.IsAbstract ||
                    type.IsGenericType ||
                    Attribute.IsDefined(type, typeof(HideInFeatureMenuAttribute), false) ||
                    type.GetConstructor(Type.EmptyTypes) == null)
                {
                    continue;
                }

                result.Add(type);
            }
        }

        result.Sort((left, right) => string.CompareOrdinal(left.Name, right.Name));
        return result;
    }
}
