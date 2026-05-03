using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class ItemQualityPreviewSceneController : MonoBehaviour
{
    private const string WEAPON_DATA_LIST_PATH = "Data/Weapon Data List";
    private const string ACCESSORY_DATA_LIST_PATH = "Data/Accessory Data List";
    private const string INVENTORY_ITEM_PREFAB_PATH = "Prefabs/New UI/Pages/Shop/Inventory Item";
    private const string SHOP_ITEM_PREFAB_PATH = "Prefabs/New UI/Pages/Shop/Shop Item";
    private const string WEAPON_POPUP_PREFAB_PATH = "Prefabs/New UI/Pages/Shop/Weapon Operate Popup";
    private const string ACCESSORY_POPUP_PREFAB_PATH = "Prefabs/New UI/Pages/Shop/Accessory Info Popup";

    [Header("Canvas")]
    [SerializeField] private Vector2 referenceResolution = new(1920f, 1080f);
    [SerializeField] private Color backgroundColor = new(0.08f, 0.09f, 0.11f, 1f);
    [SerializeField] private Color sectionColor = new(0.13f, 0.15f, 0.18f, 0.92f);
    [SerializeField] private Color cellColor = new(0.16f, 0.18f, 0.22f, 0.92f);

    [Header("Sizing")]
    [SerializeField] private Vector2 inventoryPreviewSize = new(118f, 146f);
    [SerializeField] private Vector2 shopPreviewSize = new(280f, 248f);
    [SerializeField] private Vector2 popupPreviewSize = new(520f, 296f);

    private readonly List<IDisposable> spawnedDisposables = new();
    private readonly List<GameObject> spawnedObjects = new();

    private Canvas canvas;
    private CanvasScaler canvasScaler;
    private GraphicRaycaster graphicRaycaster;
    private RectTransform contentRoot;

    private void Start()
    {
        RebuildPreview();
    }

    private void OnDestroy()
    {
        DisposeSpawnedObjects();
    }

    [ContextMenu("Rebuild Preview")]
    public void RebuildPreview()
    {
        EnsureCanvas();
        EnsureEventSystem();
        DisposeSpawnedObjects();
        ClearGeneratedChildren();
        BuildLayout();
        PopulatePreview();
    }

    private void EnsureCanvas()
    {
        canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = false;

        canvasScaler = GetComponent<CanvasScaler>();
        if (canvasScaler == null)
        {
            canvasScaler = gameObject.AddComponent<CanvasScaler>();
        }

        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = referenceResolution;
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        canvasScaler.matchWidthOrHeight = 0.5f;

        graphicRaycaster = GetComponent<GraphicRaycaster>();
        if (graphicRaycaster == null)
        {
            graphicRaycaster = gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    private static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        eventSystemObject.transform.SetAsLastSibling();
    }

    private void BuildLayout()
    {
        RectTransform backgroundRoot = CreateRectTransform("Background", transform);
        Stretch(backgroundRoot, 24f);

        Image backgroundImage = backgroundRoot.gameObject.AddComponent<Image>();
        backgroundImage.color = backgroundColor;

        contentRoot = CreateRectTransform("Content", backgroundRoot);
        Stretch(contentRoot, 36f);

        VerticalLayoutGroup contentLayout = contentRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(20, 20, 20, 20);
        contentLayout.spacing = 18f;
        contentLayout.childAlignment = TextAnchor.UpperCenter;
        contentLayout.childControlHeight = false;
        contentLayout.childControlWidth = true;
        contentLayout.childForceExpandHeight = false;
        contentLayout.childForceExpandWidth = true;

        ContentSizeFitter contentSizeFitter = contentRoot.gameObject.AddComponent<ContentSizeFitter>();
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        CreateHeading("Item Quality Preview", 42, FontStyles.Bold);
        CreateParagraph("Open this scene and press Play to inspect icon outline glow and name quality effects on inventory, shop and popup prefabs.", 20);
    }

    private void PopulatePreview()
    {
        WeaponDataListSO weaponDataList = Resources.Load<WeaponDataListSO>(WEAPON_DATA_LIST_PATH);
        AccessoryDataListSO accessoryDataList = Resources.Load<AccessoryDataListSO>(ACCESSORY_DATA_LIST_PATH);

        WeaponDataSO sampleWeapon = GetFirstWeaponWithIcon(weaponDataList);
        AccessoryDataSO[] sampleAccessories = GetAccessorySamples(accessoryDataList);

        if (sampleWeapon == null)
        {
            CreateWarning("No weapon data with icon was found. Check Resources/Data/Weapon Data List.");
            return;
        }

        if (!HasAccessorySamples(sampleAccessories))
        {
            CreateWarning("No accessory data with icon was found. Check Resources/Data/Accessory Data List.");
            return;
        }

        BuildInventorySection(sampleWeapon, sampleAccessories);
        BuildShopSection(sampleWeapon, sampleAccessories);
        BuildPopupSection(sampleWeapon, sampleAccessories[3] ?? sampleAccessories[2] ?? sampleAccessories[1] ?? sampleAccessories[0]);
    }

    private void BuildInventorySection(WeaponDataSO sampleWeapon, AccessoryDataSO[] sampleAccessories)
    {
        RectTransform section = CreateSection("Inventory Item Preview");
        CreateParagraph(section, $"Weapon sample: {sampleWeapon.ItemName}", 18);
        RectTransform weaponRow = CreateRow(section, 14f);
        for (int level = WeaponLevelHelper.MinLevel; level <= WeaponLevelHelper.MaxLevel; level++)
        {
            InventoryItem preview = LoadAndSpawnInventoryItem(sampleWeapon, level, weaponRow, $"Lv.{level}", inventoryPreviewSize);
            if (preview != null)
            {
                spawnedDisposables.Add(preview);
            }
        }

        CreateParagraph(section, "Accessory rarity samples", 18);
        RectTransform accessoryRow = CreateRow(section, 14f);
        for (int rarity = 0; rarity < sampleAccessories.Length; rarity++)
        {
            AccessoryDataSO accessory = sampleAccessories[rarity];
            if (accessory == null)
            {
                continue;
            }

            InventoryItem preview = LoadAndSpawnInventoryItem(accessory, accessory.Rarity, accessoryRow, $"Rarity {accessory.Rarity}", inventoryPreviewSize);
            if (preview != null)
            {
                spawnedDisposables.Add(preview);
            }
        }
    }

    private void BuildShopSection(WeaponDataSO sampleWeapon, AccessoryDataSO[] sampleAccessories)
    {
        RectTransform section = CreateSection("Shop Card Preview");
        RectTransform row = CreateRow(section, 18f);

        int[] weaponLevels = { 1, 3, 6 };
        for (int i = 0; i < weaponLevels.Length; i++)
        {
            int level = weaponLevels[i];
            ShopItemData shopItemData = new ShopItemData
            {
                ItemData = sampleWeapon,
                Level = level,
                Lock = level == 6
            };

            ShopItemContainer container = LoadAndSpawnShopItem(
                shopItemData,
                row,
                $"Weapon Lv.{level}",
                shopPreviewSize);

            if (container != null)
            {
                spawnedDisposables.Add(container);
            }
        }

        int[] accessoryRarities = { 0, 2, 3 };
        for (int i = 0; i < accessoryRarities.Length; i++)
        {
            int rarity = accessoryRarities[i];
            AccessoryDataSO accessory = FindAccessoryByRarity(sampleAccessories, rarity);
            if (accessory == null)
            {
                continue;
            }

            ShopItemData shopItemData = new ShopItemData
            {
                ItemData = accessory,
                Level = 0,
                Lock = rarity >= 2
            };

            ShopItemContainer container = LoadAndSpawnShopItem(
                shopItemData,
                row,
                $"Accessory R{rarity}",
                shopPreviewSize);

            if (container != null)
            {
                spawnedDisposables.Add(container);
            }
        }
    }

    private void BuildPopupSection(WeaponDataSO sampleWeapon, AccessoryDataSO sampleAccessory)
    {
        RectTransform section = CreateSection("Popup Preview");
        RectTransform row = CreateRow(section, 24f);

        WeaponOperatePopup weaponPopupPrefab = Resources.Load<WeaponOperatePopup>(WEAPON_POPUP_PREFAB_PATH);
        if (weaponPopupPrefab != null)
        {
            RectTransform cell = CreatePreviewCell(row, "Weapon Popup Lv.6", popupPreviewSize);
            WeaponOperatePopup popup = Instantiate(weaponPopupPrefab, cell, false);
            popup.transform.localScale = Vector3.one;
            ConfigureLayoutElement(popup.gameObject, popupPreviewSize);
            popup.Configure(new InventoryItemOperateResource(
                "preview_weapon_popup",
                sampleWeapon,
                WeaponLevelHelper.MaxLevel,
                WeaponPriceHelper.GetPrice(sampleWeapon.ItemPrice, WeaponLevelHelper.MaxLevel),
                new WeaponLevelDescribable(sampleWeapon, WeaponLevelHelper.MaxLevel)));
            spawnedDisposables.Add(popup);
            spawnedObjects.Add(popup.gameObject);
        }

        if (sampleAccessory != null)
        {
            AccessoryInfoPopup accessoryPopupPrefab = Resources.Load<AccessoryInfoPopup>(ACCESSORY_POPUP_PREFAB_PATH);
            if (accessoryPopupPrefab != null)
            {
                RectTransform cell = CreatePreviewCell(row, $"Accessory Popup R{sampleAccessory.Rarity}", popupPreviewSize);
                AccessoryInfoPopup popup = Instantiate(accessoryPopupPrefab, cell, false);
                popup.transform.localScale = Vector3.one;
                ConfigureLayoutElement(popup.gameObject, popupPreviewSize);
                popup.Configure(new InventoryItemOperateResource(
                    "preview_accessory_popup",
                    sampleAccessory,
                    sampleAccessory.Rarity,
                    sampleAccessory.RecyclePrice,
                    sampleAccessory));
                spawnedDisposables.Add(popup);
                spawnedObjects.Add(popup.gameObject);
            }
        }
    }

    private InventoryItem LoadAndSpawnInventoryItem(ItemDataSO itemData, int qualityValue, RectTransform row, string label, Vector2 size)
    {
        InventoryItem prefab = Resources.Load<InventoryItem>(INVENTORY_ITEM_PREFAB_PATH);
        if (prefab == null)
        {
            CreateWarning($"Missing inventory prefab at Resources/{INVENTORY_ITEM_PREFAB_PATH}");
            return null;
        }

        RectTransform cell = CreatePreviewCell(row, label, size);
        InventoryItem preview = Instantiate(prefab, cell, false);
        preview.transform.localScale = Vector3.one;
        ConfigureLayoutElement(preview.gameObject, size);
        preview.Configure($"preview_{itemData.ItemName}_{qualityValue}", itemData, qualityValue);
        spawnedObjects.Add(preview.gameObject);
        return preview;
    }

    private ShopItemContainer LoadAndSpawnShopItem(ShopItemData shopItemData, RectTransform row, string label, Vector2 size)
    {
        ShopItemContainer prefab = Resources.Load<ShopItemContainer>(SHOP_ITEM_PREFAB_PATH);
        if (prefab == null)
        {
            CreateWarning($"Missing shop item prefab at Resources/{SHOP_ITEM_PREFAB_PATH}");
            return null;
        }

        RectTransform cell = CreatePreviewCell(row, label, size);
        ShopItemContainer preview = Instantiate(prefab, cell, false);
        preview.transform.localScale = Vector3.one;
        ConfigureLayoutElement(preview.gameObject, size);
        preview.Configure(new InfoAddIndex<ShopItemData>(shopItemData, spawnedObjects.Count));
        spawnedObjects.Add(preview.gameObject);
        return preview;
    }

    private RectTransform CreateSection(string title)
    {
        RectTransform section = CreateRectTransform(title.Replace(' ', '_'), contentRoot);
        Image sectionImage = section.gameObject.AddComponent<Image>();
        sectionImage.color = sectionColor;

        VerticalLayoutGroup layoutGroup = section.gameObject.AddComponent<VerticalLayoutGroup>();
        layoutGroup.padding = new RectOffset(20, 20, 20, 20);
        layoutGroup.spacing = 12f;
        layoutGroup.childAlignment = TextAnchor.UpperCenter;
        layoutGroup.childControlHeight = false;
        layoutGroup.childControlWidth = true;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childForceExpandWidth = true;

        ContentSizeFitter fitter = section.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        LayoutElement layoutElement = section.gameObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = 80f;

        CreateHeading(section, title, 28, FontStyles.Bold);
        return section;
    }

    private RectTransform CreateRow(RectTransform parent, float spacing)
    {
        RectTransform row = CreateRectTransform("Row", parent);
        HorizontalLayoutGroup layoutGroup = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layoutGroup.spacing = spacing;
        layoutGroup.childAlignment = TextAnchor.MiddleCenter;
        layoutGroup.childControlHeight = false;
        layoutGroup.childControlWidth = false;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childForceExpandWidth = false;

        ContentSizeFitter fitter = row.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return row;
    }

    private RectTransform CreatePreviewCell(RectTransform parent, string label, Vector2 size)
    {
        RectTransform cell = CreateRectTransform(label.Replace(' ', '_'), parent);
        Image cellImage = cell.gameObject.AddComponent<Image>();
        cellImage.color = cellColor;

        VerticalLayoutGroup layoutGroup = cell.gameObject.AddComponent<VerticalLayoutGroup>();
        layoutGroup.padding = new RectOffset(12, 12, 12, 12);
        layoutGroup.spacing = 8f;
        layoutGroup.childAlignment = TextAnchor.UpperCenter;
        layoutGroup.childControlWidth = false;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;

        ContentSizeFitter fitter = cell.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        LayoutElement layoutElement = cell.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = size.x + 24f;
        layoutElement.preferredHeight = size.y + 64f;

        CreateParagraph(cell, label, 18);
        return cell;
    }

    private void CreateHeading(string text, int fontSize, FontStyles fontStyle)
    {
        CreateHeading(contentRoot, text, fontSize, fontStyle);
    }

    private static void CreateHeading(RectTransform parent, string text, int fontSize, FontStyles fontStyle)
    {
        TextMeshProUGUI label = CreateText(parent, text, fontSize, fontStyle, new Color(0.95f, 0.96f, 0.98f, 1f));
        label.alignment = TextAlignmentOptions.Center;
        LayoutElement layoutElement = label.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = fontSize + 18f;
    }

    private void CreateParagraph(string text, int fontSize)
    {
        CreateParagraph(contentRoot, text, fontSize);
    }

    private static void CreateParagraph(RectTransform parent, string text, int fontSize)
    {
        TextMeshProUGUI label = CreateText(parent, text, fontSize, FontStyles.Normal, new Color(0.82f, 0.86f, 0.9f, 1f));
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = true;
        LayoutElement layoutElement = label.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = fontSize + 20f;
    }

    private void CreateWarning(string text)
    {
        TextMeshProUGUI warning = CreateText(contentRoot, text, 22, FontStyles.Bold, new Color(1f, 0.45f, 0.45f, 1f));
        warning.alignment = TextAlignmentOptions.Center;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string text, int fontSize, FontStyles fontStyle, Color color)
    {
        GameObject textObject = new("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);

        TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = fontStyle;
        label.color = color;
        label.enableAutoSizing = false;
        label.raycastTarget = false;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        return label;
    }

    private static RectTransform CreateRectTransform(string objectName, Transform parent)
    {
        GameObject gameObject = new(objectName, typeof(RectTransform));
        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        return rectTransform;
    }

    private static void Stretch(RectTransform rectTransform, float inset)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = new Vector2(inset, inset);
        rectTransform.offsetMax = new Vector2(-inset, -inset);
    }

    private static void ConfigureLayoutElement(GameObject gameObject, Vector2 size)
    {
        LayoutElement layoutElement = gameObject.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = gameObject.AddComponent<LayoutElement>();
        }

        layoutElement.preferredWidth = size.x;
        layoutElement.preferredHeight = size.y;
        layoutElement.minWidth = size.x;
        layoutElement.minHeight = size.y;
    }

    private WeaponDataSO GetFirstWeaponWithIcon(WeaponDataListSO weaponDataList)
    {
        if (weaponDataList == null || weaponDataList.Weapons == null)
        {
            return null;
        }

        for (int i = 0; i < weaponDataList.Weapons.Length; i++)
        {
            WeaponDataSO weapon = weaponDataList.Weapons[i];
            if (weapon != null && weapon.ItemIcon != null)
            {
                return weapon;
            }
        }

        return null;
    }

    private AccessoryDataSO[] GetAccessorySamples(AccessoryDataListSO accessoryDataList)
    {
        AccessoryDataSO[] samples = new AccessoryDataSO[4];
        if (accessoryDataList == null || accessoryDataList.Accessories == null)
        {
            return samples;
        }

        for (int i = 0; i < accessoryDataList.Accessories.Length; i++)
        {
            AccessoryDataSO accessory = accessoryDataList.Accessories[i];
            if (accessory == null || accessory.ItemIcon == null)
            {
                continue;
            }

            int rarity = Mathf.Clamp(accessory.Rarity, 0, samples.Length - 1);
            if (samples[rarity] == null)
            {
                samples[rarity] = accessory;
            }
        }

        return samples;
    }

    private static bool HasAccessorySamples(AccessoryDataSO[] sampleAccessories)
    {
        if (sampleAccessories == null)
        {
            return false;
        }

        for (int i = 0; i < sampleAccessories.Length; i++)
        {
            if (sampleAccessories[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    private static AccessoryDataSO FindAccessoryByRarity(AccessoryDataSO[] sampleAccessories, int rarity)
    {
        if (sampleAccessories == null || rarity < 0 || rarity >= sampleAccessories.Length)
        {
            return null;
        }

        return sampleAccessories[rarity];
    }

    private void DisposeSpawnedObjects()
    {
        for (int i = 0; i < spawnedDisposables.Count; i++)
        {
            spawnedDisposables[i]?.Dispose();
        }

        spawnedDisposables.Clear();
        spawnedObjects.Clear();
    }

    private void ClearGeneratedChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }
}
