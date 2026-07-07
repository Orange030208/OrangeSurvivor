using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Orange.Utilities.Tests
{
    public class GameObjectExtensionsTests
    {
        private const int TEST_LAYER = 5;

        [Test]
        public void SetLayerRecursively_AppliesLayerToAllDescendants()
        {
            GameObject root = new("Root");

            try
            {
                GameObject child = new("Child");
                GameObject grandchild = new("Grandchild");

                child.transform.SetParent(root.transform);
                grandchild.transform.SetParent(child.transform);

                root.SetLayerRecursively(TEST_LAYER);

                Assert.AreEqual(TEST_LAYER, root.layer);
                Assert.AreEqual(TEST_LAYER, child.layer);
                Assert.AreEqual(TEST_LAYER, grandchild.layer);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
