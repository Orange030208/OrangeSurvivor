using System;
using UnityEngine;

namespace Orange.Utilities
{
    /// <summary>
    /// Common helpers for explicit, inspector-friendly Transform operations.
    /// </summary>
    public static class TransformExtensions
    {
        /// <summary>
        /// Destroys every direct child under the transform, iterating from last sibling to first.
        /// </summary>
        public static void Clear(this Transform transform, bool immediate = false)
        {
            if (transform == null)
            {
                throw new ArgumentNullException(nameof(transform));
            }

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                GameObject childGameObject = child.gameObject;

                if (immediate || !Application.isPlaying)
                {
                    UnityEngine.Object.DestroyImmediate(childGameObject);
                }
                else
                {
                    UnityEngine.Object.Destroy(childGameObject);
                }
            }
        }

        /// <summary>
        /// Resets local position, rotation, and scale to Unity defaults.
        /// </summary>
        public static void ResetLocal(this Transform transform)
        {
            if (transform == null)
            {
                throw new ArgumentNullException(nameof(transform));
            }

            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        /// <summary>
        /// Invokes the action for each direct child in sibling order.
        /// </summary>
        public static void ForEachChild(this Transform transform, Action<Transform> action)
        {
            if (transform == null)
            {
                throw new ArgumentNullException(nameof(transform));
            }

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            for (int i = 0; i < transform.childCount; i++)
            {
                action.Invoke(transform.GetChild(i));
            }
        }

        /// <summary>
        /// Finds the first descendant with the specified name using depth-first search.
        /// </summary>
        public static Transform FindChildRecursive(this Transform transform, string childName)
        {
            if (transform == null)
            {
                throw new ArgumentNullException(nameof(transform));
            }

            if (string.IsNullOrEmpty(childName))
            {
                throw new ArgumentException("Child name cannot be null or empty.", nameof(childName));
            }

            return FindChildRecursiveInternal(transform, childName);
        }

        private static Transform FindChildRecursiveInternal(Transform root, string childName)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == childName)
                {
                    return child;
                }

                Transform foundChild = FindChildRecursiveInternal(child, childName);
                if (foundChild != null)
                {
                    return foundChild;
                }
            }

            return null;
        }
    }
}
