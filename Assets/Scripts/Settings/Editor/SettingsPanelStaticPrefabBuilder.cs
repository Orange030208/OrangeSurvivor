#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Orange.Input;
using Orange.UIFramework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class SettingsPanelStaticPrefabBuilder
{
    private const string PREFAB_FOLDER = "Assets/GameContent/UI/Prefabs/Setting";
    private const string PREFAB_PATH = PREFAB_FOLDER + "/Settings Panel.prefab";
    private const string VOLUME_ROW_PREFAB_PATH = PREFAB_FOLDER + "/VolumeRow.prefab";
    private const string MODAL_PREFAB_PATH = "Assets/GameContent/UI/Prefabs/Modals/DisplayConfirm Modal.prefab";
    private const string PROFILE_FOLDER = "Assets/GameContent/UI/Data/Settings";
    private const string VIEW_CATALOG_PATH = "Assets/GameContent/UI/Data/OrangeUIViewCatalog.asset";
    private const string UI_FONT_PATH = "Assets/GameContent/UI/Fonts/HYPixel11pxU-2 SDF.asset";
    private const string SETTINGS_POPUP_VIEW_ID = "popup.settings";
    private const string DISPLAY_CONFIRM_MODAL_VIEW_ID = "modal.displayConfirm";
    private const int UI_LAYER = 5;

    private static TMP_FontAsset cachedUiFont;

    [MenuItem("Tools/Survivors/Rebuild Settings Panel Static UI")]
    public static void Rebuild()
    {
        EnsureFolder(PROFILE_FOLDER);
        EnsureFolder(PREFAB_FOLDER);
        EnsureFolder("Assets/GameContent/UI/Prefabs/Modals");

        PlatformSettingsProfileSO[] profiles =
        {
            CreateOrUpdateProfile(
                "PC Settings Profile",
                "PC",
                new[]
                {
                    RuntimePlatform.WindowsEditor,
                    RuntimePlatform.WindowsPlayer,
                    RuntimePlatform.OSXEditor,
                    RuntimePlatform.OSXPlayer,
                    RuntimePlatform.LinuxEditor,
                    RuntimePlatform.LinuxPlayer
                },
                new[]
                {
                    SettingsFeature.Audio,
                    SettingsFeature.DisplayResolution,
                    SettingsFeature.WindowMode,
                    SettingsFeature.Language,
                    SettingsFeature.KeyboardRebind,
                    SettingsFeature.GamepadRebind
                },
                new[] { FullScreenMode.FullScreenWindow, FullScreenMode.ExclusiveFullScreen, FullScreenMode.Windowed },
                new[] { GameSettingsService.DEFAULT_LANGUAGE_CODE, GameSettingsService.ENGLISH_LANGUAGE_CODE },
                requireDisplayConfirmation: true,
                defaultProfile: true),
            CreateOrUpdateProfile(
                "Mobile Settings Profile",
                "Mobile",
                new[] { RuntimePlatform.Android, RuntimePlatform.IPhonePlayer },
                new[] { SettingsFeature.Audio, SettingsFeature.Language, SettingsFeature.TouchControls },
                new[] { FullScreenMode.FullScreenWindow },
                new[] { GameSettingsService.DEFAULT_LANGUAGE_CODE, GameSettingsService.ENGLISH_LANGUAGE_CODE },
                requireDisplayConfirmation: false,
                defaultProfile: false),
            CreateOrUpdateProfile(
                "Console Settings Profile",
                "Console",
                ResolveConsolePlatforms(),
                new[] { SettingsFeature.Audio, SettingsFeature.Language, SettingsFeature.GamepadRebind },
                new[] { FullScreenMode.FullScreenWindow },
                new[] { GameSettingsService.DEFAULT_LANGUAGE_CODE, GameSettingsService.ENGLISH_LANGUAGE_CODE },
                requireDisplayConfirmation: false,
                defaultProfile: false)
        };

        BuildVolumeRowPrefab();
        GameObject displayConfirmModalPrefab = BuildDisplayConfirmModalPrefab();
        GameObject root = LoadOrCreatePanelPrefabContents();
        try
        {
            RebuildPrefab(root, profiles);
            ApplyUiFontRecursively(root);
            PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        GameObject settingsPanelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
        UpdateViewCatalog(settingsPanelPrefab, displayConfirmModalPrefab);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Rebuilt static settings panel prefab: {PREFAB_PATH}");
    }

    private static GameObject LoadOrCreatePanelPrefabContents()
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
        if (existing != null)
        {
            return PrefabUtility.LoadPrefabContents(PREFAB_PATH);
        }

        GameObject root = new("Settings Panel", typeof(RectTransform), typeof(CanvasGroup), typeof(UIMotionPlayer), typeof(SettingsPanelManager));
        PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
        UnityEngine.Object.DestroyImmediate(root);
        AssetDatabase.ImportAsset(PREFAB_PATH);
        return PrefabUtility.LoadPrefabContents(PREFAB_PATH);
    }

    private static void RebuildPrefab(GameObject root, PlatformSettingsProfileSO[] profiles)
    {
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = new Vector2(1160f, 650f);

        CanvasGroup canvasGroup = root.GetComponent<CanvasGroup>();
        UIMotionPlayer motionPlayer = root.GetComponent<UIMotionPlayer>();
        SettingsPanelManager manager = root.GetComponent<SettingsPanelManager>();
        if (canvasGroup == null || motionPlayer == null || manager == null)
        {
            throw new MissingReferenceException("Settings Panel prefab must keep CanvasGroup, UIMotionPlayer, and SettingsPanelManager on root.");
        }

        ClearChildren(root.transform);

        Image shellImage = root.GetComponent<Image>();
        if (shellImage == null)
        {
            shellImage = root.AddComponent<Image>();
        }

        shellImage.sprite = ResolveSprite("Assets/GameContent/UI/Sprites/SettingsNeonPunk/Panels/panel_settings_main_neon.png");
        shellImage.type = Image.Type.Sliced;
        shellImage.color = new Color(0.02f, 0.02f, 0.08f, 0.96f);
        shellImage.raycastTarget = true;

        TextMeshProUGUI title = CreateText("Title", root.transform, "SETTINGS", 54, FontStyles.Bold, TextAlignmentOptions.Left);
        title.color = new Color(1f, 0.22f, 0.68f, 1f);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(0f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.anchoredPosition = new Vector2(80f, -42f);
        titleRect.sizeDelta = new Vector2(430f, 72f);

        Button closeButton = CreateButton(root.transform, "CloseButton", "X", 58f, 58f);
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-34f, -30f);

        RectTransform nav = CreateRect("Navigation", root.transform);
        nav.anchorMin = new Vector2(0f, 0f);
        nav.anchorMax = new Vector2(0f, 1f);
        nav.pivot = new Vector2(0f, 1f);
        nav.anchoredPosition = new Vector2(46f, -138f);
        nav.sizeDelta = new Vector2(315f, -210f);
        VerticalLayoutGroup navLayout = nav.gameObject.AddComponent<VerticalLayoutGroup>();
        navLayout.spacing = 12f;
        navLayout.padding = new RectOffset(0, 0, 0, 0);
        navLayout.childControlWidth = true;
        navLayout.childControlHeight = false;
        navLayout.childForceExpandWidth = true;
        navLayout.childForceExpandHeight = false;

        Button audioTab = CreateTabButton(nav, "AudioTabButton", "音频设置", ">");
        Button displayTab = CreateTabButton(nav, "DisplayTabButton", "画面设置", ">");
        Button controlTab = CreateTabButton(nav, "ControlTabButton", "控制设置", ">");
        Button gameplayTab = CreateTabButton(nav, "GameplayTabButton", "游戏设置", ">");
        Button languageTab = CreateTabButton(nav, "LanguageTabButton", "语言设置", ">");

        RectTransform contentPanel = CreateRect("ContentPanel", root.transform);
        contentPanel.anchorMin = new Vector2(0f, 0f);
        contentPanel.anchorMax = new Vector2(1f, 1f);
        contentPanel.offsetMin = new Vector2(395f, 70f);
        contentPanel.offsetMax = new Vector2(-54f, -142f);
        Image contentImage = contentPanel.gameObject.AddComponent<Image>();
        contentImage.sprite = ResolveSprite("Assets/GameContent/UI/Sprites/SettingsNeonPunk/Panels/panel_settings_content_neon.png");
        contentImage.type = Image.Type.Sliced;
        contentImage.color = new Color(0.02f, 0.03f, 0.10f, 0.86f);

        TextMeshProUGUI sectionTitle = CreateText("SectionTitle", contentPanel, "音频设置", 26, FontStyles.Bold, TextAlignmentOptions.Left);
        sectionTitle.color = new Color(1f, 0.22f, 0.68f, 1f);
        RectTransform sectionTitleRect = sectionTitle.rectTransform;
        sectionTitleRect.anchorMin = new Vector2(0f, 1f);
        sectionTitleRect.anchorMax = new Vector2(1f, 1f);
        sectionTitleRect.pivot = new Vector2(0f, 1f);
        sectionTitleRect.offsetMin = new Vector2(34f, -72f);
        sectionTitleRect.offsetMax = new Vector2(-34f, -22f);

        RectTransform divider = CreateRect("TitleDivider", contentPanel);
        divider.anchorMin = new Vector2(0f, 1f);
        divider.anchorMax = new Vector2(1f, 1f);
        divider.pivot = new Vector2(0.5f, 1f);
        divider.offsetMin = new Vector2(34f, -86f);
        divider.offsetMax = new Vector2(-34f, -83f);
        Image dividerImage = divider.gameObject.AddComponent<Image>();
        dividerImage.color = new Color(1f, 0.13f, 0.74f, 0.75f);

        RectTransform sectionRoot = CreateRect("SectionRoot", contentPanel);
        sectionRoot.anchorMin = Vector2.zero;
        sectionRoot.anchorMax = Vector2.one;
        sectionRoot.offsetMin = new Vector2(34f, 28f);
        sectionRoot.offsetMax = new Vector2(-34f, -108f);

        GameObject audioSection = CreateContentSection(sectionRoot, "AudioSection");
        SettingsSliderRow masterRow = CreateSliderRow(audioSection.transform, "MasterVolumeRow", "主音量");
        SettingsSliderRow musicRow = CreateSliderRow(audioSection.transform, "MusicVolumeRow", "音乐音量");
        SettingsSliderRow sfxRow = CreateSliderRow(audioSection.transform, "SfxVolumeRow", "音效音量");

        GameObject displaySection = CreateContentSection(sectionRoot, "DisplaySection");
        SettingsOptionRow resolutionRow = CreateOptionRow(displaySection.transform, "ResolutionRow", "分辨率");
        SettingsOptionRow windowModeRow = CreateOptionRow(displaySection.transform, "WindowModeRow", "窗口模式");

        GameObject inputSection = CreateScrollableContentSection(sectionRoot, "InputSection", out Transform inputContent);
        List<SettingsRebindRow> rebindRows = new();
        IReadOnlyList<InputRebindEntry> entries = GameInputRebindCatalog.Entries;
        for (int i = 0; i < entries.Count; i++)
        {
            rebindRows.Add(CreateRebindRow(inputContent, entries[i]));
        }

        Button resetBindingsButton = CreateButton(inputContent, "ResetBindingsButton", "恢复默认绑定", 180f, 38f);
        SetLayout(resetBindingsButton.gameObject, preferredHeight: 44f);

        GameObject gameplaySection = CreateContentSection(sectionRoot, "GameplaySection");
        CreatePlaceholder(gameplaySection.transform, "GameplayPlaceholder", "游戏设置项待接入");

        GameObject languageSection = CreateContentSection(sectionRoot, "LanguageSection");
        SettingsOptionRow languageRow = CreateOptionRow(languageSection.transform, "LanguageRow", "语言");

        RectTransform actionBar = CreateRect("ActionBar", root.transform);
        actionBar.anchorMin = new Vector2(1f, 0f);
        actionBar.anchorMax = new Vector2(1f, 0f);
        actionBar.pivot = new Vector2(1f, 0f);
        actionBar.anchoredPosition = new Vector2(-58f, 28f);
        actionBar.sizeDelta = new Vector2(190f, 44f);
        HorizontalLayoutGroup actionLayout = actionBar.gameObject.AddComponent<HorizontalLayoutGroup>();
        actionLayout.childAlignment = TextAnchor.MiddleRight;
        actionLayout.childControlWidth = false;
        actionLayout.childControlHeight = false;
        actionLayout.childForceExpandWidth = false;
        actionLayout.childForceExpandHeight = false;
        Button resetButton = CreateButton(actionBar, "ResetButton", "恢复默认", 160f, 40f);

        SerializedObject managerObject = new(manager);
        SetObject(managerObject, "motionSource", motionPlayer);
        SetObject(managerObject, "canvasGroup", canvasGroup);
        SetObjectArray(managerObject, "platformProfiles", profiles);
        SetObject(managerObject, "defaultSelectable", audioTab);
        SetObject(managerObject, "sectionTitle", sectionTitle);
        SetObject(managerObject, "audioTabButton", audioTab);
        SetObject(managerObject, "displayTabButton", displayTab);
        SetObject(managerObject, "controlTabButton", controlTab);
        SetObject(managerObject, "gameplayTabButton", gameplayTab);
        SetObject(managerObject, "languageTabButton", languageTab);
        SetObject(managerObject, "closeButton", closeButton);
        SetObject(managerObject, "audioSection", audioSection);
        SetObject(managerObject, "displaySection", displaySection);
        SetObject(managerObject, "languageSection", languageSection);
        SetObject(managerObject, "inputSection", inputSection);
        SetObject(managerObject, "gameplaySection", gameplaySection);
        SetObject(managerObject, "touchSection", gameplaySection);
        SetObject(managerObject, "masterVolume", masterRow);
        SetObject(managerObject, "sfxVolume", sfxRow);
        SetObject(managerObject, "musicVolume", musicRow);
        SetObject(managerObject, "resolutionRow", resolutionRow);
        SetObject(managerObject, "windowModeRow", windowModeRow);
        SetObject(managerObject, "languageRow", languageRow);
        SetObjectArray(managerObject, "rebindRows", rebindRows.ToArray());
        SetObject(managerObject, "resetBindingsButton", resetBindingsButton);
        SetObject(managerObject, "resetButton", resetButton);
        managerObject.FindProperty("applyPreviewImmediately").boolValue = true;
        managerObject.ApplyModifiedPropertiesWithoutUndo();

        SetLayerRecursively(root, UI_LAYER);
    }

    private static void BuildVolumeRowPrefab()
    {
        GameObject root = new("VolumeRow", typeof(RectTransform));
        RectTransform rectTransform = root.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(620f, 54f);
        CreateSliderRow(root.transform, "VolumeRow", "音量");
        ApplyUiFontRecursively(root);
        SetLayerRecursively(root, UI_LAYER);
        PrefabUtility.SaveAsPrefabAsset(root.transform.GetChild(0).gameObject, VOLUME_ROW_PREFAB_PATH);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static PlatformSettingsProfileSO CreateOrUpdateProfile(
        string assetName,
        string profileId,
        RuntimePlatform[] platforms,
        SettingsFeature[] features,
        FullScreenMode[] windowModes,
        string[] languageCodes,
        bool requireDisplayConfirmation,
        bool defaultProfile)
    {
        string path = $"{PROFILE_FOLDER}/{assetName}.asset";
        PlatformSettingsProfileSO profile = AssetDatabase.LoadAssetAtPath<PlatformSettingsProfileSO>(path);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<PlatformSettingsProfileSO>();
            AssetDatabase.CreateAsset(profile, path);
        }

        SerializedObject serializedObject = new(profile);
        serializedObject.FindProperty("profileId").stringValue = profileId;
        SetEnumArray(serializedObject.FindProperty("platforms"), platforms);
        SetEnumArray(serializedObject.FindProperty("enabledFeatures"), features);
        SetEnumArray(serializedObject.FindProperty("windowModes"), windowModes);
        SetStringArray(serializedObject.FindProperty("languageCodes"), languageCodes);
        serializedObject.FindProperty("requireDisplayConfirmation").boolValue = requireDisplayConfirmation;
        serializedObject.FindProperty("defaultProfile").boolValue = defaultProfile;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static void UpdateViewCatalog(GameObject settingsPanelPrefab, GameObject displayConfirmModalPrefab)
    {
        if (settingsPanelPrefab == null)
        {
            throw new MissingReferenceException($"Missing settings panel prefab at '{PREFAB_PATH}'.");
        }

        if (displayConfirmModalPrefab == null)
        {
            throw new MissingReferenceException($"Missing display confirmation modal prefab at '{MODAL_PREFAB_PATH}'.");
        }

        ViewCatalog catalog = AssetDatabase.LoadAssetAtPath<ViewCatalog>(VIEW_CATALOG_PATH);
        if (catalog == null)
        {
            throw new MissingReferenceException($"Missing UI view catalog at '{VIEW_CATALOG_PATH}'.");
        }

        SerializedObject catalogObject = new(catalog);
        SerializedProperty viewsProperty = catalogObject.FindProperty("views");
        UpsertViewDefinition(
            viewsProperty,
            SETTINGS_POPUP_VIEW_ID,
            ViewKind.Popup,
            ViewLayer.Popup,
            settingsPanelPrefab,
            singleton: true,
            cacheOnClose: true,
            trackInBackStack: true,
            closeOnBackgroundClick: false);
        UpsertViewDefinition(
            viewsProperty,
            DISPLAY_CONFIRM_MODAL_VIEW_ID,
            ViewKind.Modal,
            ViewLayer.Modal,
            displayConfirmModalPrefab,
            singleton: true,
            cacheOnClose: true,
            trackInBackStack: false,
            closeOnBackgroundClick: false);
        catalogObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(catalog);
    }

    private static void UpsertViewDefinition(
        SerializedProperty viewsProperty,
        string viewId,
        ViewKind kind,
        ViewLayer layer,
        GameObject prefab,
        bool singleton,
        bool cacheOnClose,
        bool trackInBackStack,
        bool closeOnBackgroundClick)
    {
        SerializedProperty definitionProperty = FindViewDefinition(viewsProperty, viewId);
        if (definitionProperty == null)
        {
            int newIndex = viewsProperty.arraySize;
            viewsProperty.arraySize++;
            definitionProperty = viewsProperty.GetArrayElementAtIndex(newIndex);
        }

        definitionProperty.FindPropertyRelative("id").stringValue = viewId;
        definitionProperty.FindPropertyRelative("kind").intValue = (int)kind;
        definitionProperty.FindPropertyRelative("layer").intValue = (int)layer;
        definitionProperty.FindPropertyRelative("prefab").objectReferenceValue = prefab;
        definitionProperty.FindPropertyRelative("singleton").boolValue = singleton;
        definitionProperty.FindPropertyRelative("cacheOnClose").boolValue = cacheOnClose;
        definitionProperty.FindPropertyRelative("trackInBackStack").boolValue = trackInBackStack;
        definitionProperty.FindPropertyRelative("closeOnBackgroundClick").boolValue = closeOnBackgroundClick;
        definitionProperty.FindPropertyRelative("warmupCount").intValue = 0;
        definitionProperty.FindPropertyRelative("maxCachedInstancesOverride").intValue = -1;
        definitionProperty.FindPropertyRelative("allowDuplicateViewType").boolValue = false;
    }

    private static SerializedProperty FindViewDefinition(SerializedProperty viewsProperty, string viewId)
    {
        if (viewsProperty == null)
        {
            return null;
        }

        for (int i = 0; i < viewsProperty.arraySize; i++)
        {
            SerializedProperty element = viewsProperty.GetArrayElementAtIndex(i);
            if (element.FindPropertyRelative("id").stringValue == viewId)
            {
                return element;
            }
        }

        return null;
    }

    private static RuntimePlatform[] ResolveConsolePlatforms()
    {
        string[] names = { "PS4", "PS5", "XboxOne", "GameCoreXboxOne", "GameCoreXboxSeries", "Switch" };
        List<RuntimePlatform> platforms = new();
        for (int i = 0; i < names.Length; i++)
        {
            if (Enum.TryParse(names[i], out RuntimePlatform platform))
            {
                platforms.Add(platform);
            }
        }

        return platforms.ToArray();
    }

    private static GameObject CreateContentSection(Transform parent, string objectName)
    {
        RectTransform section = CreateRect(objectName, parent);
        section.anchorMin = Vector2.zero;
        section.anchorMax = Vector2.one;
        section.offsetMin = Vector2.zero;
        section.offsetMax = Vector2.zero;
        VerticalLayoutGroup layout = section.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 12f;
        layout.padding = new RectOffset(0, 0, 8, 8);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return section.gameObject;
    }

    private static GameObject CreateScrollableContentSection(Transform parent, string objectName, out Transform content)
    {
        RectTransform section = CreateRect(objectName, parent);
        section.anchorMin = Vector2.zero;
        section.anchorMax = Vector2.one;
        section.offsetMin = Vector2.zero;
        section.offsetMax = Vector2.zero;

        ScrollRect scrollRect = section.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 28f;

        RectTransform viewport = CreateRect("Viewport", section);
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = Vector2.zero;
        viewport.offsetMax = Vector2.zero;
        Image viewportImage = viewport.gameObject.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0f);
        viewportImage.raycastTarget = true;
        viewport.gameObject.AddComponent<RectMask2D>();

        RectTransform contentRect = CreateRect("Content", viewport);
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;
        VerticalLayoutGroup layout = contentRect.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 10f;
        layout.padding = new RectOffset(0, 0, 8, 8);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = contentRect.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewport;
        scrollRect.content = contentRect;
        content = contentRect;
        return section.gameObject;
    }

    private static Button CreateTabButton(Transform parent, string objectName, string label, string icon)
    {
        Button button = CreateButton(parent, objectName, string.Empty, 300f, 72f);
        Image image = button.targetGraphic as Image;
        if (image != null)
        {
            image.sprite = ResolveSprite("Assets/GameContent/UI/Sprites/SettingsNeonPunk/Tabs/tab_default.png");
            image.type = Image.Type.Sliced;
            image.color = new Color(0.03f, 0.06f, 0.16f, 0.82f);
        }

        HorizontalLayoutGroup layout = button.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(24, 18, 0, 0);
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        TextMeshProUGUI iconText = CreateText("Icon", button.transform, icon, 28, FontStyles.Bold, TextAlignmentOptions.Center);
        iconText.color = new Color(0.13f, 0.68f, 1f, 1f);
        SetLayout(iconText.gameObject, preferredWidth: 42f, preferredHeight: 56f);

        TextMeshProUGUI labelText = CreateText("Label", button.transform, label, 24, FontStyles.Bold, TextAlignmentOptions.Left);
        labelText.color = new Color(0.13f, 0.68f, 1f, 1f);
        SetLayout(labelText.gameObject, preferredWidth: 178f, preferredHeight: 56f);

        TextMeshProUGUI arrowText = CreateText("Arrow", button.transform, ">", 28, FontStyles.Bold, TextAlignmentOptions.Center);
        arrowText.color = new Color(0.13f, 0.68f, 1f, 1f);
        SetLayout(arrowText.gameObject, preferredWidth: 32f, preferredHeight: 56f);
        return button;
    }

    private static SettingsSliderRow CreateSliderRow(Transform parent, string objectName, string label)
    {
        RectTransform row = CreateRow(parent, objectName, 62f);
        Image rowImage = row.gameObject.AddComponent<Image>();
        rowImage.sprite = ResolveSprite("Assets/GameContent/UI/Sprites/SettingsNeonPunk/Panels/panel_settings_row_neon.png");
        rowImage.type = Image.Type.Sliced;
        rowImage.color = new Color(0.02f, 0.03f, 0.09f, 0.38f);

        TextMeshProUGUI labelText = CreateText("Label", row, label, 21, FontStyles.Normal, TextAlignmentOptions.Left);
        labelText.color = new Color(0.90f, 0.92f, 1f, 1f);
        SetLayout(labelText.gameObject, preferredWidth: 150f, preferredHeight: 46f);

        Slider slider = CreateSlider(row);
        SetLayout(slider.gameObject, preferredWidth: 360f, flexibleWidth: 1f, preferredHeight: 42f);

        TextMeshProUGUI valueText = CreateText("Value", row, "100%", 21, FontStyles.Bold, TextAlignmentOptions.Right);
        valueText.color = new Color(0.95f, 0.95f, 1f, 1f);
        SetLayout(valueText.gameObject, preferredWidth: 82f, preferredHeight: 46f);

        SettingsSliderRow component = row.gameObject.AddComponent<SettingsSliderRow>();
        SerializedObject serializedObject = new(component);
        SetObject(serializedObject, "labelText", labelText);
        SetObject(serializedObject, "slider", slider);
        SetObject(serializedObject, "valueText", valueText);
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        return component;
    }

    private static SettingsOptionRow CreateOptionRow(Transform parent, string objectName, string label)
    {
        RectTransform row = CreateRow(parent, objectName, 58f);
        Image rowImage = row.gameObject.AddComponent<Image>();
        rowImage.sprite = ResolveSprite("Assets/GameContent/UI/Sprites/SettingsNeonPunk/Panels/panel_settings_row_neon.png");
        rowImage.type = Image.Type.Sliced;
        rowImage.color = new Color(0.02f, 0.03f, 0.09f, 0.38f);

        TextMeshProUGUI labelText = CreateText("Label", row, label, 21, FontStyles.Normal, TextAlignmentOptions.Left);
        labelText.color = new Color(0.90f, 0.92f, 1f, 1f);
        SetLayout(labelText.gameObject, preferredWidth: 150f, preferredHeight: 42f);
        Button previousButton = CreateButton(row, "PreviousButton", "<", 48f, 38f);
        TextMeshProUGUI valueText = CreateText("Value", row, "-", 21, FontStyles.Bold, TextAlignmentOptions.Center);
        valueText.color = new Color(0.95f, 0.95f, 1f, 1f);
        SetLayout(valueText.gameObject, preferredWidth: 280f, flexibleWidth: 1f, preferredHeight: 42f);
        Button nextButton = CreateButton(row, "NextButton", ">", 48f, 38f);

        SettingsOptionRow component = row.gameObject.AddComponent<SettingsOptionRow>();
        SerializedObject serializedObject = new(component);
        SetObject(serializedObject, "labelText", labelText);
        SetObject(serializedObject, "previousButton", previousButton);
        SetObject(serializedObject, "valueText", valueText);
        SetObject(serializedObject, "nextButton", nextButton);
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        return component;
    }

    private static SettingsRebindRow CreateRebindRow(Transform parent, InputRebindEntry entry)
    {
        RectTransform row = CreateRow(parent, $"RebindRow_{SanitizeName(entry.DisplayLabel)}", 54f);
        TextMeshProUGUI labelText = CreateText("Label", row, entry.DisplayLabel, 18, FontStyles.Normal, TextAlignmentOptions.Left);
        SetLayout(labelText.gameObject, preferredWidth: 230f, preferredHeight: 38f);
        TextMeshProUGUI valueText = CreateText("Value", row, "-", 18, FontStyles.Bold, TextAlignmentOptions.Center);
        SetLayout(valueText.gameObject, preferredWidth: 170f, flexibleWidth: 1f, preferredHeight: 38f);
        Button rebindButton = CreateButton(row, "RebindButton", "重绑", 82f, 36f);

        SettingsRebindRow component = row.gameObject.AddComponent<SettingsRebindRow>();
        SerializedObject serializedObject = new(component);
        serializedObject.FindProperty("actionPath").stringValue = entry.ActionPath;
        serializedObject.FindProperty("compositePartName").stringValue = entry.CompositePartName ?? string.Empty;
        serializedObject.FindProperty("label").stringValue = entry.Label;
        serializedObject.FindProperty("controlScheme").stringValue = entry.ControlScheme;
        serializedObject.FindProperty("bindingGroup").stringValue = entry.BindingGroup;
        serializedObject.FindProperty("requiredControlPath").stringValue = entry.RequiredControlPath;
        SetStringArray(serializedObject.FindProperty("cancelControlPaths"), entry.CancelControlPaths);
        SetObject(serializedObject, "labelText", labelText);
        SetObject(serializedObject, "valueText", valueText);
        SetObject(serializedObject, "rebindButton", rebindButton);
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        return component;
    }

    private static void CreatePlaceholder(Transform parent, string objectName, string message)
    {
        RectTransform row = CreateRow(parent, objectName, 64f);
        TextMeshProUGUI labelText = CreateText("Text", row, message, 20, FontStyles.Normal, TextAlignmentOptions.Center);
        labelText.color = new Color(0.58f, 0.72f, 0.92f, 0.82f);
        SetLayout(labelText.gameObject, flexibleWidth: 1f, preferredHeight: 48f);
    }

    private static GameObject BuildDisplayConfirmModalPrefab()
    {
        GameObject root = new("DisplayConfirm Modal", typeof(RectTransform), typeof(CanvasGroup), typeof(DisplayConfirmModal));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        CanvasGroup canvasGroup = root.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        RectTransform panel = CreateRect("Panel", root.transform);
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = Vector2.zero;
        panel.sizeDelta = new Vector2(500f, 230f);
        Image panelImage = panel.gameObject.AddComponent<Image>();
        panelImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        panelImage.color = new Color(0.09f, 0.11f, 0.14f, 0.98f);
        VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(24, 24, 22, 22);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        TextMeshProUGUI messageText = CreateText("Message", panel, string.Empty, 22, FontStyles.Bold, TextAlignmentOptions.Center);
        messageText.enableWordWrapping = true;
        SetLayout(messageText.gameObject, preferredHeight: 80f);
        TextMeshProUGUI countdownText = CreateText("Countdown", panel, string.Empty, 18, FontStyles.Normal, TextAlignmentOptions.Center);
        SetLayout(countdownText.gameObject, preferredHeight: 34f);

        RectTransform actions = CreateRect("Actions", panel);
        HorizontalLayoutGroup actionLayout = actions.gameObject.AddComponent<HorizontalLayoutGroup>();
        actionLayout.spacing = 18f;
        actionLayout.childAlignment = TextAnchor.MiddleCenter;
        actionLayout.childControlWidth = false;
        actionLayout.childControlHeight = false;
        actionLayout.childForceExpandWidth = false;
        actionLayout.childForceExpandHeight = false;
        SetLayout(actions.gameObject, preferredHeight: 46f);
        Button confirmButton = CreateButton(actions, "ConfirmButton", "确认保留", 132f, 40f);
        Button cancelButton = CreateButton(actions, "CancelButton", "回退", 132f, 40f);

        DisplayConfirmModal modal = root.GetComponent<DisplayConfirmModal>();
        SerializedObject serializedObject = new(modal);
        SetObject(serializedObject, "messageText", messageText);
        SetObject(serializedObject, "countdownText", countdownText);
        SetObject(serializedObject, "confirmButton", confirmButton);
        SetObject(serializedObject, "cancelButton", cancelButton);
        serializedObject.ApplyModifiedPropertiesWithoutUndo();

        ApplyUiFontRecursively(root);
        SetLayerRecursively(root, UI_LAYER);
        root.SetActive(true);
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, MODAL_PREFAB_PATH);
        UnityEngine.Object.DestroyImmediate(root);
        AssetDatabase.ImportAsset(MODAL_PREFAB_PATH);
        return savedPrefab != null ? savedPrefab : AssetDatabase.LoadAssetAtPath<GameObject>(MODAL_PREFAB_PATH);
    }

    private static RectTransform CreateRow(Transform parent, string objectName, float preferredHeight)
    {
        RectTransform row = CreateRect(objectName, parent);
        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 12f;
        layout.padding = new RectOffset(14, 14, 0, 0);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        SetLayout(row.gameObject, preferredHeight: preferredHeight);
        return row;
    }

    private static Slider CreateSlider(Transform parent)
    {
        RectTransform root = CreateRect("Slider", parent);
        Slider slider = root.gameObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;

        RectTransform background = CreateRect("Background", root);
        background.anchorMin = new Vector2(0f, 0.42f);
        background.anchorMax = new Vector2(1f, 0.58f);
        background.offsetMin = Vector2.zero;
        background.offsetMax = Vector2.zero;
        Image backgroundImage = background.gameObject.AddComponent<Image>();
        backgroundImage.sprite = ResolveSprite("Assets/GameContent/UI/Sprites/SettingsNeonPunk/Controls/slider_track.png");
        backgroundImage.type = Image.Type.Sliced;
        backgroundImage.color = new Color(0.10f, 0.02f, 0.18f, 0.95f);

        RectTransform fillArea = CreateRect("Fill Area", root);
        fillArea.anchorMin = new Vector2(0f, 0.42f);
        fillArea.anchorMax = new Vector2(1f, 0.58f);
        fillArea.offsetMin = new Vector2(8f, 0f);
        fillArea.offsetMax = new Vector2(-8f, 0f);
        RectTransform fill = CreateRect("Fill", fillArea);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;
        Image fillImage = fill.gameObject.AddComponent<Image>();
        fillImage.sprite = ResolveSprite("Assets/GameContent/UI/Sprites/SettingsNeonPunk/Controls/slider_fill.png");
        fillImage.type = Image.Type.Sliced;
        fillImage.color = new Color(1f, 0.18f, 0.68f, 1f);

        RectTransform handleArea = CreateRect("Handle Slide Area", root);
        handleArea.anchorMin = Vector2.zero;
        handleArea.anchorMax = Vector2.one;
        handleArea.offsetMin = new Vector2(8f, 0f);
        handleArea.offsetMax = new Vector2(-8f, 0f);
        RectTransform handle = CreateRect("Handle", handleArea);
        handle.sizeDelta = new Vector2(24f, 24f);
        Image handleImage = handle.gameObject.AddComponent<Image>();
        handleImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        handleImage.color = new Color(1f, 0.22f, 0.68f, 1f);

        slider.targetGraphic = handleImage;
        slider.fillRect = fill;
        slider.handleRect = handle;
        return slider;
    }

    private static Button CreateButton(Transform parent, string objectName, string label, float width, float height)
    {
        RectTransform rectTransform = CreateRect(objectName, parent);
        rectTransform.sizeDelta = new Vector2(width, height);
        Image image = rectTransform.gameObject.AddComponent<Image>();
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        image.type = Image.Type.Sliced;
        image.color = new Color(0.08f, 0.11f, 0.22f, 0.94f);
        Button button = rectTransform.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.28f, 0.72f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(0.70f, 0.10f, 0.45f, 1f);
        colors.disabledColor = new Color(0.18f, 0.18f, 0.22f, 0.55f);
        button.colors = colors;

        if (!string.IsNullOrEmpty(label))
        {
            TextMeshProUGUI text = CreateText("Text", rectTransform, label, 16, FontStyles.Bold, TextAlignmentOptions.Center);
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        SetLayout(rectTransform.gameObject, preferredWidth: width, preferredHeight: height);
        return button;
    }

    private static TextMeshProUGUI CreateText(
        string objectName,
        Transform parent,
        string value,
        int fontSize,
        FontStyles fontStyle,
        TextAlignmentOptions alignment)
    {
        RectTransform rectTransform = CreateRect(objectName, parent);
        TextMeshProUGUI text = rectTransform.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = Color.white;
        text.alignment = alignment;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        ApplyUiFont(text, ResolveRequiredUiFont());
        return text;
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }
    }

    private static Sprite ResolveSprite(string assetPath)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        return sprite != null ? sprite : AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
    }

    private static void ApplyUiFontRecursively(GameObject root)
    {
        TMP_FontAsset font = ResolveRequiredUiFont();
        TextMeshProUGUI[] texts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            ApplyUiFont(texts[i], font);
            EditorUtility.SetDirty(texts[i]);
        }
    }

    private static void ApplyUiFont(TextMeshProUGUI text, TMP_FontAsset font)
    {
        text.font = font;
        if (font.material != null)
        {
            text.fontSharedMaterial = font.material;
        }
    }

    private static TMP_FontAsset ResolveRequiredUiFont()
    {
        if (cachedUiFont != null)
        {
            return cachedUiFont;
        }

        cachedUiFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UI_FONT_PATH);
        if (cachedUiFont == null)
        {
            throw new MissingReferenceException($"Missing required TMP font asset at '{UI_FONT_PATH}'.");
        }

        return cachedUiFont;
    }

    private static RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject gameObject = new(objectName, typeof(RectTransform));
        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        return rectTransform;
    }

    private static void SetLayout(
        GameObject target,
        float preferredWidth = -1f,
        float preferredHeight = -1f,
        float flexibleWidth = -1f)
    {
        LayoutElement layoutElement = target.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = target.AddComponent<LayoutElement>();
        }

        if (preferredWidth >= 0f)
        {
            layoutElement.preferredWidth = preferredWidth;
            layoutElement.minWidth = preferredWidth;
        }

        if (preferredHeight >= 0f)
        {
            layoutElement.preferredHeight = preferredHeight;
            layoutElement.minHeight = preferredHeight;
        }

        if (flexibleWidth >= 0f)
        {
            layoutElement.flexibleWidth = flexibleWidth;
        }
    }

    private static void SetObject(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            throw new MissingReferenceException($"{serializedObject.targetObject.GetType().Name} is missing serialized field '{propertyName}'.");
        }

        property.objectReferenceValue = value;
    }

    private static void SetObjectArray<T>(SerializedObject serializedObject, string propertyName, T[] values)
        where T : UnityEngine.Object
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            throw new MissingReferenceException($"{serializedObject.targetObject.GetType().Name} is missing serialized field '{propertyName}'.");
        }

        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }

    private static void SetStringArray(SerializedProperty property, string[] values)
    {
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            property.GetArrayElementAtIndex(i).stringValue = values[i];
        }
    }

    private static void SetEnumArray<T>(SerializedProperty property, T[] values)
        where T : Enum
    {
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            property.GetArrayElementAtIndex(i).intValue = Convert.ToInt32(values[i]);
        }
    }

    private static void EnsureFolder(string folderPath)
    {
        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    private static string SanitizeName(string value)
    {
        return value.Replace("/", "_")
            .Replace("\\", "_")
            .Replace(" ", "_")
            .Replace("(", string.Empty)
            .Replace(")", string.Empty);
    }

    private static void SetLayerRecursively(GameObject target, int layer)
    {
        target.layer = layer;
        for (int i = 0; i < target.transform.childCount; i++)
        {
            SetLayerRecursively(target.transform.GetChild(i).gameObject, layer);
        }
    }

    private static void KeepOnlyRootPrefabOverrides(GameObject instance)
    {
        PropertyModification[] modifications = PrefabUtility.GetPropertyModifications(instance);
        if (modifications == null || modifications.Length == 0)
        {
            return;
        }

        UnityEngine.Object sourceGameObject = PrefabUtility.GetCorrespondingObjectFromSource(instance);
        UnityEngine.Object sourceRectTransform = PrefabUtility.GetCorrespondingObjectFromSource(instance.GetComponent<RectTransform>());
        List<PropertyModification> filtered = new();
        for (int i = 0; i < modifications.Length; i++)
        {
            PropertyModification modification = modifications[i];
            if (modification == null || modification.target == null)
            {
                continue;
            }

            if (modification.target == sourceGameObject ||
                modification.target == sourceRectTransform)
            {
                filtered.Add(modification);
            }
        }

        PrefabUtility.SetPropertyModifications(instance, filtered.ToArray());
    }

    private readonly struct RectTransformSnapshot
    {
        private readonly Vector2 anchorMin;
        private readonly Vector2 anchorMax;
        private readonly Vector2 anchoredPosition;
        private readonly Vector2 sizeDelta;
        private readonly Vector2 pivot;
        private readonly Vector3 localPosition;
        private readonly Quaternion localRotation;
        private readonly Vector3 localScale;

        private RectTransformSnapshot(RectTransform rectTransform)
        {
            anchorMin = rectTransform.anchorMin;
            anchorMax = rectTransform.anchorMax;
            anchoredPosition = rectTransform.anchoredPosition;
            sizeDelta = rectTransform.sizeDelta;
            pivot = rectTransform.pivot;
            localPosition = rectTransform.localPosition;
            localRotation = rectTransform.localRotation;
            localScale = rectTransform.localScale;
        }

        public static RectTransformSnapshot Capture(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                throw new MissingReferenceException("Settings panel instance must have a RectTransform.");
            }

            return new RectTransformSnapshot(rectTransform);
        }

        public void Apply(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                throw new MissingReferenceException("Replacement settings panel instance must have a RectTransform.");
            }

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = pivot;
            rectTransform.sizeDelta = sizeDelta;
            rectTransform.anchoredPosition = anchoredPosition;
            Vector3 appliedLocalPosition = rectTransform.localPosition;
            appliedLocalPosition.z = localPosition.z;
            rectTransform.localPosition = appliedLocalPosition;
            rectTransform.localRotation = localRotation;
            rectTransform.localScale = localScale;
        }
    }
}
#endif
