using System;
using UnityEngine;

namespace Orange.Utilities
{
    /// <summary>
    /// Common helpers for RectTransform layout operations.
    /// </summary>
    public static class RectTransformExtensions
    {
        /// <summary>
        /// Stretches the rect to fill its parent without changing pivot.
        /// </summary>
        public static void StretchToParent(this RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                throw new ArgumentNullException(nameof(rectTransform));
            }

            if (rectTransform.parent == null)
            {
                throw new InvalidOperationException("RectTransform must have a parent before it can stretch to parent.");
            }

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
