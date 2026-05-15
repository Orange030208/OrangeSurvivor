using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public sealed class CrashGuardTests
{
    private readonly List<Object> createdObjects = new();

    [TearDown]
    public void TearDown()
    {
        LogAssert.ignoreFailingMessages = false;

        for (int i = 0; i < createdObjects.Count; i++)
        {
            if (createdObjects[i] != null)
            {
                Object.DestroyImmediate(createdObjects[i]);
            }
        }

        createdObjects.Clear();
    }

    [Test]
    public void PropertiesManagerUsesDefaultsWhenPropGroupProviderIsMissing()
    {
        GameObject gameObject = CreateGameObject("no_prop_provider");
        PlainEntity entity = gameObject.AddComponent<PlainEntity>();
        PropertiesManager propertiesManager = gameObject.AddComponent<PropertiesManager>();

        Assert.DoesNotThrow(() => propertiesManager.Initialize(entity));
        Assert.AreEqual(1f, propertiesManager.GetPropValue(PropType.ProjectileSpeed));
    }

    [Test]
    public void PropertiesManagerUsesDefaultsWhenBasePropGroupIsMissing()
    {
        GameObject gameObject = CreateGameObject("no_base_prop_group");
        NullBasePropEntity entity = gameObject.AddComponent<NullBasePropEntity>();
        PropertiesManager propertiesManager = gameObject.AddComponent<PropertiesManager>();

        LogAssert.Expect(
            LogType.Warning,
            new Regex(".*no BasePropsGroup.*default base properties.*"));

        Assert.DoesNotThrow(() => propertiesManager.Initialize(entity));
        Assert.AreEqual(1f, propertiesManager.GetPropValue(PropType.ProjectileSpeed));
    }

    [Test]
    public void AccessoryManagerAllowsMissingInitialAccessoryProvider()
    {
        GameObject gameObject = CreateGameObject("no_initial_accessory_provider");
        PlainEntity entity = gameObject.AddComponent<PlainEntity>();
        AccessoryManager accessoryManager = gameObject.AddComponent<AccessoryManager>();

        Assert.DoesNotThrow(() => accessoryManager.Initialize(entity));
        Assert.AreEqual(0, accessoryManager.EquippedAccessoryList.Count);
    }

    [Test]
    public void AccessoryManagerAllowsNullInitialAccessoryList()
    {
        GameObject gameObject = CreateGameObject("null_initial_accessory_list");
        NullInitialAccessoryEntity entity = gameObject.AddComponent<NullInitialAccessoryEntity>();
        AccessoryManager accessoryManager = gameObject.AddComponent<AccessoryManager>();

        Assert.DoesNotThrow(() => accessoryManager.Initialize(entity));
        Assert.AreEqual(0, accessoryManager.EquippedAccessoryList.Count);
    }

    [Test]
    public void InvalidGameContentCatalogPreventsBootstrapInitialization()
    {
        GameObject gameObject = CreateGameObject("invalid_catalog_bootstrap");
        GameContentBootstrap bootstrap = gameObject.AddComponent<GameContentBootstrap>();
        GameContentCatalogSO catalog = ScriptableObject.CreateInstance<GameContentCatalogSO>();
        createdObjects.Add(catalog);
        SetPrivateField(bootstrap, "catalog", catalog);

        LogAssert.ignoreFailingMessages = true;

        Assert.IsFalse(bootstrap.TryInitializeRuntime());
    }

    [Test]
    public void CatalogValidationRequiresWaveSpawnPool()
    {
        GameContentCatalogSO catalog = ScriptableObject.CreateInstance<GameContentCatalogSO>();
        createdObjects.Add(catalog);
        List<string> errors = new();

        Assert.IsFalse(catalog.ValidateCatalog(errors));
        Assert.IsTrue(errors.Exists(error => error.Contains("waveSpawnPool")));
    }

    private GameObject CreateGameObject(string name)
    {
        GameObject gameObject = new(name);
        createdObjects.Add(gameObject);
        return gameObject;
    }

    private static void SetPrivateField<TTarget>(TTarget target, string fieldName, object value)
    {
        System.Reflection.FieldInfo field = typeof(TTarget).GetField(
            fieldName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Missing private field '{fieldName}' on {typeof(TTarget).Name}.");
        field.SetValue(target, value);
    }

    private sealed class PlainEntity : Entity
    {
    }

    private sealed class NullBasePropEntity : Entity, IPropGroupProvider
    {
        public BasePropGroupSO BasePropsGroup => null;
    }

    private sealed class NullInitialAccessoryEntity : Entity, IInitialAccessoryProvider
    {
        public IReadOnlyList<AccessoryDataSO> InitialAccessories => null;
    }
}
