using NUnit.Framework;
using Orange.UIFramework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GameSettingsAndInputTests
{
    private const string MASTER_VOLUME_KEY = "Settings.MasterVolume";
    private const string SFX_VOLUME_KEY = "Settings.SfxVolume";
    private const string MUSIC_VOLUME_KEY = "Settings.MusicVolume";
    private const string RESOLUTION_WIDTH_KEY = "Settings.ResolutionWidth";
    private const string RESOLUTION_HEIGHT_KEY = "Settings.ResolutionHeight";
    private const string WINDOW_MODE_KEY = "Settings.WindowMode";
    private const string LANGUAGE_CODE_KEY = "Settings.LanguageCode";
    private const string INPUT_REBINDS_JSON_KEY = "Settings.InputRebindsJson";
    private const string SETTINGS_PANEL_PREFAB_PATH = "Assets/GameContent/UI/Prefabs/Pages/Setting/Settings Panel.prefab";
    private const string SETTINGS_UI_FONT_PATH = "Assets/GameContent/UI/Fonts/HYPixel11pxU-2 SDF.asset";
    private const string DISPLAY_CONFIRM_MODAL_PREFAB_PATH = "Assets/GameContent/UI/Prefabs/Modals/DisplayConfirm Modal.prefab";
    private const string VIEW_CATALOG_PATH = "Assets/GameContent/UI/Data/OrangeUIViewCatalog.asset";
    private const string PC_PROFILE_PATH = "Assets/GameContent/UI/Data/Settings/PC Settings Profile.asset";
    private const string MOBILE_PROFILE_PATH = "Assets/GameContent/UI/Data/Settings/Mobile Settings Profile.asset";
    private const string CONSOLE_PROFILE_PATH = "Assets/GameContent/UI/Data/Settings/Console Settings Profile.asset";
    private static readonly string[] SETTINGS_PANEL_HOST_PREFABS =
    {
        "Assets/GameContent/UI/Prefabs/Pages/UI Menu.prefab",
        "Assets/GameContent/UI/Prefabs/Pages/UI Pause.prefab"
    };

    private static readonly PrefSpec[] settingsKeys =
    {
        new PrefSpec(MASTER_VOLUME_KEY, PrefValueType.Float),
        new PrefSpec(SFX_VOLUME_KEY, PrefValueType.Float),
        new PrefSpec(MUSIC_VOLUME_KEY, PrefValueType.Float),
        new PrefSpec(RESOLUTION_WIDTH_KEY, PrefValueType.Int),
        new PrefSpec(RESOLUTION_HEIGHT_KEY, PrefValueType.Int),
        new PrefSpec(WINDOW_MODE_KEY, PrefValueType.Int),
        new PrefSpec(LANGUAGE_CODE_KEY, PrefValueType.String),
        new PrefSpec(INPUT_REBINDS_JSON_KEY, PrefValueType.String)
    };

    private SavedPrefs savedPrefs;

    [SetUp]
    public void SetUp()
    {
        savedPrefs = SavedPrefs.Capture(settingsKeys);
        ClearSettingsPrefs();
    }

    [TearDown]
    public void TearDown()
    {
        ClearSettingsPrefs();
        savedPrefs.Restore();
    }

    [Test]
    public void GameSettingsStateDefaultUsesSupportedDisplayAndLanguage()
    {
        GameSettingsState state = GameSettingsState.Default();

        Assert.GreaterOrEqual(state.ResolutionWidth, DisplaySettingsService.MIN_WIDTH);
        Assert.GreaterOrEqual(state.ResolutionHeight, DisplaySettingsService.MIN_HEIGHT);
        Assert.AreEqual(FullScreenMode.FullScreenWindow, state.WindowMode);
        Assert.AreEqual(GameSettingsService.DEFAULT_LANGUAGE_CODE, state.LanguageCode);
        Assert.AreEqual(string.Empty, state.InputRebindsJson);
    }

    [Test]
    public void GameSettingsServiceSavesLoadsAndSanitizesState()
    {
        GameSettingsState state = GameSettingsState.Default();
        state.MasterVolume = 2f;
        state.SfxVolume = -1f;
        state.MusicVolume = 0.25f;
        state.ResolutionWidth = 1024;
        state.ResolutionHeight = 600;
        state.WindowMode = FullScreenMode.MaximizedWindow;
        state.LanguageCode = "en-US";
        state.InputRebindsJson = "{\"bindings\":[]}";

        GameSettingsService.Save(state);
        GameSettingsState loaded = GameSettingsService.Load();

        Assert.AreEqual(1f, loaded.MasterVolume);
        Assert.AreEqual(0f, loaded.SfxVolume);
        Assert.AreEqual(0.25f, loaded.MusicVolume);
        Assert.GreaterOrEqual(loaded.ResolutionWidth, DisplaySettingsService.MIN_WIDTH);
        Assert.GreaterOrEqual(loaded.ResolutionHeight, DisplaySettingsService.MIN_HEIGHT);
        Assert.AreEqual(FullScreenMode.FullScreenWindow, loaded.WindowMode);
        Assert.AreEqual(GameSettingsService.ENGLISH_LANGUAGE_CODE, loaded.LanguageCode);
        Assert.AreEqual("{\"bindings\":[]}", loaded.InputRebindsJson);
    }

    [Test]
    public void DisplayResolutionOptionsFilterDeduplicateAndSortDescending()
    {
        DisplayResolutionOption[] source =
        {
            new DisplayResolutionOption(800, 600),
            new DisplayResolutionOption(1920, 1080),
            new DisplayResolutionOption(1280, 720),
            new DisplayResolutionOption(1920, 1080),
            new DisplayResolutionOption(2560, 1080),
            new DisplayResolutionOption(2560, 1440)
        };

        var result = DisplaySettingsService.BuildResolutionOptions(source);

        Assert.AreEqual(4, result.Count);
        Assert.AreEqual(new DisplayResolutionOption(2560, 1440), result[0]);
        Assert.AreEqual(new DisplayResolutionOption(2560, 1080), result[1]);
        Assert.AreEqual(new DisplayResolutionOption(1920, 1080), result[2]);
        Assert.AreEqual(new DisplayResolutionOption(1280, 720), result[3]);
    }

    [Test]
    public void DisplayResolutionOptionsFallbackWhenAllCandidatesAreTooSmall()
    {
        var result = DisplaySettingsService.BuildResolutionOptions(new[]
        {
            new DisplayResolutionOption(640, 480),
            new DisplayResolutionOption(1024, 600)
        });

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(new DisplayResolutionOption(DisplaySettingsService.MIN_WIDTH, DisplaySettingsService.MIN_HEIGHT), result[0]);
    }

    [Test]
    public void InputBindingOverridesCanSaveLoadAndClear()
    {
        InputActionAsset asset = CreateTestInputActions();
        InputAction pause = asset.FindAction("Gameplay/Pause", throwIfNotFound: true);
        pause.ApplyBindingOverride(0, "<Keyboard>/p");

        string json = asset.SaveBindingOverridesAsJson();
        asset.RemoveAllBindingOverrides();
        Assert.AreEqual("<Keyboard>/escape", pause.bindings[0].effectivePath);

        asset.LoadBindingOverridesFromJson(json);
        Assert.AreEqual("<Keyboard>/p", pause.bindings[0].effectivePath);

        asset.RemoveAllBindingOverrides();
        Assert.AreEqual("<Keyboard>/escape", pause.bindings[0].effectivePath);
    }

    [Test]
    public void InputRebindConflictDetectsDuplicateBindingInSameAction()
    {
        InputActionAsset asset = CreateTestInputActions();
        InputAction move = asset.FindAction("Gameplay/Move", throwIfNotFound: true);
        int upIndex = FindBindingIndex(move, "Up");
        int leftIndex = FindBindingIndex(move, "Left");
        move.ApplyBindingOverride(upIndex, "<Keyboard>/s");

        Assert.IsTrue(InputRebindService.HasConflict(move, upIndex, move.bindings[upIndex].effectivePath));
        Assert.IsFalse(InputRebindService.HasConflict(move, leftIndex, move.bindings[leftIndex].effectivePath));
    }

    [Test]
    public void PlatformSettingsProfilesSelectExpectedPlatformAndFeatures()
    {
        PlatformSettingsProfileSO pc = LoadRequiredProfile(PC_PROFILE_PATH);
        PlatformSettingsProfileSO mobile = LoadRequiredProfile(MOBILE_PROFILE_PATH);
        PlatformSettingsProfileSO console = LoadRequiredProfile(CONSOLE_PROFILE_PATH);

        Assert.IsTrue(pc.DefaultProfile);
        Assert.IsTrue(pc.IsEnabled(SettingsFeature.DisplayResolution));
        Assert.IsTrue(pc.IsEnabled(SettingsFeature.WindowMode));
        Assert.IsTrue(pc.IsEnabled(SettingsFeature.KeyboardRebind));
        Assert.IsTrue(pc.IsEnabled(SettingsFeature.GamepadRebind));
        Assert.IsFalse(pc.IsEnabled(SettingsFeature.TouchControls));
        Assert.AreEqual(3, pc.GetWindowModeCount());
        Assert.AreEqual(2, pc.GetLanguageCount());

        Assert.IsTrue(mobile.IsEnabled(SettingsFeature.TouchControls));
        Assert.IsFalse(mobile.IsEnabled(SettingsFeature.DisplayResolution));
        Assert.IsFalse(mobile.IsEnabled(SettingsFeature.WindowMode));
        Assert.IsFalse(mobile.IsEnabled(SettingsFeature.KeyboardRebind));

        Assert.IsTrue(console.IsEnabled(SettingsFeature.GamepadRebind));
        Assert.IsFalse(console.IsEnabled(SettingsFeature.KeyboardRebind));
        Assert.IsFalse(console.RequireDisplayConfirmation);

        PlatformSettingsProfileSO[] profiles = { pc, mobile, console };
        Assert.AreSame(pc, PlatformSettingsProfileSO.SelectProfile(profiles, RuntimePlatform.WindowsPlayer));
        Assert.AreSame(mobile, PlatformSettingsProfileSO.SelectProfile(profiles, RuntimePlatform.Android));
        Assert.AreSame(console, PlatformSettingsProfileSO.SelectProfile(profiles, RuntimePlatform.PS4));
        Assert.AreSame(pc, PlatformSettingsProfileSO.SelectProfile(profiles, RuntimePlatform.WebGLPlayer));
    }

    [Test]
    public void SettingsPanelPrefabHasStaticSettingsReferences()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(SETTINGS_PANEL_PREFAB_PATH);
        try
        {
            Assert.NotNull(root.transform.Find("Scroll View/Viewport/Content/AudioSection/MasterVolumeRow"));
            Assert.NotNull(root.transform.Find("Scroll View/Viewport/Content/DisplaySection/ResolutionRow"));
            Assert.NotNull(root.transform.Find("Scroll View/Viewport/Content/LanguageSection/LanguageRow"));
            Assert.NotNull(root.transform.Find("Scroll View/Viewport/Content/InputSection/ResetBindingsButton"));
            Assert.NotNull(root.transform.Find("Scroll View/Viewport/Content/TouchSection"));
            Assert.IsNull(root.transform.Find("DisplayConfirmPopup"), "Settings Panel prefab must no longer embed the display confirmation popup.");

            SettingsPanelManager manager = root.GetComponent<SettingsPanelManager>();
            Assert.NotNull(manager);
            SerializedObject managerObject = new(manager);
            AssertObjectReference(managerObject, "masterVolume");
            AssertObjectReference(managerObject, "sfxVolume");
            AssertObjectReference(managerObject, "musicVolume");
            AssertObjectReference(managerObject, "resolutionRow");
            AssertObjectReference(managerObject, "windowModeRow");
            AssertObjectReference(managerObject, "languageRow");
            AssertObjectReference(managerObject, "resetBindingsButton");
            AssertObjectReference(managerObject, "saveButton");
            AssertObjectReference(managerObject, "resetButton");
            Assert.IsNull(managerObject.FindProperty("displayConfirmPopup"), "SettingsPanelManager must not keep a direct display confirm popup reference.");

            SerializedProperty profileProperty = managerObject.FindProperty("platformProfiles");
            Assert.AreEqual(3, profileProperty.arraySize);
            for (int i = 0; i < profileProperty.arraySize; i++)
            {
                Assert.NotNull(profileProperty.GetArrayElementAtIndex(i).objectReferenceValue);
            }

            SerializedProperty rebindRowsProperty = managerObject.FindProperty("rebindRows");
            Assert.AreEqual(InputRebindService.RebindEntries.Count, rebindRowsProperty.arraySize);
            for (int i = 0; i < rebindRowsProperty.arraySize; i++)
            {
                Assert.NotNull(rebindRowsProperty.GetArrayElementAtIndex(i).objectReferenceValue);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    [Test]
    public void SettingsPanelPrefabUsesLayoutControlledStaticContent()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(SETTINGS_PANEL_PREFAB_PATH);
        try
        {
            RectTransform rootRect = root.GetComponent<RectTransform>();
            Assert.NotNull(rootRect);
            Assert.GreaterOrEqual(rootRect.sizeDelta.y, 600f);

            RectTransform scrollView = AssertRect(root.transform, "Scroll View");
            Assert.Less(scrollView.offsetMin.y, scrollView.offsetMax.y + rootRect.sizeDelta.y);

            Transform content = AssertTransform(root.transform, "Scroll View/Viewport/Content");
            VerticalLayoutGroup contentLayout = content.GetComponent<VerticalLayoutGroup>();
            Assert.NotNull(contentLayout);
            Assert.IsTrue(contentLayout.childControlHeight, "Content must control section heights from preferred sizes.");
            Assert.NotNull(root.transform.Find("Scroll View/Viewport").GetComponent<RectMask2D>(), "Viewport must use RectMask2D instead of a transparent Mask.");
            Assert.IsNull(root.transform.Find("Scroll View/Viewport").GetComponent<Mask>(), "Viewport must not use a transparent Mask.");

            AssertSectionUsesPreferredHeights(content, "AudioSection");
            AssertSectionUsesPreferredHeights(content, "DisplaySection");
            AssertSectionUsesPreferredHeights(content, "LanguageSection");
            AssertSectionUsesPreferredHeights(content, "InputSection");
            AssertSectionUsesPreferredHeights(content, "TouchSection");
            AssertSliderRowUsesReadableHorizontalLayout(content, "AudioSection/MasterVolumeRow");
            AssertSliderRowUsesReadableHorizontalLayout(content, "AudioSection/SfxVolumeRow");
            AssertSliderRowUsesReadableHorizontalLayout(content, "AudioSection/MusicVolumeRow");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    [Test]
    public void SettingsPanelHostPrefabsUseCleanNestedSettingsPanelInstance()
    {
        for (int i = 0; i < SETTINGS_PANEL_HOST_PREFABS.Length; i++)
        {
            string hostPath = SETTINGS_PANEL_HOST_PREFABS[i];
            GameObject root = PrefabUtility.LoadPrefabContents(hostPath);
            try
            {
                SettingsPanelManager panel = root.GetComponentInChildren<SettingsPanelManager>(true);
                Assert.NotNull(panel, $"{hostPath} must contain a settings panel instance.");
                Assert.NotNull(panel.transform.Find("Scroll View/Viewport/Content/AudioSection/MasterVolumeRow"), $"{hostPath} is missing the audio slider rows.");
                Assert.NotNull(panel.transform.Find("Scroll View/Viewport/Content/DisplaySection/ResolutionRow"), $"{hostPath} is missing the display rows.");
                Assert.NotNull(panel.transform.Find("Scroll View/Viewport/Content/LanguageSection/LanguageRow"), $"{hostPath} is missing the language row.");
                Assert.NotNull(panel.transform.Find("Scroll View/Viewport/Content/InputSection/ResetBindingsButton"), $"{hostPath} is missing the input rows.");

                AssertNoUnsafeNestedSettingsPanelOverrides(panel.gameObject, hostPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }

    [Test]
    public void SettingsPanelPrefabUsesProjectPixelFontForAllTmpText()
    {
        AssertPrefabUsesProjectPixelFont(SETTINGS_PANEL_PREFAB_PATH);
        AssertPrefabUsesProjectPixelFont(DISPLAY_CONFIRM_MODAL_PREFAB_PATH);
    }

    [Test]
    public void DisplayConfirmModalPrefabIsRegisteredInViewCatalog()
    {
        GameObject modalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DISPLAY_CONFIRM_MODAL_PREFAB_PATH);
        Assert.NotNull(modalPrefab, $"Missing display confirm modal prefab at {DISPLAY_CONFIRM_MODAL_PREFAB_PATH}.");

        GameObject root = PrefabUtility.LoadPrefabContents(DISPLAY_CONFIRM_MODAL_PREFAB_PATH);
        try
        {
            Assert.NotNull(root.GetComponent<DisplayConfirmModal>());
            Assert.NotNull(root.GetComponent<CanvasGroup>());
            Assert.NotNull(root.transform.Find("Panel/Actions/ConfirmButton"));
            Assert.NotNull(root.transform.Find("Panel/Actions/CancelButton"));
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        ViewCatalog catalog = AssetDatabase.LoadAssetAtPath<ViewCatalog>(VIEW_CATALOG_PATH);
        Assert.NotNull(catalog, $"Missing view catalog at {VIEW_CATALOG_PATH}.");
        Assert.IsTrue(catalog.TryFindByType<DisplayConfirmModal>(out ViewDefinition definition));
        Assert.NotNull(definition);
        Assert.AreEqual(ViewKind.Modal, definition.Kind);
        Assert.AreEqual(ViewLayer.Modal, definition.Layer);
        Assert.AreEqual(modalPrefab, definition.Prefab);
        Assert.IsFalse(definition.CloseOnBackgroundClick);
        Assert.IsTrue(definition.Singleton);
        Assert.IsFalse(definition.TrackInBackStack);
    }

    private static InputActionAsset CreateTestInputActions()
    {
        InputActionAsset asset = ScriptableObject.CreateInstance<InputActionAsset>();
        InputActionMap gameplay = asset.AddActionMap("Gameplay");
        InputAction move = gameplay.AddAction("Move", InputActionType.Value);
        InputActionSetupExtensions.CompositeSyntax composite = move.AddCompositeBinding("2DVector");
        composite
            .With("Up", "<Keyboard>/w", groups: "Keyboard&Mouse")
            .With("Down", "<Keyboard>/s", groups: "Keyboard&Mouse")
            .With("Left", "<Keyboard>/a", groups: "Keyboard&Mouse")
            .With("Right", "<Keyboard>/d", groups: "Keyboard&Mouse");
        gameplay.AddAction("Pause", InputActionType.Button)
            .AddBinding("<Keyboard>/escape", groups: "Keyboard&Mouse");
        return asset;
    }

    private static int FindBindingIndex(InputAction action, string partName)
    {
        for (int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding binding = action.bindings[i];
            if (binding.isPartOfComposite && binding.name == partName)
            {
                return i;
            }
        }

        throw new AssertionException($"Missing binding part '{partName}'.");
    }

    private static PlatformSettingsProfileSO LoadRequiredProfile(string path)
    {
        PlatformSettingsProfileSO profile = AssetDatabase.LoadAssetAtPath<PlatformSettingsProfileSO>(path);
        Assert.NotNull(profile, $"Missing platform settings profile at {path}.");
        return profile;
    }

    private static RectTransform AssertRect(Transform root, string path)
    {
        Transform transform = AssertTransform(root, path);
        RectTransform rectTransform = transform.GetComponent<RectTransform>();
        Assert.NotNull(rectTransform, $"{path} must have a RectTransform.");
        return rectTransform;
    }

    private static Transform AssertTransform(Transform root, string path)
    {
        Transform transform = root.Find(path);
        Assert.NotNull(transform, $"Missing settings panel object '{path}'.");
        return transform;
    }

    private static void AssertSectionUsesPreferredHeights(Transform content, string sectionName)
    {
        Transform section = AssertTransform(content, sectionName);
        VerticalLayoutGroup layout = section.GetComponent<VerticalLayoutGroup>();
        Assert.NotNull(layout, $"{sectionName} must have a VerticalLayoutGroup.");
        Assert.IsTrue(layout.childControlHeight, $"{sectionName} must control row heights from preferred sizes.");

        ContentSizeFitter fitter = section.GetComponent<ContentSizeFitter>();
        Assert.IsNull(fitter, $"{sectionName} is a direct Content child and must not have ContentSizeFitter.");
    }

    private static void AssertSliderRowUsesReadableHorizontalLayout(Transform content, string rowPath)
    {
        Transform row = AssertTransform(content, rowPath);
        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        Assert.NotNull(layout, $"{rowPath} must have a HorizontalLayoutGroup.");
        Assert.IsTrue(layout.childControlWidth, $"{rowPath} must let layout drive child widths.");

        LayoutElement labelLayout = AssertLayout(row, "Label");
        LayoutElement sliderLayout = AssertLayout(row, "Slider");
        LayoutElement valueLayout = AssertLayout(row, "Value");
        Assert.GreaterOrEqual(labelLayout.preferredWidth, 80f, $"{rowPath} label must leave room for TMP glyphs.");
        Assert.GreaterOrEqual(sliderLayout.preferredWidth, 260f, $"{rowPath} slider must be wide enough to read and drag.");
        Assert.Greater(sliderLayout.flexibleWidth, 0f, $"{rowPath} slider must expand with available row width.");
        Assert.GreaterOrEqual(valueLayout.preferredWidth, 60f, $"{rowPath} value text must leave room for 100%.");
    }

    private static LayoutElement AssertLayout(Transform root, string path)
    {
        Transform transform = AssertTransform(root, path);
        LayoutElement layoutElement = transform.GetComponent<LayoutElement>();
        Assert.NotNull(layoutElement, $"{path} must have a LayoutElement.");
        return layoutElement;
    }

    private static void AssertNoUnsafeNestedSettingsPanelOverrides(GameObject panelInstance, string hostPath)
    {
        UnityEngine.Object sourceGameObject = PrefabUtility.GetCorrespondingObjectFromSource(panelInstance);
        UnityEngine.Object sourceRectTransform = PrefabUtility.GetCorrespondingObjectFromSource(panelInstance.GetComponent<RectTransform>());
        Assert.NotNull(sourceGameObject, $"{hostPath} settings panel must remain a nested prefab instance.");
        Assert.NotNull(sourceRectTransform, $"{hostPath} settings panel root RectTransform must come from the nested prefab source.");

        PropertyModification[] modifications = PrefabUtility.GetPropertyModifications(panelInstance);
        if (modifications == null)
        {
            return;
        }

        for (int i = 0; i < modifications.Length; i++)
        {
            PropertyModification modification = modifications[i];
            if (modification == null || modification.target == null)
            {
                continue;
            }

            if (modification.target == sourceGameObject || modification.target == sourceRectTransform)
            {
                continue;
            }

            string propertyPath = modification.propertyPath ?? string.Empty;
            bool unsafeOverride =
                propertyPath.StartsWith("m_Anchor") ||
                propertyPath.StartsWith("m_SizeDelta") ||
                propertyPath.StartsWith("m_AnchoredPosition") ||
                propertyPath.StartsWith("m_Pivot") ||
                propertyPath == "m_Layer" ||
                propertyPath.StartsWith("targets.bindings");

            Assert.IsFalse(unsafeOverride, $"{hostPath} has an unsafe nested Settings Panel override: {propertyPath}.");
        }
    }

    private static void AssertObjectReference(SerializedObject serializedObject, string propertyName)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        Assert.NotNull(property, $"Missing serialized property '{propertyName}'.");
        Assert.NotNull(property.objectReferenceValue, $"Serialized property '{propertyName}' is not assigned.");
    }

    private static void AssertPrefabUsesProjectPixelFont(string prefabPath)
    {
        TMP_FontAsset expectedFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SETTINGS_UI_FONT_PATH);
        Assert.NotNull(expectedFont, $"Missing required settings UI font at {SETTINGS_UI_FONT_PATH}.");

        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            TextMeshProUGUI[] texts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
            Assert.Greater(texts.Length, 0, $"{prefabPath} must contain TMP text components.");

            for (int i = 0; i < texts.Length; i++)
            {
                string path = GetHierarchyPath(texts[i].transform);
                Assert.AreSame(expectedFont, texts[i].font, $"{path} must use {SETTINGS_UI_FONT_PATH}.");
                Assert.AreSame(expectedFont.material, texts[i].fontSharedMaterial, $"{path} must use the font asset default material.");
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static string GetHierarchyPath(Transform transform)
    {
        string path = transform.name;
        Transform current = transform.parent;
        while (current != null)
        {
            path = $"{current.name}/{path}";
            current = current.parent;
        }

        return path;
    }

    private static void ClearSettingsPrefs()
    {
        for (int i = 0; i < settingsKeys.Length; i++)
        {
            PlayerPrefs.DeleteKey(settingsKeys[i].Key);
        }
    }

    private readonly struct SavedPrefs
    {
        private readonly PrefEntry[] entries;

        private SavedPrefs(PrefEntry[] entries)
        {
            this.entries = entries;
        }

        public static SavedPrefs Capture(PrefSpec[] keys)
        {
            PrefEntry[] entries = new PrefEntry[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                entries[i] = PrefEntry.Capture(keys[i]);
            }

            return new SavedPrefs(entries);
        }

        public void Restore()
        {
            for (int i = 0; i < entries.Length; i++)
            {
                entries[i].Restore();
            }

            PlayerPrefs.Save();
        }
    }

    private readonly struct PrefEntry
    {
        private readonly string key;
        private readonly PrefValueType valueType;
        private readonly float floatValue;
        private readonly int intValue;
        private readonly string stringValue;
        private readonly bool existed;

        private PrefEntry(
            string key,
            PrefValueType valueType,
            float floatValue,
            int intValue,
            string stringValue,
            bool existed)
        {
            this.key = key;
            this.valueType = valueType;
            this.floatValue = floatValue;
            this.intValue = intValue;
            this.stringValue = stringValue;
            this.existed = existed;
        }

        public static PrefEntry Capture(PrefSpec spec)
        {
            return new PrefEntry(
                spec.Key,
                spec.ValueType,
                PlayerPrefs.GetFloat(spec.Key, 0f),
                PlayerPrefs.GetInt(spec.Key, 0),
                PlayerPrefs.GetString(spec.Key, string.Empty),
                PlayerPrefs.HasKey(spec.Key));
        }

        public void Restore()
        {
            if (existed)
            {
                switch (valueType)
                {
                    case PrefValueType.Float:
                        PlayerPrefs.SetFloat(key, floatValue);
                        break;
                    case PrefValueType.Int:
                        PlayerPrefs.SetInt(key, intValue);
                        break;
                    default:
                        PlayerPrefs.SetString(key, stringValue);
                        break;
                }
            }
            else
            {
                PlayerPrefs.DeleteKey(key);
            }
        }
    }

    private readonly struct PrefSpec
    {
        public PrefSpec(string key, PrefValueType valueType)
        {
            Key = key;
            ValueType = valueType;
        }

        public string Key { get; }
        public PrefValueType ValueType { get; }
    }

    private enum PrefValueType
    {
        Float,
        Int,
        String
    }
}
