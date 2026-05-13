using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

public class PropertyModifierFeatureTests
{
    [Test]
    public void InstallFeatureAppliesAndRemovesMultiplePropertyModifiersFromSameSource()
    {
        GameObject gameObject = new("PropertyModifierFeatureTests_Player");
        try
        {
            TestEntity entity = gameObject.AddComponent<TestEntity>();
            PropertiesManager propertiesManager = gameObject.AddComponent<PropertiesManager>();
            FeatureHost featureHost = gameObject.AddComponent<FeatureHost>();

            propertiesManager.Initialize(entity);
            featureHost.Initialize(entity);

            FeatureEffectBase[] features =
            {
                new PropertyModifierFeature(new PropModifierData(PropType.Damage, 10f)),
                new PropertyModifierFeature(new PropModifierData(PropType.MaxHealth, 20f))
            };

            Assert.IsTrue(featureHost.InstallFeature("UpgradeCard_Test", features));
            Assert.AreEqual(10f, propertiesManager.GetPropValue(PropType.Damage));
            Assert.AreEqual(20f, propertiesManager.GetPropValue(PropType.MaxHealth));

            Assert.IsTrue(featureHost.RemoveFeature("UpgradeCard_Test"));
            Assert.AreEqual(0f, propertiesManager.GetPropValue(PropType.Damage));
            Assert.AreEqual(0f, propertiesManager.GetPropValue(PropType.MaxHealth));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(gameObject);
        }
    }

    private sealed class TestEntity : Entity, IFeatureEffectsProvider
    {
        public IReadOnlyList<FeatureEffectBase> FeatureEffects => Array.Empty<FeatureEffectBase>();
    }
}
