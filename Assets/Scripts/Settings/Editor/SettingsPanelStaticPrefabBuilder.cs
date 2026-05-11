#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Orange.UIFramework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class SettingsPanelStaticPrefabBuilder
{
    private const string PREFAB_PATH = "Assets/GameContent/UI/Prefabs/Pages/Setting/Settings Panel.prefab";
    private const string MODAL_PREFAB_PATH = "Assets/GameContent/UI/Prefabs/Modals/DisplayConfirm Modal.prefab";
    private const string PROFILE_FOLDER = "Assets/GameContent/UI/Data/Settings";
    private const string VIEW_CATALOG_PATH = "Assets/GameContent/UI/Data/OrangeUIViewCatalog.asset";
    private const string UI_FONT_PATH = "Assets/GameContent/UI/Fonts/HYPixel11pxU-2 SDF.asset";
    private const string DISPLAY_CONFIRM_MODAL_VIEW_ID = "modal.displayConfirm";
    private static readonly string[] HOST_PREFAB_PATHS =
    {
        "Assets/GameContent/UI/Prefabs/Pages/UI Menu.prefab",
        "Assets/GameContent/UI/Prefabs/Pages/UI Pause.prefab"
    };

    private static TMP_FontAsset cachedUiFont;

    [MenuItem("Tools/Survivors/Rebuild Settings Panel Static UI")]
    public static void Rebuild()
    {
        EnsureFolder(PROFILE_FOLDER);
        EnsureFolder("Assets/GameContent/UI/Prefabs/Modals");
        PlatformSettingsProfileSO pcProfile = CreateOrUpdateProfile(
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
            defaultProfile: true);

        PlatformSettingsProfileSO mobileProfile = CreateOrUpdateProfile(
            "Mobile Settings Profile",
            "Mobile",
            new[] { RuntimePlatform.Android, RuntimePlatform.IPhonePlayer },
            new[] { SettingsFeature.Audio, SettingsFeature.Language, SettingsFeature.TouchControls },
            new[] { FullScreenMode.FullScreenWindow },
            new[] { GameSettingsService.DEFAULT_LANGUAGE_CODE, GameSettingsService.ENGLISH_LANGUAGE_CODE },
            requireDisplayConfirmation: false,
            defaultProfile: false);

        PlatformSettingsProfileSO consoleProfile = CreateOrUpdateProfile(
            "Console Settings Profile",
            "Console",
            ResolveConsolePlatforms(),
            new[] { SettingsFeature.Audio, SettingsFeature.Language, SettingsFeature.GamepadRebind },
            new[] { FullScreenMode.FullScreenWindow },
            new[] { GameSettingsService.DEFAULT_LANGUAGE_CODE, GameSettingsService.ENGLISH_LANGUAGE_CODE },
            requireDisplayConfirmation: false,
            defaultProfile: false);

        GameObject displayConfirmModalPrefab = BuildDisplayConfirmModalPrefab();
        GameObject root = PrefabUtility.LoadPrefabContents(PREFAB_PATH);
        try
        {
            RebuildPrefab(root, new[] { pcProfile, mobileProfile, consoleProfile });
            ApplyUiFontRecursively(root);
            PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        UpdateViewCatalog(displayConfirmModalPrefab);
        RefreshSettingsPanelHostPrefabs();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Rebuilt static settings panel prefab: {PREFAB_PATH}");
    }

    private static void RefreshSettingsPanelHostPrefabs()
    {
        for (int i = 0; i < HOST_PREFAB_PATHS.Length; i++)
        {
            string hostPath = HOST_PREFAB_PATHS[i];
            GameObject hostPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(hostPath);
            if (hostPrefab == null)
            {
                continue;
            }

            GameObject hostRoot = PrefabUtility.LoadPrefabContents(hostPath);
            try
            {
                if (ReplaceNestedSettingsPanelInstance(hostRoot, hostPath))
                {
                    PrefabUtility.SaveAsPrefabAsset(hostRoot, hostPath);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(hostRoot);
            }
        }
    }

    private static bool ReplaceNestedSettingsPanelInstance(GameObject hostRoot, string hostPath)
    {
        SettingsPanelManager existingPanel = hostRoot.GetComponentInChildren<SettingsPanelManager>(true);
        if (existingPanel == null)
        {
            Debug.LogWarning($"Host prefab '{hostPath}' does not contain a {nameof(SettingsPanelManager)} instance.");
            return false;
        }

        GameObject panelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
        if (panelPrefab == null)
        {
            throw new MissingReferenceException($"Missing settings panel prefab at '{PREFAB_PATH}'.");
        }

        Transform parent = existingPanel.transform.parent;
        int siblingIndex = existingPanel.transform.GetSiblingIndex();
        string instanceName = existingPanel.gameObject.name;
        bool activeSelf = existingPanel.gameObject.activeSelf;
        int layer = existingPanel.gameObject.layer;
        RectTransformSnapshot rectTransformSnapshot = RectTransformSnapshot.Capture(existingPanel.GetComponent<RectTransform>());

        UnityEngine.Object.DestroyImmediate(existingPanel.gameObject);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(panelPrefab, parent);
        PrefabUtility.RevertPrefabInstance(instance, InteractionMode.AutomatedAction);
        instance.name = instanceName;
        instance.transform.SetSiblingIndex(siblingIndex);
        instance.SetActive(activeSelf);
        instance.layer = layer;
        rectTransformSnapshot.Apply(instance.GetComponent<RectTransform>());
        KeepOnlyRootPrefabOverrides(instance);

        SettingsPanelManager replacementPanel = instance.GetComponent<SettingsPanelManager>();
        RebindHostSettingsPanelReference(hostRoot, replacementPanel);
        return true;
    }

    private static void RebindHostSettingsPanelReference(GameObject hostRoot, SettingsPanelManager panel)
    {
        MenuUIPage[] menuPages = hostRoot.GetComponentsInChildren<MenuUIPage>(true);
        for (int i = 0; i < menuPages.Length; i++)
        {
            SetSettingsPanelReference(menuPages[i], panel);
        }

        GamePauseMenu[] pauseMenus = hostRoot.GetComponentsInChildren<GamePauseMenu>(true);
        for (int i = 0; i < pauseMenus.Length; i++)
        {
            SetSettingsPanelReference(pauseMenus[i], panel);
        }
    }

    private static void SetSettingsPanelReference(MonoBehaviour owner, SettingsPanelManager panel)
    {
        SerializedObject serializedObject = new(owner);
        SerializedProperty property = serializedObject.FindProperty("settingsPanel");
        if (property == null)
        {
            return;
        }

        property.objectReferenceValue = panel;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(owner);
    }

    private static void RebuildPrefab(GameObject root, PlatformSettingsProfileSO[] profiles)
    {
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(560f, 680f);

        CanvasGroup canvasGroup = root.GetComponent<CanvasGroup>();
        UIMotionPlayer motionPlayer = root.GetComponent<UIMotionPlayer>();
        SettingsPanelManager manager = root.GetComponent<SettingsPanelManager>();
        if (canvasGroup == null || motionPlayer == null || manager == null)
        {
            throw new MissingReferenceException("Settings Panel prefab must keep CanvasGroup, UIMotionPlayer, and SettingsPanelManager on root.");
        }

        for (int i = root.transform.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
        }

        TextMeshProUGUI title = CreateText("Title", root.transform, "设置", 40, FontStyles.Normal, TextAlignmentOptions.Center);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0f, -36f);
        titleRect.sizeDelta = new Vector2(220f, 52f);

        ScrollRect scrollRect = CreateScrollView(root.transform, out RectTransform content);

        GameObject audioSection = CreateSection(content, "AudioSection", "音量");
        SettingsSliderRow masterRow = CreateSliderRow(audioSection.transform, "MasterVolumeRow", "总音量");
        SettingsSliderRow sfxRow = CreateSliderRow(audioSection.transform, "SfxVolumeRow", "音效");
        SettingsSliderRow musicRow = CreateSliderRow(audioSection.transform, "MusicVolumeRow", "音乐");

        GameObject displaySection = CreateSection(content, "DisplaySection", "显示");
        SettingsOptionRow resolutionRow = CreateOptionRow(displaySection.transform, "ResolutionRow", "分辨率");
        SettingsOptionRow windowModeRow = CreateOptionRow(displaySection.transform, "WindowModeRow", "窗口模式");

        GameObject languageSection = CreateSection(content, "LanguageSection", "语言");
        SettingsOptionRow languageRow = CreateOptionRow(languageSection.transform, "LanguageRow", "语言");

        GameObject inputSection = CreateSection(content, "InputSection", "输入");
        List<SettingsRebindRow> rebindRows = new();
        IReadOnlyList<InputRebindService.RebindEntry> entries = InputRebindService.RebindEntries;
        for (int i = 0; i < entries.Count; i++)
        {
            rebindRows.Add(CreateRebindRow(inputSection.transform, entries[i]));
        }

        Button resetBindingsButton = CreateButton(inputSection.transform, "ResetBindingsButton", "恢复默认绑定", 160f, 36f);
        SetLayout(resetBindingsButton.gameObject, preferredHeight: 40f);

        GameObject touchSection = CreateSection(content, "TouchSection", "触控");

        RectTransform actions = CreateRect("ActionBar", root.transform);
        actions.anchorMin = new Vector2(0f, 0f);
        actions.anchorMax = new Vector2(1f, 0f);
        actions.pivot = new Vector2(0.5f, 0f);
        actions.anchoredPosition = new Vector2(0f, 22f);
        actions.sizeDelta = new Vector2(-56f, 48f);
        HorizontalLayoutGroup actionLayout = actions.gameObject.AddComponent<HorizontalLayoutGroup>();
        actionLayout.spacing = 18f;
        actionLayout.childAlignment = TextAnchor.MiddleCenter;
        actionLayout.childControlWidth = false;
        actionLayout.childControlHeight = false;
        actionLayout.childForceExpandWidth = false;
        actionLayout.childForceExpandHeight = false;
        Button saveButton = CreateButton(actions, "SaveButton", "保存", 128f, 40f);
        Button resetButton = CreateButton(actions, "ResetButton", "重置", 128f, 40f);

        SerializedObject managerObject = new(manager);
        SetObject(managerObject, "motionSource", motionPlayer);
        SetObject(managerObject, "canvasGroup", canvasGroup);
        SetObjectArray(managerObject, "platformProfiles", profiles);
        SetObject(managerObject, "defaultSelectable", masterRow.DefaultSelectable);
        SetObject(managerObject, "audioSection", audioSection);
        SetObject(managerObject, "displaySection", displaySection);
        SetObject(managerObject, "languageSection", languageSection);
        SetObject(managerObject, "inputSection", inputSection);
        SetObject(managerObject, "touchSection", touchSection);
        SetObject(managerObject, "masterVolume", masterRow);
        SetObject(managerObject, "sfxVolume", sfxRow);
        SetObject(managerObject, "musicVolume", musicRow);
        SetObject(managerObject, "resolutionRow", resolutionRow);
        SetObject(managerObject, "windowModeRow", windowModeRow);
        SetObject(managerObject, "languageRow", languageRow);
        SetObjectArray(managerObject, "rebindRows", rebindRows.ToArray());
        SetObject(managerObject, "resetBindingsButton", resetBindingsButton);
        SetObject(managerObject, "saveButton", saveButton);
        SetObject(managerObject, "resetButton", resetButton);
        managerObject.FindProperty("applyPreviewImmediately").boolValue = true;
        managerObject.ApplyModifiedPropertiesWithoutUndo();

        scrollRect.verticalNormalizedPosition = 1f;
        SetLayerRecursively(root, 5);
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

    private static void UpdateViewCatalog(GameObject displayConfirmModalPrefab)
    {
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
        SerializedProperty definitionProperty = FindViewDefinition(viewsProperty, DISPLAY_CONFIRM_MODAL_VIEW_ID);
        if (definitionProperty == null)
        {
            int newIndex = viewsProperty.arraySize;
            viewsProperty.arraySize++;
            definitionProperty = viewsProperty.GetArrayElementAtIndex(newIndex);
        }

        definitionProperty.FindPropertyRelative("id").stringValue = DISPLAY_CONFIRM_MODAL_VIEW_ID;
        definitionProperty.FindPropertyRelative("kind").intValue = (int)ViewKind.Modal;
        definitionProperty.FindPropertyRelative("layer").intValue = (int)ViewLayer.Modal;
        definitionProperty.FindPropertyRelative("prefab").objectReferenceValue = displayConfirmModalPrefab;
        definitionProperty.FindPropertyRelative("singleton").boolValue = true;
        definitionProperty.FindPropertyRelative("cacheOnClose").boolValue = true;
        definitionProperty.FindPropertyRelative("trackInBackStack").boolValue = false;
        definitionProperty.FindPropertyRelative("closeOnBackgroundClick").boolValue = false;
        definitionProperty.FindPropertyRelative("warmupCount").intValue = 0;
        definitionProperty.FindPropertyRelative("maxCachedInstancesOverride").intValue = -1;
        definitionProperty.FindPropertyRelative("allowDuplicateViewType").boolValue = false;
        catalogObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(catalog);
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

    private static ScrollRect CreateScrollView(Transform parent, out RectTransform content)
    {
        RectTransform scrollView = CreateRect("Scroll View", parent);
        scrollView.anchorMin = new Vector2(0f, 0f);
        scrollView.anchorMax = new Vector2(1f, 1f);
        scrollView.offsetMin = new Vector2(28f, 82f);
        scrollView.offsetMax = new Vector2(-28f, -82f);

        ScrollRect scrollRect = scrollView.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 32f;

        RectTransform viewport = CreateRect("Viewport", scrollView);
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = Vector2.zero;
        viewport.offsetMax = Vector2.zero;
        Image viewportImage = viewport.gameObject.AddComponent<Image>();
        viewportImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        viewportImage.color = new Color(1f, 1f, 1f, 0f);
        viewportImage.raycastTarget = true;
        viewport.gameObject.AddComponent<RectMask2D>();

        content = CreateRect("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;
        VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 8, 8);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewport;
        scrollRect.content = content;
        return scrollRect;
    }

    private static GameObject CreateSection(Transform parent, string objectName, string title)
    {
        RectTransform section = CreateRect(objectName, parent);
        VerticalLayoutGroup layout = section.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(4, 4, 0, 8);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        TextMeshProUGUI header = CreateText($"{objectName}Header", section, title, 18, FontStyles.Bold, TextAlignmentOptions.Left);
        SetLayout(header.gameObject, preferredHeight: 30f);
        return section.gameObject;
    }

    private static SettingsSliderRow CreateSliderRow(Transform parent, string objectName, string label)
    {
        RectTransform row = CreateRow(parent, objectName, 46f);
        TextMeshProUGUI labelText = CreateText("Label", row, label, 16, FontStyles.Normal, TextAlignmentOptions.Left);
        SetLayout(labelText.gameObject, preferredWidth: 88f, preferredHeight: 36f);

        Slider slider = CreateSlider(row);
        SetLayout(slider.gameObject, preferredWidth: 280f, flexibleWidth: 1f, preferredHeight: 28f);

        TextMeshProUGUI valueText = CreateText("Value", row, "100%", 16, FontStyles.Bold, TextAlignmentOptions.Right);
        SetLayout(valueText.gameObject, preferredWidth: 68f, preferredHeight: 36f);

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
        RectTransform row = CreateRow(parent, objectName, 44f);
        TextMeshProUGUI labelText = CreateText("Label", row, label, 16, FontStyles.Normal, TextAlignmentOptions.Left);
        SetLayout(labelText.gameObject, preferredWidth: 104f, preferredHeight: 36f);
        Button previousButton = CreateButton(row, "PreviousButton", "<", 42f, 34f);
        TextMeshProUGUI valueText = CreateText("Value", row, "-", 16, FontStyles.Bold, TextAlignmentOptions.Center);
        SetLayout(valueText.gameObject, preferredWidth: 220f, flexibleWidth: 1f, preferredHeight: 36f);
        Button nextButton = CreateButton(row, "NextButton", ">", 42f, 34f);

        SettingsOptionRow component = row.gameObject.AddComponent<SettingsOptionRow>();
        SerializedObject serializedObject = new(component);
        SetObject(serializedObject, "labelText", labelText);
        SetObject(serializedObject, "previousButton", previousButton);
        SetObject(serializedObject, "valueText", valueText);
        SetObject(serializedObject, "nextButton", nextButton);
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        return component;
    }

    private static SettingsRebindRow CreateRebindRow(Transform parent, InputRebindService.RebindEntry entry)
    {
        RectTransform row = CreateRow(parent, $"RebindRow_{SanitizeName(entry.DisplayLabel)}", 44f);
        TextMeshProUGUI labelText = CreateText("Label", row, entry.DisplayLabel, 15, FontStyles.Normal, TextAlignmentOptions.Left);
        SetLayout(labelText.gameObject, preferredWidth: 210f, preferredHeight: 36f);
        TextMeshProUGUI valueText = CreateText("Value", row, "-", 15, FontStyles.Bold, TextAlignmentOptions.Center);
        SetLayout(valueText.gameObject, preferredWidth: 130f, flexibleWidth: 1f, preferredHeight: 36f);
        Button rebindButton = CreateButton(row, "RebindButton", "重绑", 72f, 34f);

        SettingsRebindRow component = row.gameObject.AddComponent<SettingsRebindRow>();
        SerializedObject serializedObject = new(component);
        serializedObject.FindProperty("actionPath").stringValue = entry.ActionPath;
        serializedObject.FindProperty("compositePartName").stringValue = entry.CompositePartName ?? string.Empty;
        serializedObject.FindProperty("label").stringValue = entry.Label;
        serializedObject.FindProperty("controlScheme").stringValue = entry.ControlScheme;
        SetObject(serializedObject, "labelText", labelText);
        SetObject(serializedObject, "valueText", valueText);
        SetObject(serializedObject, "rebindButton", rebindButton);
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        return component;
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
        SetLayerRecursively(root, 5);
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
        layout.spacing = 10f;
        layout.padding = new RectOffset(0, 0, 0, 0);
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
        background.anchorMin = new Vector2(0f, 0.35f);
        background.anchorMax = new Vector2(1f, 0.65f);
        background.offsetMin = Vector2.zero;
        background.offsetMax = Vector2.zero;
        Image backgroundImage = background.gameObject.AddComponent<Image>();
        backgroundImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        backgroundImage.color = new Color(0.16f, 0.18f, 0.22f, 1f);

        RectTransform fillArea = CreateRect("Fill Area", root);
        fillArea.anchorMin = new Vector2(0f, 0.35f);
        fillArea.anchorMax = new Vector2(1f, 0.65f);
        fillArea.offsetMin = new Vector2(7f, 0f);
        fillArea.offsetMax = new Vector2(-7f, 0f);
        Image fillAreaImage = fillArea.gameObject.AddComponent<Image>();
        fillAreaImage.color = new Color(1f, 1f, 1f, 0f);
        RectTransform fill = CreateRect("Fill", fillArea);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;
        Image fillImage = fill.gameObject.AddComponent<Image>();
        fillImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        fillImage.color = new Color(0.55f, 0.72f, 0.95f, 1f);

        RectTransform handleArea = CreateRect("Handle Slide Area", root);
        handleArea.anchorMin = Vector2.zero;
        handleArea.anchorMax = Vector2.one;
        handleArea.offsetMin = new Vector2(7f, 0f);
        handleArea.offsetMax = new Vector2(-7f, 0f);
        Image handleAreaImage = handleArea.gameObject.AddComponent<Image>();
        handleAreaImage.color = new Color(1f, 1f, 1f, 0f);
        RectTransform handle = CreateRect("Handle", handleArea);
        handle.sizeDelta = new Vector2(18f, 18f);
        Image handleImage = handle.gameObject.AddComponent<Image>();
        handleImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        handleImage.color = new Color(0.94f, 0.96f, 1f, 1f);

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
        image.color = new Color(0.18f, 0.23f, 0.30f, 1f);
        Button button = rectTransform.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.28f, 0.36f, 0.46f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(0.12f, 0.16f, 0.20f, 1f);
        colors.disabledColor = new Color(0.12f, 0.14f, 0.17f, 0.6f);
        button.colors = colors;

        TextMeshProUGUI text = CreateText("Text", rectTransform, label, 16, FontStyles.Bold, TextAlignmentOptions.Center);
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
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
        serializedObject.FindProperty(propertyName).objectReferenceValue = value;
    }

    private static void SetObjectArray<T>(SerializedObject serializedObject, string propertyName, T[] values)
        where T : UnityEngine.Object
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
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
