using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Orange.UIFramework.Tests
{
    public sealed class LocalizationServiceEditModeTests
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
        public void GetText_ReplacesSimpleArguments()
        {
            LocalizationService service = CreateService(
                "zh-CN",
                CreateTable("zh-CN", new LocalizationEntry("shop.cost", "Cost {cost}")));

            string text = service.GetText("shop.cost", new Dictionary<string, object>
            {
                ["cost"] = 25
            });

            Assert.That(text, Is.EqualTo("Cost 25"));
        }

        [Test]
        public void GetText_FallsBackToDefaultLanguage()
        {
            LocalizationService service = CreateService(
                "zh-CN",
                CreateTable("zh-CN", new LocalizationEntry("shop.buy", "Buy Default")),
                CreateTable("en-US"));

            service.SetLanguageAsync("en-US").GetAwaiter().GetResult();

            Assert.That(service.GetText("shop.buy"), Is.EqualTo("Buy Default"));
        }

        [Test]
        public void GetText_ReturnsKeyWhenMissing()
        {
            LocalizationService service = CreateService("zh-CN", CreateTable("zh-CN"));

            Assert.That(service.GetText("missing.key"), Is.EqualTo("missing.key"));
        }

        private LocalizationService CreateService(string defaultLanguage, params LocalizationTable[] tables)
        {
            GameObject gameObject = new GameObject("LocalizationService");
            cleanupObjects.Add(gameObject);

            LocalizationService service = gameObject.AddComponent<LocalizationService>();
            TestReflection.SetField(service, "defaultLanguage", defaultLanguage);
            TestReflection.SetField(service, "dontDestroyOnLoad", false);
            TestReflection.SetField(service, "tables", new List<LocalizationTable>(tables));
            TestReflection.InvokePrivate(service, "Awake");
            return service;
        }

        private LocalizationTable CreateTable(string language, params LocalizationEntry[] entries)
        {
            LocalizationTable table = ScriptableObject.CreateInstance<LocalizationTable>();
            cleanupObjects.Add(table);
            TestReflection.SetField(table, "language", language);
            TestReflection.SetField(table, "entries", new List<LocalizationEntry>(entries));
            return table;
        }
    }
}
