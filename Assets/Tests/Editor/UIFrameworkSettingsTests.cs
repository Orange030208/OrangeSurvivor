using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Orange.UIFramework;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIFrameworkSettingsTests
{
    private readonly List<Object> createdObjects = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        for (int i = createdObjects.Count - 1; i >= 0; i--)
        {
            Object createdObject = createdObjects[i];
            if (createdObject != null)
            {
                Object.DestroyImmediate(createdObject);
            }
        }

        createdObjects.Clear();
    }

    [Test]
    public void Validate_AllowsMissingPopupOutsideClickBlockerPrefab()
    {
        UIFrameworkSettings settings = CreateSettings(null);

        ValidationReport report = settings.Validate();

        Assert.IsFalse(report.HasErrors, report.ToDisplayString());
    }

    [Test]
    public void Validate_ReportsPopupOutsideClickBlockerPrefabWithoutButton()
    {
        GameObject prefab = CreateBlockerPrefab("Missing Button");
        Image image = prefab.AddComponent<Image>();
        image.raycastTarget = true;

        UIFrameworkSettings settings = CreateSettings(prefab);

        ValidationReport report = settings.Validate();

        Assert.IsTrue(report.HasErrors);
        StringAssert.Contains("must contain an enabled Graphic with raycastTarget enabled and an enabled Button", report.ToDisplayString());
    }

    [Test]
    public void Validate_ReportsPopupOutsideClickBlockerPrefabWithoutRaycastGraphic()
    {
        GameObject prefab = CreateBlockerPrefab("Missing Raycast Graphic");
        prefab.AddComponent<Button>();

        UIFrameworkSettings settings = CreateSettings(prefab);

        ValidationReport report = settings.Validate();

        Assert.IsTrue(report.HasErrors);
        StringAssert.Contains("must contain an enabled Graphic with raycastTarget enabled and an enabled Button", report.ToDisplayString());
    }

    [Test]
    public void Validate_ReportsPopupOutsideClickBlockerPrefabWithDisabledButton()
    {
        GameObject prefab = CreateBlockerPrefab("Disabled Button");
        Image image = prefab.AddComponent<Image>();
        image.raycastTarget = true;
        Button button = prefab.AddComponent<Button>();
        button.enabled = false;

        UIFrameworkSettings settings = CreateSettings(prefab);

        ValidationReport report = settings.Validate();

        Assert.IsTrue(report.HasErrors);
        StringAssert.Contains("must contain an enabled Graphic with raycastTarget enabled and an enabled Button", report.ToDisplayString());
    }

    [Test]
    public void Validate_AllowsValidPopupOutsideClickBlockerPrefab()
    {
        GameObject prefab = CreateBlockerPrefab("Valid Blocker");
        Image image = prefab.AddComponent<Image>();
        image.raycastTarget = true;
        prefab.AddComponent<Button>();

        UIFrameworkSettings settings = CreateSettings(prefab);

        ValidationReport report = settings.Validate();

        Assert.IsFalse(report.HasErrors, report.ToDisplayString());
    }

    private UIFrameworkSettings CreateSettings(GameObject blockerPrefab)
    {
        UIFrameworkSettings settings = ScriptableObject.CreateInstance<UIFrameworkSettings>();
        CanvasProfile canvasProfile = ScriptableObject.CreateInstance<CanvasProfile>();
        PopupOutsideClickBlockerSettings blockerSettings = new PopupOutsideClickBlockerSettings();

        createdObjects.Add(settings);
        createdObjects.Add(canvasProfile);

        SetPrivateField(settings, "canvasProfile", canvasProfile);
        SetPrivateField(blockerSettings, "prefab", blockerPrefab);
        SetPrivateField(settings, "popupOutsideClickBlocker", blockerSettings);
        return settings;
    }

    private GameObject CreateBlockerPrefab(string name)
    {
        GameObject prefab = new GameObject(name);
        prefab.AddComponent<RectTransform>();
        createdObjects.Add(prefab);
        return prefab;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field, $"Missing field '{fieldName}' on {target.GetType().Name}.");
        field.SetValue(target, value);
    }
}
