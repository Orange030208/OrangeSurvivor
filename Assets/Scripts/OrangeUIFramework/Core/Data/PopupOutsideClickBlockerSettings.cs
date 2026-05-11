using System;
using UnityEngine;
using UnityEngine.UI;

namespace Orange.UIFramework
{
    [Serializable]
    public sealed class PopupOutsideClickBlockerSettings
    {
        [SerializeField] private GameObject prefab;

        public GameObject Prefab => prefab;

        public void Validate(string ownerName, ValidationReport report)
        {
            if (report == null || prefab == null)
            {
                return;
            }

            if (!TryFindClickableBlockerControls(prefab, out _, out _))
            {
                report.AddError($"UIFrameworkSettings '{ownerName}' PopupOutsideClickBlocker prefab '{prefab.name}' must contain an enabled Graphic with raycastTarget enabled and an enabled Button on the same object or one of its parents.");
            }
        }

        internal static bool TryFindClickableBlockerControls(GameObject root, out Button button, out Graphic graphic)
        {
            button = null;
            graphic = null;
            if (root == null)
            {
                return false;
            }

            Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                Graphic candidate = graphics[i];
                if (candidate != null && candidate.enabled && candidate.raycastTarget)
                {
                    Button candidateButton = candidate.GetComponentInParent<Button>(true);
                    if (candidateButton == null || !candidateButton.enabled)
                    {
                        continue;
                    }

                    button = candidateButton;
                    graphic = candidate;
                    return true;
                }
            }

            return false;
        }
    }
}
