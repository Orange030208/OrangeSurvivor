using System;
using System.Collections.Generic;

namespace Orange.GameServices
{
    internal static class GameServiceTypeCache
    {
        private static readonly Dictionary<Type, string> displayNames = new Dictionary<Type, string>();

        public static string GetDisplayName(Type type)
        {
            if (type == null)
            {
                return "<null>";
            }

            if (displayNames.TryGetValue(type, out string cachedName))
            {
                return cachedName;
            }

            string displayName = string.IsNullOrEmpty(type.FullName) ? type.Name : type.FullName;
            displayNames.Add(type, displayName);
            return displayName;
        }
    }
}
