using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Orange.Utilities.Tests
{
    public class TransformExtensionsTests
    {
        [Test]
        public void Clear_RemovesAllChildren()
        {
            GameObject root = new("Root");

            try
            {
                new GameObject("ChildA").transform.SetParent(root.transform);
                new GameObject("ChildB").transform.SetParent(root.transform);
                new GameObject("ChildC").transform.SetParent(root.transform);

                root.transform.Clear(immediate: true);

                Assert.AreEqual(0, root.transform.childCount);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ResetLocal_ResetsTransformValues()
        {
            GameObject root = new("Root");

            try
            {
                root.transform.localPosition = new Vector3(1f, 2f, 3f);
                root.transform.localRotation = Quaternion.Euler(10f, 20f, 30f);
                root.transform.localScale = new Vector3(2f, 3f, 4f);

                root.transform.ResetLocal();

                Assert.AreEqual(Vector3.zero, root.transform.localPosition);
                Assert.AreEqual(Quaternion.identity, root.transform.localRotation);
                Assert.AreEqual(Vector3.one, root.transform.localScale);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
