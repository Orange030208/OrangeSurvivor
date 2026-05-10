using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Orange.UIFramework
{
    [RequireComponent(typeof(TMP_Text))]
    public sealed class LocalizedText : MonoBehaviour
    {
        [Header("本地化")]
        [SerializeField] private LocalizationService localizationService;
        [SerializeField] private string key;
        [SerializeField] private bool refreshOnEnable = true;

        private readonly Dictionary<string, object> args = new Dictionary<string, object>();
        private TMP_Text text;
        private ILocalizationService boundService;

        private void Awake()
        {
            ResolveText();
        }

        private void OnEnable()
        {
            BindService();
            if (refreshOnEnable)
            {
                Refresh();
            }
        }

        private void OnDisable()
        {
            UnbindService();
        }

        public void SetKey(string newKey)
        {
            key = newKey ?? string.Empty;
            Refresh();
        }

        public void SetArgs(IReadOnlyDictionary<string, object> newArgs)
        {
            args.Clear();
            if (newArgs != null)
            {
                foreach (KeyValuePair<string, object> pair in newArgs)
                {
                    args[pair.Key] = pair.Value;
                }
            }

            Refresh();
        }

        public void SetArg(string argKey, object value)
        {
            if (string.IsNullOrWhiteSpace(argKey))
            {
                return;
            }

            args[argKey] = value;
            Refresh();
        }

        public void ClearArgs()
        {
            args.Clear();
            Refresh();
        }

        public void Refresh()
        {
            ResolveText();
            ILocalizationService service = ResolveService();
            if (text == null)
            {
                return;
            }

            if (service == null)
            {
                text.text = key ?? string.Empty;
                return;
            }

            text.text = args.Count > 0
                ? service.GetText(key, args)
                : service.GetText(key);
        }

        private void BindService()
        {
            ResolveText();
            ILocalizationService service = ResolveService();
            if (ReferenceEquals(boundService, service))
            {
                return;
            }

            UnbindService();
            boundService = service;
            if (boundService != null)
            {
                boundService.LanguageChanged += Refresh;
            }
        }

        private void UnbindService()
        {
            if (boundService == null)
            {
                return;
            }

            boundService.LanguageChanged -= Refresh;
            boundService = null;
        }

        private ILocalizationService ResolveService()
        {
            return localizationService != null
                ? localizationService
                : LocalizationService.Current;
        }

        private void ResolveText()
        {
            if (text == null)
            {
                text = GetComponent<TMP_Text>();
            }
        }

        private void OnValidate()
        {
            key = key?.Trim() ?? string.Empty;
        }
    }
}
