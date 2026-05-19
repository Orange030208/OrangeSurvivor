using System;
using NUnit.Framework;
using UnityEditor;

public sealed class EnemyAttackRangeAssetAuditTests
{
    [Test]
    public void EnemyBasePropGroupsUseDefinedPropTypes()
    {
        string[] guids = AssetDatabase.FindAssets("t:BasePropGroupSO", new[] { "Assets/GameContent/Enemies/Data" });
        Assert.IsNotEmpty(guids, "Expected enemy BasePropGroupSO assets under Assets/GameContent/Enemies/Data.");

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            BasePropGroupSO propGroup = AssetDatabase.LoadAssetAtPath<BasePropGroupSO>(path);
            Assert.NotNull(propGroup, path);

            foreach (BasePropData value in propGroup.Values)
            {
                Assert.IsTrue(
                    Enum.IsDefined(typeof(PropType), value.propType),
                    $"{path} contains undefined prop type value {(int)value.propType}.");
            }
        }
    }

    [Test]
    public void PropPresentationCatalogUsesDefinedPropTypes()
    {
        PropPresentationCatalogSO catalog = AssetDatabase.LoadAssetAtPath<PropPresentationCatalogSO>(
            GameContentAssetPaths.PropPresentationCatalog);

        Assert.NotNull(catalog, GameContentAssetPaths.PropPresentationCatalog);

        foreach (PropPresentationEntry entry in catalog.Entries)
        {
            Assert.IsTrue(
                Enum.IsDefined(typeof(PropType), entry.PropType),
                $"{GameContentAssetPaths.PropPresentationCatalog} contains undefined prop type value {(int)entry.PropType}.");
        }
    }
}
