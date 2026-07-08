using System;
using System.Collections.Generic;
using UnityEditor;

namespace Orange.GameServices.Editor
{
    internal static class GameServiceEditorTypeUtility
    {
        private static List<Type> cachedServiceTypes;

        public static IReadOnlyList<Type> GetConcreteServiceTypes()
        {
            if (cachedServiceTypes != null)
            {
                return cachedServiceTypes;
            }

            cachedServiceTypes = new List<Type>();
            TypeCache.TypeCollection serviceTypes = TypeCache.GetTypesDerivedFrom<GameService>();
            for (int i = 0; i < serviceTypes.Count; i++)
            {
                Type type = serviceTypes[i];
                if (!type.IsClass ||
                    type.IsAbstract ||
                    type.IsGenericTypeDefinition ||
                    type.IsNested ||
                    !type.IsSerializable)
                {
                    continue;
                }

                cachedServiceTypes.Add(type);
            }

            cachedServiceTypes.Sort(CompareTypeName);
            return cachedServiceTypes;
        }

        public static string GetMenuName(Type type)
        {
            string namespaceName = string.IsNullOrEmpty(type.Namespace) ? "Global" : type.Namespace;
            return $"{namespaceName}/{type.Name}";
        }

        public static string GetDisplayName(SerializedProperty managedReferenceProperty)
        {
            object value = managedReferenceProperty.managedReferenceValue;
            if (value != null)
            {
                return value.GetType().Name;
            }

            return "<Missing Service>";
        }

        private static int CompareTypeName(Type left, Type right)
        {
            return string.CompareOrdinal(GetMenuName(left), GetMenuName(right));
        }
    }
}
