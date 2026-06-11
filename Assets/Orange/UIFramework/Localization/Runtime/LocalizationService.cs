using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Orange.UIFramework
{
    [DisallowMultipleComponent]
    public sealed class LocalizationService : MonoBehaviour, ILocalizationService
    {
        public static LocalizationService Current { get; private set; }

        [Header("语言")]
        [SerializeField] private string defaultLanguage = "zh-CN";
        [SerializeField] private bool dontDestroyOnLoad = true;

        [Header("表格")]
        [SerializeField] private List<LocalizationTable> tables = new List<LocalizationTable>();

        private readonly Dictionary<string, LocalizationTable> tablesByLanguage = new Dictionary<string, LocalizationTable>();
        private string currentLanguage;

        public string CurrentLanguage => string.IsNullOrWhiteSpace(currentLanguage) ? defaultLanguage : currentLanguage;
        public event Action LanguageChanged;

        private void Awake()
        {
            if (Current != null && Current != this)
            {
                Destroy(gameObject);
                return;
            }

            Current = this;
            BuildTableCache();
            currentLanguage = string.IsNullOrWhiteSpace(defaultLanguage) ? "zh-CN" : defaultLanguage;

            if (dontDestroyOnLoad && transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Current == this)
            {
                Current = null;
            }
        }

        public UniTask SetLanguageAsync(string language, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string normalizedLanguage = NormalizeLanguage(language);
            if (string.Equals(currentLanguage, normalizedLanguage, StringComparison.Ordinal))
            {
                return UniTask.CompletedTask;
            }

            currentLanguage = normalizedLanguage;
            LanguageChanged?.Invoke();
            return UniTask.CompletedTask;
        }

        public string GetText(string key)
        {
            return GetText(key, null);
        }

        public string GetText(string key, IReadOnlyDictionary<string, object> args)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            string value = ResolveText(key);
            return args == null || args.Count == 0
                ? value
                : Format(value, args);
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

        public ValidationReport Validate()
        {
            ValidationReport report = new ValidationReport();
            if (string.IsNullOrWhiteSpace(defaultLanguage))
            {
                report.AddError($"LocalizationService '{name}' has an empty default language.");
            }

            HashSet<string> languages = new HashSet<string>();
            if (tables == null || tables.Count == 0)
            {
                report.AddWarning($"LocalizationService '{name}' has no localization tables.");
                return report;
            }

            for (int i = 0; i < tables.Count; i++)
            {
                LocalizationTable table = tables[i];
                if (table == null)
                {
                    report.AddError($"LocalizationService '{name}' has a null table at index {i}.");
                    continue;
                }

                if (!languages.Add(table.Language))
                {
                    report.AddError($"LocalizationService '{name}' contains duplicate language '{table.Language}'.");
                }

                report.Append(table.Validate());
            }

            return report;
        }

        private void BuildTableCache()
        {
            tablesByLanguage.Clear();
            if (tables == null)
            {
                return;
            }

            for (int i = 0; i < tables.Count; i++)
            {
                LocalizationTable table = tables[i];
                if (table == null)
                {
                    continue;
                }

                tablesByLanguage[table.Language] = table;
            }
        }

        private string ResolveText(string key)
        {
            if (tablesByLanguage.Count == 0)
            {
                BuildTableCache();
            }

            if (TryGetText(CurrentLanguage, key, out string value))
            {
                return value;
            }

            if (!string.Equals(CurrentLanguage, defaultLanguage, StringComparison.Ordinal) &&
                TryGetText(defaultLanguage, key, out value))
            {
                return value;
            }

            return key;
        }

        private bool TryGetText(string language, string key, out string value)
        {
            value = string.Empty;
            return !string.IsNullOrWhiteSpace(language) &&
                   tablesByLanguage.TryGetValue(language, out LocalizationTable table) &&
                   table != null &&
                   table.TryGetText(key, out value);
        }

        private static string NormalizeLanguage(string language)
        {
            return string.IsNullOrWhiteSpace(language) ? "zh-CN" : language.Trim();
        }

        private static string Format(string template, IReadOnlyDictionary<string, object> args)
        {
            if (string.IsNullOrEmpty(template))
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder(template);
            foreach (KeyValuePair<string, object> pair in args)
            {
                string token = "{" + pair.Key + "}";
                string value = Convert.ToString(pair.Value, CultureInfo.InvariantCulture) ?? string.Empty;
                builder.Replace(token, value);
            }

            return builder.ToString();
        }

        private void OnValidate()
        {
            defaultLanguage = string.IsNullOrWhiteSpace(defaultLanguage) ? "zh-CN" : defaultLanguage.Trim();
            if (tables == null)
            {
                tables = new List<LocalizationTable>();
            }

            tablesByLanguage.Clear();
        }
    }
}
