#if UNITY_EDITOR
using System;
using Orange.UIFramework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class PropertiesPopupPrefabBuilder
{
    private const string PREFAB_FOLDER = GameContentAssetPaths.UIPrefabsProp;
    private const string PROP_SHOWER_PREFAB_PATH = GameContentAssetPaths.PropShowerPrefab;
    private const string PROP_ITEM_PREFAB_PATH = GameContentAssetPaths.CyberPropItemPrefab;
    private const string VIEW_CATALOG_PATH = GameContentAssetPaths.UIViewCatalog;
    private const string UI_FONT_PATH = GameContentAssetPaths.UIFonts + "/HYPixel11pxU-2 SDF.asset";
    private const string PANEL_MOTION_PATH = "Assets/GameContent/UI/Data/Motion/Shop Prop Shower Motion.asset";
    private const string CYBER_ICON_BUTTON_MOTION_PATH = "Assets/GameContent/UI/Data/Motion/Cyber Icon Button Motion.asset";
    private const string VIEW_ID = "popup.properties";
    private const int UI_LAYER = 5;

    private const string BACKGROUND_PATH = GameContentAssetPaths.PropertyShowFolder + "/all_prop_container_bg.png";
    private const string FRAME_PATH = GameContentAssetPaths.PropertyShowFolder + "/all_prop_container_frame.png";
    private const string TITLE_PATH = GameContentAssetPaths.PropertyShowFolder + "/title.png";
    private const string CLOSE_BUTTON_FRAME_PATH = GameContentAssetPaths.PropertyShowFolder + "/closebutton_frame.png";
    private const string CLOSE_BUTTON_ICON_PATH = GameContentAssetPaths.PropertyShowFolder + "/closebutton_icon.png";
    private const string PROP_FRAME_PATH = GameContentAssetPaths.PropertyShowFolder + "/prop_frame.png";
    private const string PROP_ICONS_PATH = GameContentAssetPaths.PropertyShowPropIconsAtlas;

    private static readonly Color LabelColor = new(1f, 0.16f, 0.58f, 1f);
    private static readonly Color ValueColor = new(1f, 1f, 1f, 1f);
    private static readonly Color DividerColor = new(1f, 0.08f, 0.58f, 0.9f);

    private static TMP_FontAsset cachedUiFont;

    [MenuItem("Tools/Survivors/Rebuild Properties Popup UI")]
    public static void Rebuild()
    {
        EnsureFolder(PREFAB_FOLDER);

        GameObject itemPrefab = BuildPropItemPrefab();
        GameObject popupPrefab = BuildPropShowerPrefab(itemPrefab);
        UpdateViewCatalog(popupPrefab);
        PropPresentationCatalogIconBinder.BindDefaultCatalogIcons();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Rebuilt properties popup UI: {PROP_SHOWER_PREFAB_PATH}, {PROP_ITEM_PREFAB_PATH}");
    }

    private static GameObject BuildPropItemPrefab()
    {
        GameObject root = new("CyberPropItem", typeof(RectTransform), typeof(PropContainer));
        try
        {
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(560f, 72f);

            RectTransform visualRoot = CreateVisualRoot(root.transform);

            Image iconFrame = CreateImage("PropFrame", visualRoot, ResolveRequiredSprite(PROP_FRAME_PATH));
            iconFrame.preserveAspect = true;
            RectTransform iconFrameRect = iconFrame.rectTransform;
            iconFrameRect.anchorMin = new Vector2(0f, 0.5f);
            iconFrameRect.anchorMax = new Vector2(0f, 0.5f);
            iconFrameRect.pivot = new Vector2(0.5f, 0.5f);
            iconFrameRect.anchoredPosition = new Vector2(38f, 0f);
            iconFrameRect.sizeDelta = new Vector2(78f, 70f);

            Image icon = CreateImage("Icon", visualRoot, ResolveRequiredSprite(PROP_ICONS_PATH, "prop_icons_0"));
            icon.preserveAspect = true;
            RectTransform iconRect = icon.rectTransform;
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = iconFrameRect.anchoredPosition;
            iconRect.sizeDelta = new Vector2(44f, 44f);

            TextMeshProUGUI nameText = CreateText(
                "Name",
                visualRoot,
                "攻击力",
                24,
                FontStyles.Bold,
                TextAlignmentOptions.MidlineLeft,
                LabelColor);
            RectTransform nameRect = nameText.rectTransform;
            nameRect.anchorMin = new Vector2(0f, 0.5f);
            nameRect.anchorMax = new Vector2(0f, 0.5f);
            nameRect.pivot = new Vector2(0f, 0.5f);
            nameRect.anchoredPosition = new Vector2(104f, 7f);
            nameRect.sizeDelta = new Vector2(300f, 42f);

            TextMeshProUGUI valueText = CreateText(
                "Value",
                visualRoot,
                "+0.0",
                24,
                FontStyles.Bold,
                TextAlignmentOptions.MidlineRight,
                ValueColor);
            RectTransform valueRect = valueText.rectTransform;
            valueRect.anchorMin = new Vector2(1f, 0.5f);
            valueRect.anchorMax = new Vector2(1f, 0.5f);
            valueRect.pivot = new Vector2(1f, 0.5f);
            valueRect.anchoredPosition = new Vector2(-26f, 7f);
            valueRect.sizeDelta = new Vector2(160f, 42f);

            Image divider = CreateBuiltinImage("Divider", visualRoot, DividerColor);
            RectTransform dividerRect = divider.rectTransform;
            dividerRect.anchorMin = new Vector2(0f, 0f);
            dividerRect.anchorMax = new Vector2(1f, 0f);
            dividerRect.pivot = new Vector2(0.5f, 0f);
            dividerRect.offsetMin = new Vector2(104f, 10f);
            dividerRect.offsetMax = new Vector2(-28f, 15f);

            SerializedObject containerObject = new(root.GetComponent<PropContainer>());
            SetObject(containerObject, "propImage", icon);
            SetObject(containerObject, "propText", nameText);
            SetObject(containerObject, "propValueText", valueText);
            containerObject.ApplyModifiedPropertiesWithoutUndo();

            ApplyUiFontRecursively(root);
            SetLayerRecursively(root, UI_LAYER);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PROP_ITEM_PREFAB_PATH);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Failed to save prop item prefab at '{PROP_ITEM_PREFAB_PATH}'.");
            }

            return prefab;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static GameObject BuildPropShowerPrefab(GameObject itemPrefab)
    {
        if (itemPrefab == null)
        {
            throw new MissingReferenceException($"Missing prop item prefab at '{PROP_ITEM_PREFAB_PATH}'.");
        }

        PropContainer itemContainer = itemPrefab.GetComponent<PropContainer>();
        if (itemContainer == null)
        {
            throw new MissingReferenceException($"Prop item prefab '{PROP_ITEM_PREFAB_PATH}' must have {nameof(PropContainer)} on the root.");
        }

        GameObject root = new(
            "PropShower",
            typeof(RectTransform),
            typeof(CanvasGroup),
            typeof(UIMotionPlayer),
            typeof(UIMotionTransition),
            typeof(PropertiesPopup),
            typeof(PropertiesIconTextDescriber));

        try
        {
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(1600f, 900f);

            CanvasGroup canvasGroup = root.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            UIMotionPlayer motionPlayer = root.GetComponent<UIMotionPlayer>();
            ConfigurePanelMotion(root.GetComponent<UIMotionTransition>(), motionPlayer);

            RectTransform visualRoot = CreateVisualRoot(root.transform);

            Image background = CreateImage("Background", visualRoot, ResolveRequiredSprite(BACKGROUND_PATH), raycastTarget: true);
            StretchToParent(background.rectTransform);

            Image frame = CreateImage("Frame", visualRoot, ResolveRequiredSprite(FRAME_PATH));
            StretchToParent(frame.rectTransform);

            Image title = CreateImage("Title", visualRoot, ResolveRequiredSprite(TITLE_PATH));
            title.preserveAspect = true;
            RectTransform titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -28f);
            titleRect.sizeDelta = new Vector2(740f, 262f);

            Button closeButton = CreateCloseButton(
                visualRoot,
                ResolveRequiredSprite(CLOSE_BUTTON_FRAME_PATH),
                ResolveRequiredSprite(CLOSE_BUTTON_ICON_PATH),
                out RectTransform closeIcon);
            RectTransform closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(0.5f, 0.5f);
            closeRect.anchoredPosition = new Vector2(-130f, -88f);
            ConfigureCyberIconButtonMotion(closeButton, closeIcon);

            RectTransform scrollRoot = CreateRect("PropertyScrollView", visualRoot);
            scrollRoot.anchorMin = Vector2.zero;
            scrollRoot.anchorMax = Vector2.one;
            scrollRoot.offsetMin = new Vector2(145f, 92f);
            scrollRoot.offsetMax = new Vector2(-145f, -205f);
            ScrollRect scrollRect = scrollRoot.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = true;
            scrollRect.scrollSensitivity = 38f;

            RectTransform viewport = CreateRect("Viewport", scrollRoot);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = Vector2.zero;
            Image viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            viewportImage.color = new Color(1f, 1f, 1f, 0f);
            viewportImage.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();

            RectTransform content = CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            GridLayoutGroup grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(24, 24, 10, 18);
            grid.cellSize = new Vector2(560f, 72f);
            grid.spacing = new Vector2(92f, 12f);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;

            ContentSizeFitter contentSizeFitter = content.gameObject.AddComponent<ContentSizeFitter>();
            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewport;
            scrollRect.content = content;

            Image columnDivider = CreateBuiltinImage("ColumnDivider", visualRoot, DividerColor);
            RectTransform dividerRect = columnDivider.rectTransform;
            dividerRect.anchorMin = new Vector2(0.5f, 0f);
            dividerRect.anchorMax = new Vector2(0.5f, 1f);
            dividerRect.pivot = new Vector2(0.5f, 0.5f);
            dividerRect.offsetMin = new Vector2(-1.5f, 110f);
            dividerRect.offsetMax = new Vector2(1.5f, -225f);

            PropertiesIconTextDescriber describer = root.GetComponent<PropertiesIconTextDescriber>();
            SerializedObject describerObject = new(describer);
            SetObject(describerObject, "contentRoot", content);
            SetObject(describerObject, "propContainerPrefab", itemContainer);
            describerObject.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject popupObject = new(root.GetComponent<PropertiesPopup>());
            SetObject(popupObject, "closeButton", closeButton);
            SetObject(popupObject, "propertiesDescriber", describer);
            popupObject.ApplyModifiedPropertiesWithoutUndo();

            ApplyUiFontRecursively(root);
            SetLayerRecursively(root, UI_LAYER);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PROP_SHOWER_PREFAB_PATH);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Failed to save player properties popup prefab at '{PROP_SHOWER_PREFAB_PATH}'.");
            }

            return prefab;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void ConfigurePanelMotion(UIMotionTransition transition, UIMotionPlayer motionPlayer)
    {
        UIMotionDefinition definition = AssetDatabase.LoadAssetAtPath<UIMotionDefinition>(PANEL_MOTION_PATH);
        if (definition == null)
        {
            throw new MissingReferenceException($"Missing required UI motion definition at '{PANEL_MOTION_PATH}'.");
        }

        SerializedObject motionObject = new(motionPlayer);
        SetObject(motionObject, "definition", definition);
        motionObject.FindProperty("refreshDefaultsOnEnable").boolValue = true;
        motionObject.FindProperty("stopAllChannelsOnDestroy").boolValue = true;
        motionObject.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject transitionObject = new(transition);
        SetObject(transitionObject, "motionSource", motionPlayer);
        transitionObject.FindProperty("autoResolveInChildren").boolValue = true;
        transitionObject.FindProperty("hideImmediatelyBeforeEnter").boolValue = true;
        transitionObject.FindProperty("showImmediatelyWhenSkipped").boolValue = true;
        transitionObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Button CreateCloseButton(
        Transform parent,
        Sprite frameSprite,
        Sprite iconSprite,
        out RectTransform iconTransform)
    {
        RectTransform root = CreateRect("CloseButton", parent);
        root.sizeDelta = new Vector2(102f, 102f);

        Button button = root.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.82f, 0.96f, 1f);
        colors.selectedColor = Color.white;
        colors.pressedColor = new Color(0.86f, 0.62f, 0.80f, 1f);
        colors.disabledColor = new Color(0.32f, 0.32f, 0.36f, 0.45f);
        button.colors = colors;

        Image frame = CreateImage("Frame", root, frameSprite, raycastTarget: true);
        frame.preserveAspect = true;
        StretchToParent(frame.rectTransform);
        button.targetGraphic = frame;

        Image icon = CreateImage("Icon", root, iconSprite);
        icon.preserveAspect = true;
        iconTransform = icon.rectTransform;
        iconTransform.anchorMin = new Vector2(0.5f, 0.5f);
        iconTransform.anchorMax = new Vector2(0.5f, 0.5f);
        iconTransform.pivot = new Vector2(0.5f, 0.5f);
        iconTransform.anchoredPosition = Vector2.zero;
        iconTransform.sizeDelta = new Vector2(58f, 58f);

        return button;
    }

    private static void ConfigureCyberIconButtonMotion(Button button, Transform iconTarget)
    {
        UIMotionDefinition definition = AssetDatabase.LoadAssetAtPath<UIMotionDefinition>(CYBER_ICON_BUTTON_MOTION_PATH);
        if (definition == null)
        {
            throw new MissingReferenceException($"Missing required UI motion definition at '{CYBER_ICON_BUTTON_MOTION_PATH}'.");
        }

        button.transition = Selectable.Transition.None;
        RectTransform target = button.GetComponent<RectTransform>();
        CanvasGroup canvasGroup = button.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = button.gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        UIMotionPlayer player = button.GetComponent<UIMotionPlayer>();
        if (player == null)
        {
            player = button.gameObject.AddComponent<UIMotionPlayer>();
        }

        SerializedObject playerObject = new(player);
        SetObject(playerObject, "definition", definition);
        SerializedProperty targetBindings = playerObject.FindProperty("targets").FindPropertyRelative("bindings");
        targetBindings.arraySize = 2;
        SetMotionTargetBinding(targetBindings.GetArrayElementAtIndex(0), UIMotionTargetKeys.SELF, target);
        SetMotionTargetBinding(targetBindings.GetArrayElementAtIndex(1), "Icon", iconTarget != null ? iconTarget : target);
        playerObject.FindProperty("refreshDefaultsOnEnable").boolValue = false;
        playerObject.FindProperty("stopAllChannelsOnDestroy").boolValue = true;
        playerObject.ApplyModifiedPropertiesWithoutUndo();

        UIMotionTrigger trigger = button.GetComponent<UIMotionTrigger>();
        if (trigger == null)
        {
            trigger = button.gameObject.AddComponent<UIMotionTrigger>();
        }

        SerializedObject triggerObject = new(trigger);
        SetObject(triggerObject, "player", player);
        SerializedProperty bindings = triggerObject.FindProperty("bindings");
        bindings.arraySize = 6;
        SetMotionTriggerBinding(bindings.GetArrayElementAtIndex(0), UIMotionTriggerEvent.OnEnable, UIMotionClipIds.VISIBLE, false);
        SetMotionTriggerBinding(bindings.GetArrayElementAtIndex(1), UIMotionTriggerEvent.PointerEnter, UIMotionClipIds.HOVER_IN, false);
        SetMotionTriggerBinding(bindings.GetArrayElementAtIndex(2), UIMotionTriggerEvent.PointerExit, UIMotionClipIds.HOVER_OUT, false);
        SetMotionTriggerBinding(bindings.GetArrayElementAtIndex(3), UIMotionTriggerEvent.PointerDown, UIMotionClipIds.PRESS, true);
        SetMotionTriggerBinding(bindings.GetArrayElementAtIndex(4), UIMotionTriggerEvent.PointerUp, UIMotionClipIds.RELEASE, true);
        SetMotionTriggerBinding(bindings.GetArrayElementAtIndex(5), UIMotionTriggerEvent.PointerClick, UIMotionClipIds.CLICK_PULSE, true);
        triggerObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void UpdateViewCatalog(GameObject popupPrefab)
    {
        if (popupPrefab == null)
        {
            throw new MissingReferenceException($"Missing player properties popup prefab at '{PROP_SHOWER_PREFAB_PATH}'.");
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
            VIEW_ID,
            ViewKind.Popup,
            ViewLayer.Popup,
            popupPrefab,
            singleton: true,
            cacheOnClose: true,
            trackInBackStack: true,
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

    private static RectTransform CreateVisualRoot(Transform parent)
    {
        RectTransform visualRoot = CreateRect("VisualRoot", parent);
        StretchToParent(visualRoot);
        return visualRoot;
    }

    private static Image CreateImage(string objectName, Transform parent, Sprite sprite, bool raycastTarget = false)
    {
        RectTransform rectTransform = CreateRect(objectName, parent);
        Image image = rectTransform.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.color = Color.white;
        image.raycastTarget = raycastTarget;
        return image;
    }

    private static Image CreateBuiltinImage(string objectName, Transform parent, Color color)
    {
        Image image = CreateImage(
            objectName,
            parent,
            AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"));
        image.type = Image.Type.Sliced;
        image.color = color;
        return image;
    }

    private static TextMeshProUGUI CreateText(
        string objectName,
        Transform parent,
        string value,
        int fontSize,
        FontStyles fontStyle,
        TextAlignmentOptions alignment,
        Color color)
    {
        RectTransform rectTransform = CreateRect(objectName, parent);
        TextMeshProUGUI text = rectTransform.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = color;
        text.alignment = alignment;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        ApplyUiFont(text, ResolveRequiredUiFont());
        return text;
    }

    private static RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject gameObject = new(objectName, typeof(RectTransform));
        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        return rectTransform;
    }

    private static void StretchToParent(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private static Sprite ResolveRequiredSprite(string assetPath, string spriteName = null)
    {
        Sprite sprite = LoadSprite(assetPath, spriteName);
        if (sprite == null)
        {
            string suffix = string.IsNullOrWhiteSpace(spriteName) ? string.Empty : $" ({spriteName})";
            throw new MissingReferenceException($"Missing required player properties UI sprite '{assetPath}'{suffix}.");
        }

        return sprite;
    }

    private static Sprite LoadSprite(string assetPath, string spriteName)
    {
        if (string.IsNullOrWhiteSpace(spriteName))
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite sprite && string.Equals(sprite.name, spriteName, StringComparison.Ordinal))
            {
                return sprite;
            }
        }

        return null;
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

    private static void SetMotionTargetBinding(SerializedProperty binding, string key, Transform target)
    {
        binding.FindPropertyRelative("key").stringValue = key;
        binding.FindPropertyRelative("target").objectReferenceValue = target;
    }

    private static void SetMotionTriggerBinding(
        SerializedProperty binding,
        UIMotionTriggerEvent triggerEvent,
        string clipId,
        bool requireLeftButton)
    {
        binding.FindPropertyRelative("triggerEvent").enumValueIndex = (int)triggerEvent;
        binding.FindPropertyRelative("clipId").stringValue = clipId;
        binding.FindPropertyRelative("requireLeftButton").boolValue = requireLeftButton;
        binding.FindPropertyRelative("delay").floatValue = 0f;
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

    private static void SetLayerRecursively(GameObject target, int layer)
    {
        target.layer = layer;
        for (int i = 0; i < target.transform.childCount; i++)
        {
            SetLayerRecursively(target.transform.GetChild(i).gameObject, layer);
        }
    }
}
#endif
