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

    [Test]
    public void Validate_AllowsScreenSpaceCameraProfileWithoutAssetCamera()
    {
        CanvasProfile canvasProfile = ScriptableObject.CreateInstance<CanvasProfile>();
        createdObjects.Add(canvasProfile);

        SetPrivateField(canvasProfile, "renderMode", RenderMode.ScreenSpaceCamera);
        SetPrivateField(canvasProfile, "uiCamera", null);

        ValidationReport report = canvasProfile.Validate();

        Assert.IsFalse(report.HasErrors, report.ToDisplayString());
    }

    [Test]
    public void Initialize_AppliesExplicitUiCameraToRootCanvas()
    {
        Camera camera = CreateCamera("UI Camera");
        Canvas existingCanvas = CreateCanvas("Root Canvas");
        UIManager manager = CreateManager(CreateSettingsWithProfile(RenderMode.ScreenSpaceCamera), existingCanvas, camera);

        manager.Initialize();

        Assert.AreEqual(RenderMode.ScreenSpaceCamera, manager.RootCanvas.renderMode);
        Assert.AreSame(camera, manager.RootCanvas.worldCamera);
    }

    [Test]
    public void Initialize_UsesProfileCameraAsLegacyFallback()
    {
        Camera camera = CreateCamera("Legacy UI Camera");
        Canvas existingCanvas = CreateCanvas("Legacy Root Canvas");
        UIFrameworkSettings settings = CreateSettingsWithProfile(RenderMode.ScreenSpaceCamera, camera);
        UIManager manager = CreateManager(settings, existingCanvas, null);

        manager.Initialize();

        Assert.AreSame(camera, manager.RootCanvas.worldCamera);
    }

    [Test]
    public void Initialize_ThrowsWhenScreenSpaceCameraHasNoResolvedCamera()
    {
        Canvas existingCanvas = CreateCanvas("Root Canvas Without Camera");
        UIManager manager = CreateManager(CreateSettingsWithProfile(RenderMode.ScreenSpaceCamera), existingCanvas, null);

        MissingReferenceException exception = Assert.Throws<MissingReferenceException>(() => manager.Initialize());

        StringAssert.Contains("has no UI Camera assigned", exception.Message);
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

    private UIFrameworkSettings CreateSettingsWithProfile(RenderMode renderMode, Camera camera = null)
    {
        UIFrameworkSettings settings = ScriptableObject.CreateInstance<UIFrameworkSettings>();
        CanvasProfile canvasProfile = ScriptableObject.CreateInstance<CanvasProfile>();

        createdObjects.Add(settings);
        createdObjects.Add(canvasProfile);

        SetPrivateField(canvasProfile, "renderMode", renderMode);
        SetPrivateField(canvasProfile, "uiCamera", camera);
        SetPrivateField(settings, "canvasProfile", canvasProfile);
        return settings;
    }

    private UIManager CreateManager(UIFrameworkSettings settings, Canvas existingCanvas, Camera camera)
    {
        GameObject gameObject = new GameObject("UIManager");
        UIManager manager = gameObject.AddComponent<UIManager>();
        ViewCatalog catalog = ScriptableObject.CreateInstance<ViewCatalog>();

        createdObjects.Add(gameObject);
        createdObjects.Add(catalog);

        SetPrivateField(manager, "settings", settings);
        SetPrivateField(manager, "catalog", catalog);
        SetPrivateField(manager, "uiCamera", camera);
        SetPrivateField(manager, "existingRootCanvas", existingCanvas);
        return manager;
    }

    private Camera CreateCamera(string name)
    {
        GameObject gameObject = new GameObject(name);
        Camera camera = gameObject.AddComponent<Camera>();
        createdObjects.Add(gameObject);
        return camera;
    }

    private Canvas CreateCanvas(string name)
    {
        GameObject gameObject = new GameObject(name);
        gameObject.AddComponent<RectTransform>();
        Canvas canvas = gameObject.AddComponent<Canvas>();
        createdObjects.Add(gameObject);
        return canvas;
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
