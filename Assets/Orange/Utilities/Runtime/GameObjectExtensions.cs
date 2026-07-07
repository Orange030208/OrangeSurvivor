using System;
using UnityEngine;

namespace Orange.Utilities
{
    /// <summary>
    /// Common helpers for GameObject state and hierarchy operations.
    /// </summary>
    public static class GameObjectExtensions
    {
        private const int MIN_LAYER = 0;
        private const int MAX_LAYER = 31;

        /// <summary>
        /// Applies active state only when the current state differs.
        /// </summary>
        public static void SetActiveIfChanged(this GameObject gameObject, bool active)
        {
            if (gameObject == null)
            {
                throw new ArgumentNullException(nameof(gameObject));
            }

            if (gameObject.activeSelf == active)
            {
                return;
            }

            gameObject.SetActive(active);
        }

        /// <summary>
        /// Sets the layer on this object and every descendant.
        /// </summary>
        public static void SetLayerRecursively(this GameObject gameObject, int layer)
        {
            if (gameObject == null)
            {
                throw new ArgumentNullException(nameof(gameObject));
            }

            if (layer < MIN_LAYER || layer > MAX_LAYER)
            {
                throw new ArgumentOutOfRangeException(nameof(layer), layer, $"Layer must be between {MIN_LAYER} and {MAX_LAYER}.");
            }

            SetLayerRecursively(gameObject.transform, layer);
        }

        private static void SetLayerRecursively(Transform transform, int layer)
        {
            transform.gameObject.layer = layer;

            for (int i = 0; i < transform.childCount; i++)
            {
                SetLayerRecursively(transform.GetChild(i), layer);
            }
        }
    }
}
