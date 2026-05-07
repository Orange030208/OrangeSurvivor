using System.Collections.Generic;
using UnityEngine;

namespace Orange.UIFramework
{
    [CreateAssetMenu(menuName = "Orange/UI Framework/Localization Table", fileName = "LocalizationTable")]
    public sealed class LocalizationTable : ScriptableObject
    {
        [SerializeField] private string language = "zh-CN";
        [SerializeField] private List<LocalizationEntry> entries = new List<LocalizationEntry>();

        private Dictionary<string, string> valuesByKey;

        public string Language => string.IsNullOrWhiteSpace(language) ? "zh-CN" : language;
        public IReadOnlyList<LocalizationEntry> Entries => entries;

        public bool TryGetText(string key, out string value)
        {
            EnsureCache();
            if (string.IsNullOrWhiteSpace(key))
            {
                value = string.Empty;
                return false;
            }

            return valuesByKey.TryGetValue(key, out value);
        }

        public ValidationReport Validate()
        {
            ValidationReport report = new ValidationReport();
            if (string.IsNullOrWhiteSpace(language))
            {
                report.AddError($"LocalizationTable '{name}' has an empty language.");
            }

            HashSet<string> keys = new HashSet<string>();
            if (entries == null)
            {
                report.AddError($"LocalizationTable '{name}' has a null entries list.");
                return report;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                LocalizationEntry entry = entries[i];
                if (entry == null)
                {
                    report.AddError($"LocalizationTable '{name}' has a null entry at index {i}.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(entry.Key))
                {
                    report.AddError($"LocalizationTable '{name}' has an empty key at index {i}.");
                    continue;
                }

                if (!keys.Add(entry.Key))
                {
                    report.AddError($"LocalizationTable '{name}' contains duplicate key '{entry.Key}'.");
                }
            }

            return report;
        }

        [ContextMenu("Log Validation Report")]
        private void LogValidationReport()
        {
            ValidationReport report = Validate();
            if (report.HasErrors)
            {
                Debug.LogError(report.ToDisplayString(), this);
                return;
            }

            Debug.Log(report.ToDisplayString(), this);
        }

        private void EnsureCache()
        {
            if (valuesByKey != null)
            {
                return;
            }

            valuesByKey = new Dictionary<string, string>();
            if (entries == null)
            {
                return;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                LocalizationEntry entry = entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.Key))
                {
                    continue;
                }

                valuesByKey[entry.Key] = entry.Value;
            }
        }

        private void OnValidate()
        {
            language = string.IsNullOrWhiteSpace(language) ? "zh-CN" : language.Trim();
            if (entries != null)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    entries[i]?.Normalize();
                }
            }

            valuesByKey = null;
        }
    }
}
