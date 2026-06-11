using UnityEngine;
using UnityEngine.UI;

namespace Orange.UIFramework
{
    public sealed class FloatingViewPositioner : IFloatingViewPositioner
    {
        private static readonly FloatingViewAnchor[] bottomRightCandidates =
        {
            FloatingViewAnchor.BottomRight,
            FloatingViewAnchor.TopRight,
            FloatingViewAnchor.BottomLeft,
            FloatingViewAnchor.TopLeft
        };

        private static readonly FloatingViewAnchor[] topRightCandidates =
        {
            FloatingViewAnchor.TopRight,
            FloatingViewAnchor.BottomRight,
            FloatingViewAnchor.TopLeft,
            FloatingViewAnchor.BottomLeft
        };

        private static readonly FloatingViewAnchor[] bottomLeftCandidates =
        {
            FloatingViewAnchor.BottomLeft,
            FloatingViewAnchor.TopLeft,
            FloatingViewAnchor.BottomRight,
            FloatingViewAnchor.TopRight
        };

        private static readonly FloatingViewAnchor[] topLeftCandidates =
        {
            FloatingViewAnchor.TopLeft,
            FloatingViewAnchor.BottomLeft,
            FloatingViewAnchor.TopRight,
            FloatingViewAnchor.BottomRight
        };

        private static readonly FloatingViewAnchor[] centerCandidates =
        {
            FloatingViewAnchor.Center,
            FloatingViewAnchor.BottomRight,
            FloatingViewAnchor.TopRight,
            FloatingViewAnchor.BottomLeft,
            FloatingViewAnchor.TopLeft
        };

        private static readonly Vector3[] worldCorners = new Vector3[4];
        private static readonly Vector3[] localCorners = new Vector3[4];

        public FloatingViewPlacement Place(
            RectTransform view,
            RectTransform layerRoot,
            Canvas rootCanvas,
            RectTransform anchor,
            bool useScreenPosition,
            Vector2 screenPosition,
            Vector2 offset,
            float margin,
            FloatingViewAnchor preferredAnchor = FloatingViewAnchor.BottomRight,
            bool rebuildLayout = false)
        {
            if (view == null)
            {
                throw new System.ArgumentNullException(nameof(view));
            }

            if (layerRoot == null)
            {
                throw new System.ArgumentNullException(nameof(layerRoot));
            }

            if (rebuildLayout)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(view);
            }

            Camera camera = ResolveCamera(rootCanvas);
            Vector2 origin = ResolveOrigin(anchor, useScreenPosition, screenPosition, layerRoot, camera);
            Rect boundsRect = CreateBoundsRect(layerRoot, margin);
            FloatingViewAnchor resolvedAnchor = ResolveAnchor(view, origin, offset, boundsRect, preferredAnchor);
            Vector2 requestedPosition = CalculateAnchoredPosition(view, origin, offset, resolvedAnchor);
            Rect requestedRect = CalculateLocalRect(view, requestedPosition);
            Vector2 anchoredPosition = ClampPosition(requestedPosition, requestedRect, boundsRect, out bool wasClamped);

            view.anchoredPosition = anchoredPosition;
            Rect localRect = CalculateLocalRect(view, anchoredPosition);
            bool wasFlipped = resolvedAnchor != preferredAnchor;

            return new FloatingViewPlacement(
                requestedPosition,
                anchoredPosition,
                preferredAnchor,
                resolvedAnchor,
                wasFlipped,
                wasClamped,
                localRect,
                boundsRect);
        }

        private static Camera ResolveCamera(Canvas rootCanvas)
        {
            return rootCanvas != null && rootCanvas.renderMode == RenderMode.ScreenSpaceCamera
                ? rootCanvas.worldCamera
                : null;
        }

        private static Vector2 ResolveOrigin(
            RectTransform anchor,
            bool useScreenPosition,
            Vector2 screenPosition,
            RectTransform layerRoot,
            Camera camera)
        {
            if (anchor != null)
            {
                Vector3 worldCenter = anchor.TransformPoint(anchor.rect.center);
                Vector2 anchorScreenPosition = RectTransformUtility.WorldToScreenPoint(camera, worldCenter);
                return ScreenToLayerPosition(anchorScreenPosition, layerRoot, camera);
            }

            return useScreenPosition
                ? ScreenToLayerPosition(screenPosition, layerRoot, camera)
                : Vector2.zero;
        }

        private static Vector2 ScreenToLayerPosition(Vector2 screenPosition, RectTransform layerRoot, Camera camera)
        {
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(layerRoot, screenPosition, camera, out Vector2 localPoint)
                ? localPoint
                : Vector2.zero;
        }

        private static Rect CreateBoundsRect(RectTransform layerRoot, float margin)
        {
            Rect rect = layerRoot.rect;
            float safeMargin = Mathf.Max(0f, margin);
            float width = Mathf.Max(0f, rect.width - safeMargin * 2f);
            float height = Mathf.Max(0f, rect.height - safeMargin * 2f);

            return new Rect(
                rect.xMin + safeMargin,
                rect.yMin + safeMargin,
                width,
                height);
        }

