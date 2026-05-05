using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Orange.UIFramework.Tests
{
    public sealed class FloatingViewPositionerEditModeTests
    {
        private readonly List<Object> cleanupObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = cleanupObjects.Count - 1; i >= 0; i--)
            {
                if (cleanupObjects[i] != null)
                {
                    Object.DestroyImmediate(cleanupObjects[i]);
                }
            }

            cleanupObjects.Clear();
        }

        [Test]
        public void Place_FlipsWhenPreferredAnchorWouldLeaveBounds()
        {
            TestLayout layout = CreateLayout(new Vector2(200f, 200f), new Vector2(40f, 40f));
            FloatingViewPositioner positioner = new FloatingViewPositioner();

            FloatingViewPlacement placement = positioner.Place(
                layout.View,
                layout.LayerRoot,
                layout.Canvas,
                anchor: null,
                useScreenPosition: false,
                screenPosition: default,
                offset: new Vector2(90f, 0f),
                margin: 0f,
                preferredAnchor: FloatingViewAnchor.BottomRight);

            Assert.That(placement.HasValue, Is.True);
            Assert.That(placement.ResolvedAnchor, Is.EqualTo(FloatingViewAnchor.BottomLeft));
            Assert.That(placement.WasFlipped, Is.True);
            Assert.That(placement.WasClamped, Is.False);
        }

        [Test]
        public void Place_ClampsOversizedViewIntoBounds()
        {
            TestLayout layout = CreateLayout(new Vector2(200f, 200f), new Vector2(300f, 300f));
            FloatingViewPositioner positioner = new FloatingViewPositioner();

            FloatingViewPlacement placement = positioner.Place(
                layout.View,
                layout.LayerRoot,
                layout.Canvas,
                anchor: null,
                useScreenPosition: false,
                screenPosition: default,
                offset: Vector2.zero,
                margin: 10f,
                preferredAnchor: FloatingViewAnchor.BottomRight);

            Assert.That(placement.HasValue, Is.True);
            Assert.That(placement.WasClamped, Is.True);
            Assert.That(placement.BoundsRect.xMin, Is.EqualTo(-90f).Within(0.01f));
            Assert.That(placement.BoundsRect.xMax, Is.EqualTo(90f).Within(0.01f));
        }

        private TestLayout CreateLayout(Vector2 layerSize, Vector2 viewSize)
        {
            GameObject canvasObject = new GameObject("CanvasRoot", typeof(RectTransform), typeof(Canvas));
            cleanupObjects.Add(canvasObject);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            RectTransform layerRoot = CreateRect("LayerRoot", canvasObject.transform, layerSize);
            RectTransform view = CreateRect("FloatingView", layerRoot, viewSize);
            view.pivot = new Vector2(0f, 1f);
            view.anchorMin = new Vector2(0.5f, 0.5f);
            view.anchorMax = new Vector2(0.5f, 0.5f);
            view.anchoredPosition = Vector2.zero;

            return new TestLayout(canvas, layerRoot, view);
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 size)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.localScale = Vector3.one;
            return rectTransform;
        }

        private readonly struct TestLayout
        {
            public TestLayout(Canvas canvas, RectTransform layerRoot, RectTransform view)
            {
                Canvas = canvas;
                LayerRoot = layerRoot;
                View = view;
            }

            public Canvas Canvas { get; }
            public RectTransform LayerRoot { get; }
            public RectTransform View { get; }
        }
    }
}
