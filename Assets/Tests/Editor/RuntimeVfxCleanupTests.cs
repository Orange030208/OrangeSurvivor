using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class RuntimeVfxCleanupTests
{
    private GameObject prefab;

    [TearDown]
    public void TearDown()
    {
        GameObject[] activeVfx = RuntimeVfx.CreateActiveSnapshot();
        for (int i = 0; i < activeVfx.Length; i++)
        {
            RuntimeVfx.ReleaseForWaveCleanup(activeVfx[i]);
        }

        if (prefab != null)
        {
            Object.DestroyImmediate(prefab);
        }
    }

    [Test]
    public void SpawnedVfxWithoutLifetimeIsTrackedForWaveCleanup()
    {
        prefab = new GameObject("runtime_vfx_test_prefab");
        GameObject instance = RuntimeVfx.Spawn(prefab, Vector3.zero, Quaternion.identity);

        GameObject[] snapshot = RuntimeVfx.CreateActiveSnapshot();

        Assert.Contains(instance, snapshot);
    }

    [Test]
    public void ReleaseForWaveCleanupRemovesVfxFromRuntimeSnapshot()
    {
        prefab = new GameObject("runtime_vfx_release_test_prefab");
        GameObject instance = RuntimeVfx.Spawn(prefab, Vector3.zero, Quaternion.identity);

        RuntimeVfx.ReleaseForWaveCleanup(instance);

        GameObject[] snapshot = RuntimeVfx.CreateActiveSnapshot();
        CollectionAssert.DoesNotContain(snapshot, instance);
    }
}
