using System;
using UnityEngine;

namespace Orange.UIFramework
{
    [DisallowMultipleComponent]
    public sealed class StaticTextTooltipSource : MonoBehaviour, ITooltipContentSource
    {
        [SerializeField] [TextArea] private string text = string.Empty;
        [SerializeField] private string viewId = string.Empty;
        [SerializeField] private bool allowUserPin;
        [SerializeField] private bool showCloseButton;
        [SerializeField] private bool allowInteractiveTransient;

        public bool TryBuildTooltipContent(out TooltipContent content)
        {
            string resolvedViewId = ResolveViewId();
            if (string.IsNullOrWhiteSpace(resolvedViewId))
            {
                throw new InvalidOperationException("StaticTextTooltipSource requires a configured tooltip view id.");
            }

            content = new TooltipContent(
                resolvedViewId,
                text,
                new TooltipChromeOptions(
                    allowUserPin: allowUserPin,
                    showCloseButton: showCloseButton,
                    allowInteractiveTransient: allowInteractiveTransient));
            return true;
        }

        private string ResolveViewId()
        {
            if (!string.IsNullOrWhiteSpace(viewId))
            {
                return viewId.Trim();
            }

            UIManager manager = UIManager.Instance;
            if (manager != null && manager.Settings != null)
            {
                return manager.Settings.DefaultTextTooltipViewId;
            }

            return string.Empty;
        }
    }
}
