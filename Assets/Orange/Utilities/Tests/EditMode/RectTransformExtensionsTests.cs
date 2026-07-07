using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Orange.Utilities.Tests
{
    public class RectTransformExtensionsTests
    {
        [Test]
        public void StretchToParent_StretchesAnchorsAndClearsOffsets()
        {
            GameObject parent = new("Parent", typeof(RectTransform));
            GameObject child = new("Child", typeof(RectTransform));

            try
            {
                RectTransform parentRect = parent.GetComponent<RectTransform>();
                RectTransform childRect = child.GetComponent<RectTransform>();

                childRect.SetParent(parentRect, false);
                childRect.anchorMin = new Vector2(0.25f, 0.25f);
                childRect.anchorMax = new Vector2(0.75f, 0.75f);
                childRect.offsetMin = new Vector2(10f, 20f);
                childRect.offsetMax = new Vector2(-30f, -40f);

                childRect.StretchToParent();

                Assert.AreEqual(Vector2.zero, childRect.anchorMin);
                Assert.AreEqual(Vector2.one, childRect.anchorMax);
                Assert.AreEqual(Vector2.zero, childRect.offsetMin);
                Assert.AreEqual(Vector2.zero, childRect.offsetMax);
            }
            finally
            {
                Object.DestroyImmediate(child);
                Object.DestroyImmediate(parent);
            }
        }
    }
}