        private static FloatingViewAnchor ResolveAnchor(
            RectTransform view,
            Vector2 origin,
            Vector2 offset,
            Rect boundsRect,
            FloatingViewAnchor preferredAnchor)
        {
            FloatingViewAnchor[] candidates = CreateAnchorCandidates(preferredAnchor);
            for (int i = 0; i < candidates.Length; i++)
            {
                FloatingViewAnchor candidate = candidates[i];
                Vector2 position = CalculateAnchoredPosition(view, origin, offset, candidate);
                Rect rect = CalculateLocalRect(view, position);
                if (Contains(boundsRect, rect))
                {
                    return candidate;
                }
            }

            return preferredAnchor;
        }

        private static FloatingViewAnchor[] CreateAnchorCandidates(FloatingViewAnchor preferredAnchor)
        {
            switch (preferredAnchor)
            {
                case FloatingViewAnchor.TopRight:
                    return topRightCandidates;
                case FloatingViewAnchor.BottomLeft:
                    return bottomLeftCandidates;
                case FloatingViewAnchor.TopLeft:
                    return topLeftCandidates;
                case FloatingViewAnchor.Center:
                    return centerCandidates;
                default:
                    return bottomRightCandidates;
            }
        }

        private static Vector2 CalculateAnchoredPosition(
            RectTransform view,
            Vector2 origin,
            Vector2 offset,
            FloatingViewAnchor anchor)
        {
            Rect rect = view.rect;
            Vector2 pivot = view.pivot;
            float x;
            float y;

            switch (anchor)
            {
                case FloatingViewAnchor.TopRight:
                    x = origin.x + offset.x + rect.width * pivot.x;
                    y = origin.y + offset.y + rect.height * pivot.y;
                    break;
                case FloatingViewAnchor.BottomLeft:
                    x = origin.x + offset.x - rect.width * (1f - pivot.x);
                    y = origin.y + offset.y - rect.height * (1f - pivot.y);
                    break;
                case FloatingViewAnchor.TopLeft:
                    x = origin.x + offset.x - rect.width * (1f - pivot.x);
                    y = origin.y + offset.y + rect.height * pivot.y;
                    break;
                case FloatingViewAnchor.Center:
                    x = origin.x + offset.x + rect.width * (pivot.x - 0.5f);
                    y = origin.y + offset.y + rect.height * (pivot.y - 0.5f);
                    break;
                default:
                    x = origin.x + offset.x + rect.width * pivot.x;
                    y = origin.y + offset.y - rect.height * (1f - pivot.y);
                    break;
            }

            return new Vector2(x, y);
        }

        private static Rect CalculateLocalRect(RectTransform view, Vector2 anchoredPosition)
        {
            Vector2 previousPosition = view.anchoredPosition;
            view.anchoredPosition = anchoredPosition;
            view.GetWorldCorners(worldCorners);
            view.anchoredPosition = previousPosition;

            RectTransform parent = view.parent as RectTransform;
            for (int i = 0; i < worldCorners.Length; i++)
            {
                localCorners[i] = parent != null
                    ? parent.InverseTransformPoint(worldCorners[i])
                    : worldCorners[i];
            }

            return CreateRectFromCorners(localCorners);
        }

        private static Rect CreateRectFromCorners(Vector3[] corners)
        {
            float minX = corners[0].x;
            float maxX = corners[0].x;
            float minY = corners[0].y;
            float maxY = corners[0].y;

            for (int i = 1; i < corners.Length; i++)
            {
                Vector3 corner = corners[i];
                minX = Mathf.Min(minX, corner.x);
                maxX = Mathf.Max(maxX, corner.x);
                minY = Mathf.Min(minY, corner.y);
                maxY = Mathf.Max(maxY, corner.y);
            }

            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }

        private static bool Contains(Rect boundsRect, Rect rect)
        {
            return rect.xMin >= boundsRect.xMin &&
                   rect.xMax <= boundsRect.xMax &&
                   rect.yMin >= boundsRect.yMin &&
                   rect.yMax <= boundsRect.yMax;
        }

        private static Vector2 ClampPosition(
            Vector2 position,
            Rect rect,
            Rect boundsRect,
            out bool wasClamped)
        {
            Vector2 delta = Vector2.zero;

            if (rect.xMin < boundsRect.xMin)
            {
                delta.x = boundsRect.xMin - rect.xMin;
            }
            else if (rect.xMax > boundsRect.xMax)
            {
                delta.x = boundsRect.xMax - rect.xMax;
            }

            if (rect.yMin < boundsRect.yMin)
            {
                delta.y = boundsRect.yMin - rect.yMin;
            }
            else if (rect.yMax > boundsRect.yMax)
            {
                delta.y = boundsRect.yMax - rect.yMax;
            }

            wasClamped = delta != Vector2.zero;
            return position + delta;
        }
    }
}
