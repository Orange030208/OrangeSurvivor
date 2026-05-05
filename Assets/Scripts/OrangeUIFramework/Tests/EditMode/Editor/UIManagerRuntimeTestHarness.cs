using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Orange.UIFramework.Tests
{
    internal sealed class UIManagerRuntimeTestHarness : IDisposable
    {
        private readonly List<UnityEngine.Object> cleanupObjects = new List<UnityEngine.Object>();
        private bool disposed;

        public UIManagerRuntimeTestHarness()
        {
            RuntimeTestViewState.Reset();

            CanvasProfile canvasProfile = CreateCanvasProfile();
            UIFrameworkSettings settings = CreateSettings(canvasProfile);
            ViewCatalog catalog = CreateCatalog();

            GameObject managerObject = new GameObject("UIManagerRuntimeTest");
            managerObject.SetActive(false);
            cleanupObjects.Add(managerObject);

            Manager = managerObject.AddComponent<UIManager>();
            TestReflection.SetField(Manager, "settings", settings);
            TestReflection.SetField(Manager, "catalog", catalog);
            TestReflection.SetField(Manager, "existingRootCanvas", null);

            managerObject.SetActive(true);
            if (Manager.RootCanvas != null)
            {
                RectTransform rootRect = Manager.RootCanvas.GetComponent<RectTransform>();
                if (rootRect != null)
                {
                    rootRect.sizeDelta = new Vector2(1920f, 1080f);
                }

                Canvas.ForceUpdateCanvases();
                cleanupObjects.Add(Manager.RootCanvas.gameObject);
            }
        }

        public UIManager Manager { get; }

        public Button FindBlockerButton(string layerName, string blockerName)
        {
            Transform blocker = Manager.RootCanvas.transform.Find($"Layers/{layerName}/{blockerName}");
            if (blocker == null)
            {
                throw new MissingReferenceException($"Could not find blocker 'Layers/{layerName}/{blockerName}'.");
            }

            Button button = blocker.GetComponent<Button>();
            if (button == null)
            {
                throw new MissingComponentException($"Blocker '{blockerName}' is missing Button.");
            }

            return button;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            for (int i = cleanupObjects.Count - 1; i >= 0; i--)
            {
                UnityEngine.Object target = cleanupObjects[i];
                if (target != null)
                {
                    UnityEngine.Object.DestroyImmediate(target);
                }
            }

            cleanupObjects.Clear();
            RuntimeTestViewState.Reset();
        }

        private CanvasProfile CreateCanvasProfile()
        {
            CanvasProfile canvasProfile = ScriptableObject.CreateInstance<CanvasProfile>();
            cleanupObjects.Add(canvasProfile);
            TestReflection.SetField(canvasProfile, "renderMode", RenderMode.ScreenSpaceOverlay);
            TestReflection.SetField(canvasProfile, "referenceResolution", new Vector2(1920f, 1080f));
            return canvasProfile;
        }

        private UIFrameworkSettings CreateSettings(CanvasProfile canvasProfile)
        {
            UIFrameworkSettings settings = ScriptableObject.CreateInstance<UIFrameworkSettings>();
            cleanupObjects.Add(settings);
            TestReflection.SetField(settings, "canvasProfile", canvasProfile);
            TestReflection.SetField(settings, "dontDestroyOnLoad", false);
            TestReflection.SetField(settings, "enablePooling", true);
            TestReflection.SetField(settings, "maxCachedInstancesPerView", 4);
            return settings;
        }

        private ViewCatalog CreateCatalog()
        {
            ViewCatalog catalog = ScriptableObject.CreateInstance<ViewCatalog>();
            cleanupObjects.Add(catalog);

            List<ViewDefinition> definitions = new List<ViewDefinition>
            {
                CreateDefinition<RuntimeTestPageView>("page.runtime", ViewKind.Page, ViewLayer.Page, false, true),
                CreateDefinition<SecondRuntimeTestPageView>("page.secondRuntime", ViewKind.Page, ViewLayer.Page, false, true),
                CreateDefinition<RuntimeSlowOpeningPageView>("page.slowOpening", ViewKind.Page, ViewLayer.Page, false, true),
                CreateDefinition<RuntimeSlowClosingPageView>("page.slowClosing", ViewKind.Page, ViewLayer.Page, false, true),
                CreateDefinition<RuntimeTestPopupView>("popup.runtime", ViewKind.Popup, ViewLayer.Popup, false, true),
                CreateDefinition<RuntimeTestModalView>("modal.runtime", ViewKind.Modal, ViewLayer.Modal, false, true, closeOnBackgroundClick: true),
                CreateDefinition<RuntimeTestTooltipView>("tooltip.runtime", ViewKind.Tooltip, ViewLayer.Tooltip, true, true)
            };

            TestReflection.SetField(catalog, "views", definitions);
            return catalog;
        }

        private ViewDefinition CreateDefinition<TView>(
            string id,
            ViewKind kind,
            ViewLayer layer,
            bool singleton,
            bool cacheOnClose,
            bool closeOnBackgroundClick = false)
            where TView : ViewBase
        {
            GameObject prefab = CreateViewPrefab<TView>($"{typeof(TView).Name}Prefab", kind);
            ViewDefinition definition = new ViewDefinition();
            TestReflection.SetField(definition, "id", id);
            TestReflection.SetField(definition, "kind", kind);
            TestReflection.SetField(definition, "layer", layer);
            TestReflection.SetField(definition, "prefab", prefab);
            TestReflection.SetField(definition, "singleton", singleton);
            TestReflection.SetField(definition, "cacheOnClose", cacheOnClose);
            TestReflection.SetField(definition, "closeOnBackgroundClick", closeOnBackgroundClick);
            TestReflection.SetField(definition, "maxCachedInstancesOverride", 4);
            return definition;
        }

        private GameObject CreateViewPrefab<TView>(string name, ViewKind kind)
            where TView : ViewBase
        {
            GameObject prefab = new GameObject(name, typeof(RectTransform));
            prefab.SetActive(false);
            cleanupObjects.Add(prefab);

            RectTransform rectTransform = prefab.GetComponent<RectTransform>();
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = kind == ViewKind.Tooltip
                ? new Vector2(320f, 180f)
                : new Vector2(240f, 140f);

            prefab.AddComponent<CanvasGroup>();
            prefab.AddComponent<TView>();
            return prefab;
        }
    }
}
