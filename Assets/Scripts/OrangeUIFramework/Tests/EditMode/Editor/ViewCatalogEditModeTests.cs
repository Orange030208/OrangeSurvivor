using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Orange.UIFramework.Tests
{
    public sealed class ViewCatalogEditModeTests
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
        public void Validate_ReportsDuplicateIds()
        {
            ViewCatalog catalog = CreateCatalog(
                CreateDefinition("page.duplicate", ViewKind.Page, CreateViewObject<TestPageView>("PageA")),
                CreateDefinition("page.duplicate", ViewKind.Page, CreateViewObject<OtherTestPageView>("PageB")));

            ValidationReport report = catalog.Validate();

            Assert.That(report.HasErrors, Is.True);
            Assert.That(report.ToDisplayString(), Does.Contain("duplicate id 'page.duplicate'"));
        }

        [Test]
        public void Validate_ReportsKindMismatch()
        {
            ViewCatalog catalog = CreateCatalog(
                CreateDefinition("popup.invalid", ViewKind.Popup, CreateViewObject<TestPageView>("PagePrefab")));

            ValidationReport report = catalog.Validate();

            Assert.That(report.HasErrors, Is.True);
            Assert.That(report.ToDisplayString(), Does.Contain("does not match prefab component type"));
        }

        [Test]
        public void Validate_RejectsPartDefinitions()
        {
            ViewCatalog catalog = CreateCatalog(
                CreateDefinition("part.invalid", ViewKind.Part, CreateViewObject<TestPageView>("PartPrefab")));

            ValidationReport report = catalog.Validate();

            Assert.That(report.HasErrors, Is.True);
            Assert.That(report.ToDisplayString(), Does.Contain("must not be registered in the global ViewCatalog"));
        }

        [Test]
        public void Validate_ReportsPrefabWithoutViewBase()
        {
            GameObject prefab = new GameObject("PlainPrefab");
            cleanupObjects.Add(prefab);
            ViewCatalog catalog = CreateCatalog(CreateDefinition("page.missingView", ViewKind.Page, prefab));

            ValidationReport report = catalog.Validate();

            Assert.That(report.HasErrors, Is.True);
            Assert.That(report.ToDisplayString(), Does.Contain("does not contain ViewBase on the root"));
        }

        [Test]
        public void OrangeCatalog_RegistersMigratedMenuAndGamingPages()
        {
            ViewCatalog catalog = AssetDatabase.LoadAssetAtPath<ViewCatalog>("Assets/Resources/Data/UI/OrangeUIViewCatalog.asset");

            Assert.That(catalog, Is.Not.Null);
            ValidationReport report = catalog.Validate();
            Assert.That(report.HasErrors, Is.False, report.ToDisplayString());
            Assert.That(catalog.TryFindByType<MenuUIPage>(out ViewDefinition menuDefinition), Is.True);
            Assert.That(menuDefinition.Id, Is.EqualTo("page.menu"));
            Assert.That(menuDefinition.Layer, Is.EqualTo(ViewLayer.Page));
            Assert.That(catalog.TryFindByType<GamingUIPage>(out ViewDefinition gamingDefinition), Is.True);
            Assert.That(gamingDefinition.Id, Is.EqualTo("page.gaming"));
            Assert.That(gamingDefinition.Layer, Is.EqualTo(ViewLayer.Hud));
        }

        private ViewCatalog CreateCatalog(params ViewDefinition[] definitions)
        {
            ViewCatalog catalog = ScriptableObject.CreateInstance<ViewCatalog>();
            cleanupObjects.Add(catalog);
            TestReflection.SetField(catalog, "views", new List<ViewDefinition>(definitions));
            return catalog;
        }

        private ViewDefinition CreateDefinition(string id, ViewKind kind, GameObject prefab)
        {
            ViewDefinition definition = new ViewDefinition();
            TestReflection.SetField(definition, "id", id);
            TestReflection.SetField(definition, "kind", kind);
            TestReflection.SetField(definition, "layer", kind == ViewKind.Popup ? ViewLayer.Popup : ViewLayer.Page);
            TestReflection.SetField(definition, "prefab", prefab);
            return definition;
        }

        private GameObject CreateViewObject<TView>(string name)
            where TView : ViewBase
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.AddComponent<TView>();
            cleanupObjects.Add(gameObject);
            return gameObject;
        }
    }
}
