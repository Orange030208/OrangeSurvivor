using UnityEngine;

namespace Orange.UIFramework
{
    public interface IFloatingViewPositioner
    {
        FloatingViewPlacement Place(
            RectTransform view,
            RectTransform layerRoot,
            Canvas rootCanvas,
            RectTransform anchor,
            bool useScreenPosition,
            Vector2 screenPosition,
            Vector2 offset,
            float margin,
            FloatingViewAnchor preferredAnchor = FloatingViewAnchor.BottomRight,
            bool rebuildLayout = false);
    }
}
