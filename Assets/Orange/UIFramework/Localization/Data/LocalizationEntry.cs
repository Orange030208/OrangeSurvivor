using System;

namespace Orange.UIFramework
{
    [Serializable]
    public sealed class LocalizationEntry
    {
        public LocalizationEntry(string key, string value)
        {
            this.key = key ?? string.Empty;
            this.value = value ?? string.Empty;
        }

        [UnityEngine.SerializeField] private string key;
        [UnityEngine.TextArea]
        [UnityEngine.SerializeField] private string value;

        public string Key => key ?? string.Empty;
        public string Value => value ?? string.Empty;

        internal void Normalize()
        {
            key = key?.Trim() ?? string.Empty;
            if (value == null)
            {
                value = string.Empty;
            }
        }
    }
}
