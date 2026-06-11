using System;
using System.Collections.Generic;
using UnityEngine;

namespace Orange.UIFramework
{
    [CreateAssetMenu(menuName = "Orange/UI Framework/View Catalog", fileName = "OrangeUIViewCatalog")]
    public sealed class ViewCatalog : ScriptableObject
    {
        [SerializeField] private List<ViewDefinition> views = new List<ViewDefinition>();

        public IReadOnlyList<ViewDefinition> Views => views;

        public ValidationReport Validate()
        {
            ValidationReport report = new ValidationReport();
            if (views == null)
            {
                report.AddError($"ViewCatalog '{name}' has a null view definition list.");
                return report;
            }

            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            Dictionary<Type, ViewDefinition> definitionsByType = new Dictionary<Type, ViewDefinition>();

            for (int i = 0; i < views.Count; i++)
            {
                ViewDefinition definition = views[i];
                if (definition == null)
                {
                    report.AddError($"ViewCatalog '{name}' contains a null definition at index {i}.");
                    continue;
                }

                ValidateDefinition(i, definition, ids, definitionsByType, report);
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

        public bool TryFindById(string id, out ViewDefinition definition)
        {
            definition = null;
            if (string.IsNullOrWhiteSpace(id))
            {
                return false;
            }

            for (int i = 0; i < views.Count; i++)
            {
                ViewDefinition candidate = views[i];
                if (candidate != null && string.Equals(candidate.Id, id, StringComparison.Ordinal))
                {
                    definition = candidate;
                    return true;
                }
            }

            return false;
        }

        public bool TryFindByType<TView>(out ViewDefinition definition)
            where TView : ViewBase
        {
            return TryFindByType(typeof(TView), out definition);
        }

        public bool TryFindByType(Type viewType, out ViewDefinition definition)
        {
            definition = null;
            if (viewType == null)
            {
                return false;
            }

            for (int i = 0; i < views.Count; i++)
            {
                ViewDefinition candidate = views[i];
                if (candidate == null || !candidate.TryGetViewType(out Type candidateType))
                {
                    continue;
                }

                if (candidateType == viewType)
                {
                    definition = candidate;
                    return true;
                }
            }

            return false;
        }

        private void ValidateDefinition(
            int index,
            ViewDefinition definition,
            HashSet<string> ids,
            Dictionary<Type, ViewDefinition> definitionsByType,
            ValidationReport report)
        {
            string id = definition.Id;
            if (string.IsNullOrWhiteSpace(id))
            {
                report.AddError($"ViewCatalog '{name}' definition at index {index} has an empty id.");
            }
            else if (!ids.Add(id))
            {
                report.AddError($"ViewCatalog '{name}' contains duplicate id '{id}'.");
            }

            if (definition.Prefab == null)
            {
                report.AddError($"ViewCatalog '{name}' definition '{id}' has no prefab.");
                return;
            }

            if (definition.Kind == ViewKind.Part)
            {
                report.AddError($"ViewCatalog '{name}' definition '{id}' is configured as Part. ViewPart is page-internal and must not be registered in the global ViewCatalog.");
                return;
            }

            ViewBase view = definition.Prefab.GetComponent<ViewBase>();
            if (view == null)
            {
                report.AddError($"ViewCatalog '{name}' definition '{id}' prefab '{definition.Prefab.name}' does not contain ViewBase on the root.");
                return;
            }

            Type viewType = view.GetType();
            if (!definition.AllowDuplicateViewType)
            {
                if (definitionsByType.TryGetValue(viewType, out ViewDefinition existingDefinition))
                {
                    report.AddError($"ViewCatalog '{name}' registers view type '{viewType.FullName}' more than once: '{existingDefinition.Id}' and '{id}'.");
                }
                else
                {
                    definitionsByType.Add(viewType, definition);
                }
            }

            ValidateKindMatchesBase(definition, view, viewType, report);
        }

        private void ValidateKindMatchesBase(
            ViewDefinition definition,
            ViewBase view,
            Type viewType,
            ValidationReport report)
        {
            bool valid = definition.Kind switch
            {
                ViewKind.Page => view is PageBase,
                ViewKind.Popup => view is PopupBase,
                ViewKind.Modal => IsModalType(viewType),
                ViewKind.Tooltip => view is TooltipBase,
                ViewKind.Toast => view is ToastBase,
                _ => false
            };

            if (!valid)
            {
                report.AddError($"ViewCatalog '{name}' definition '{definition.Id}' kind '{definition.Kind}' does not match prefab component type '{viewType.FullName}'.");
            }
        }

        private static bool IsModalType(Type viewType)
        {
            Type currentType = viewType;
            while (currentType != null)
            {
                if (currentType.IsGenericType && currentType.GetGenericTypeDefinition() == typeof(ModalBase<>))
                {
                    return true;
                }

                currentType = currentType.BaseType;
            }

            return false;
        }

        private void OnValidate()
        {
            if (views == null)
            {
                return;
            }

            for (int i = 0; i < views.Count; i++)
            {
                views[i]?.Normalize();
            }
        }
    }
}
