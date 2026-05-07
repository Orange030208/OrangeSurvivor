using System.Collections.Generic;
using UnityEngine;

namespace Orange.UIFramework
{
    [CreateAssetMenu(menuName = "Orange/UI Framework/Settings", fileName = "OrangeUIFrameworkSettings")]
    public sealed class UIFrameworkSettings : ScriptableObject
    {
        [Header("Runtime")]
        [SerializeField] private string instanceIdPrefix = "ui_";
        [SerializeField] private bool useUnscaledTime = true;

        [Header("Root")]
        [SerializeField] private string rootName = "UIRoot";
        [SerializeField] private bool dontDestroyOnLoad = true;
        [SerializeField] private CanvasProfile canvasProfile;

        [Header("Pooling")]
        [SerializeField] private bool enablePooling = true;
        [Min(0)]
        [SerializeField] private int maxCachedInstancesPerView = 3;

        [Header("Layers")]
        [SerializeField] private List<LayerDefinition> layers = CreateDefaultLayers();

        public string InstanceIdPrefix => instanceIdPrefix;
        public bool UseUnscaledTime => useUnscaledTime;
        public string RootName => string.IsNullOrWhiteSpace(rootName) ? "UIRoot" : rootName;
        public bool DontDestroyOnLoad => dontDestroyOnLoad;
        public CanvasProfile CanvasProfile => canvasProfile;
        public bool EnablePooling => enablePooling;
        public int MaxCachedInstancesPerView => maxCachedInstancesPerView;
        public IReadOnlyList<LayerDefinition> Layers => layers;

        public ValidationReport Validate(ViewCatalog catalog = null)
        {
            ValidationReport report = new ValidationReport();
            if (string.IsNullOrWhiteSpace(instanceIdPrefix))
            {
                report.AddError($"UIFrameworkSettings '{name}' has an empty instanceIdPrefix.");
            }

            if (canvasProfile == null)
            {
                report.AddError($"UIFrameworkSettings '{name}' has no CanvasProfile assigned.");
            }
            else
            {
                report.Append(canvasProfile.Validate());
            }

            ValidateLayers(report);

            if (catalog != null)
            {
                report.Append(catalog.Validate());
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

        public bool TryGetLayer(ViewLayer layer, out LayerDefinition definition)
        {
            if (layers == null)
            {
                definition = null;
                return false;
            }

            for (int i = 0; i < layers.Count; i++)
            {
                LayerDefinition candidate = layers[i];
                if (candidate != null && candidate.Layer == layer)
                {
                    definition = candidate;
                    return true;
                }
            }

            definition = null;
            return false;
        }

        private void ValidateLayers(ValidationReport report)
        {
            if (layers == null || layers.Count == 0)
            {
                report.AddError($"UIFrameworkSettings '{name}' has no layer definitions.");
                return;
            }

            HashSet<ViewLayer> configuredLayers = new HashSet<ViewLayer>();
            for (int i = 0; i < layers.Count; i++)
            {
                LayerDefinition layer = layers[i];
                if (layer == null)
                {
                    report.AddError($"UIFrameworkSettings '{name}' has a null LayerDefinition at index {i}.");
                    continue;
                }

                if (!configuredLayers.Add(layer.Layer))
                {
                    report.AddError($"UIFrameworkSettings '{name}' contains duplicate layer '{layer.Layer}'.");
                }
            }
        }

        private void OnValidate()
        {
            instanceIdPrefix = string.IsNullOrWhiteSpace(instanceIdPrefix) ? "ui_" : instanceIdPrefix.Trim();
            rootName = string.IsNullOrWhiteSpace(rootName) ? "UIRoot" : rootName.Trim();
            maxCachedInstancesPerView = Mathf.Max(0, maxCachedInstancesPerView);

            if (layers == null)
            {
                return;
            }

            for (int i = 0; i < layers.Count; i++)
            {
                layers[i]?.Normalize();
            }
        }

        private static List<LayerDefinition> CreateDefaultLayers()
        {
            return new List<LayerDefinition>
            {
                new LayerDefinition(ViewLayer.Background, -300, true),
                new LayerDefinition(ViewLayer.Hud, -100, true),
                new LayerDefinition(ViewLayer.Page, 0, true),
                new LayerDefinition(ViewLayer.Popup, 200, true),
                new LayerDefinition(ViewLayer.ModalMask, 300, true),
                new LayerDefinition(ViewLayer.Modal, 320, true),
                new LayerDefinition(ViewLayer.Tooltip, 500, false),
                new LayerDefinition(ViewLayer.System, 700, true),
                new LayerDefinition(ViewLayer.Debug, 900, true)
            };
        }
    }
}
