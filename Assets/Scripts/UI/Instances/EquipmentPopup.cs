using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class EquipmentPopup : PopupBase
{
    private const string DEFAULT_WEAPON_TAB_NAME = "WeaponTab";
    private const string DEFAULT_ACCESSORY_TAB_NAME = "AccessoryTab";
    private const string DEFAULT_CLOSE_BUTTON_NAME = "CloseButton";
    private const string DEFAULT_SELL_BUTTON_NAME = "SellButton";
    private const string DEFAULT_MERGE_BUTTON_NAME = "MergeButton";

    [Header("子视图")]
    [SerializeField] private EquipmentInventoryListView inventoryListView;
    [SerializeField] private EquipmentSelectedDisplayView selectedDisplayView;
    [SerializeField] private EquipmentInfoDocumentView infoDocumentView;

    [Header("Tabs")]
    [SerializeField] private Button weaponTabButton;
    [SerializeField] private Button accessoryTabButton;

    [Header("操作")]
    [SerializeField] private Button sellButton;
    [SerializeField] private TextMeshProUGUI sellText;
    [SerializeField] private Button mergeButton;
    [SerializeField] private TextMeshProUGUI mergeText;
    [SerializeField] private Button closeButton;

    private readonly List<EquipmentEntry> currentEntries = new();
    private readonly List<EquipmentListItemViewData> listItemBuffer = new();
    private readonly ItemInfoViewDataBuilder itemInfoViewDataBuilder = new();
    private EquipmentPopupContext popupContext;
    private EquipmentEntry? selectedEntry;
    private bool showWeapons = true;
    private bool eventsBound;
    private bool dataBound;

    protected override void OnCreate()
    {
        base.OnCreate();
        ResolveViewParts();
        ValidateConfiguration();
        ApplyTabVisuals();
        ClearItems();
        ClearSelectionVisuals();
    }

    protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
    {
        EquipmentPopupContext nextContext = context.GetPayload<EquipmentPopupContext>()
            ?? throw new InvalidOperationException($"{nameof(EquipmentPopup)} requires {nameof(EquipmentPopupContext)} payload.");

        UnbindFromContext();
        popupContext = nextContext;
        BindToContext(nextContext);
        return UniTask.CompletedTask;
    }

    protected override void OnClosed(CloseReason reason)
    {
        UnbindFromContext();
        popupContext = null;
    }

    private void OnDestroy()
    {
        UnbindFromContext();
    }

    private void BindToContext(EquipmentPopupContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        context.WeaponsHolder.OnWeaponsChanged += OnSourceChanged;
        context.AccessoryManager.OnAccessoryEquipped += OnAccessoryChanged;
        context.AccessoryManager.OnAccessoryUnequipped += OnAccessoryChanged;
        dataBound = true;

        BindUiEvents();
        RefreshFromSource();
    }

    private void UnbindFromContext()
    {
        if (dataBound && popupContext != null)
        {
            popupContext.WeaponsHolder.OnWeaponsChanged -= OnSourceChanged;
            popupContext.AccessoryManager.OnAccessoryEquipped -= OnAccessoryChanged;
            popupContext.AccessoryManager.OnAccessoryUnequipped -= OnAccessoryChanged;
        }

        dataBound = false;

        UnbindUiEvents();
        selectedEntry = null;
        currentEntries.Clear();
        ClearItems();
        ClearSelectionVisuals();
    }

    private void BindUiEvents()
    {
        if (eventsBound)
        {
            return;
        }

        inventoryListView.ItemClicked += OnItemClicked;
        weaponTabButton.onClick.AddListener(OnWeaponTabClicked);
        accessoryTabButton.onClick.AddListener(OnAccessoryTabClicked);
        sellButton.onClick.AddListener(OnSellClicked);
        mergeButton.onClick.AddListener(OnMergeClicked);
        closeButton.onClick.AddListener(OnCloseClicked);
        eventsBound = true;
    }

    private void UnbindUiEvents()
    {
        if (!eventsBound)
        {
            return;
        }

        if (inventoryListView != null)
        {
            inventoryListView.ItemClicked -= OnItemClicked;
        }

        if (weaponTabButton != null)
        {
            weaponTabButton.onClick.RemoveListener(OnWeaponTabClicked);
        }

        if (accessoryTabButton != null)
        {
            accessoryTabButton.onClick.RemoveListener(OnAccessoryTabClicked);
        }

        if (sellButton != null)
        {
            sellButton.onClick.RemoveListener(OnSellClicked);
        }

        if (mergeButton != null)
        {
            mergeButton.onClick.RemoveListener(OnMergeClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(OnCloseClicked);
        }

        eventsBound = false;
    }

    private void OnSourceChanged()
    {
        RefreshFromSource();
    }

    private void OnAccessoryChanged(AccessoryDataSO _)
    {
        RefreshFromSource();
    }

    private void RefreshFromSource()
    {
        if (popupContext == null)
        {
            return;
        }

        BuildEntries();
        RenderItems();
        EnsureSelection();
    }

    private void BuildEntries()
    {
        currentEntries.Clear();

        WeaponsHolder weaponsHolder = popupContext.WeaponsHolder;
        if (weaponsHolder != null)
        {
            IReadOnlyList<Weapon> equippedWeapons = weaponsHolder.EquippedWeapons;
            for (int i = 0; i < equippedWeapons.Count; i++)
            {
                Weapon equippedWeapon = equippedWeapons[i];
                if (equippedWeapon == null || equippedWeapon.WeaponData == null)
                {
                    continue;
                }

                currentEntries.Add(EquipmentEntry.CreateWeapon(
                    BuildWeaponEntryId(equippedWeapon),
                    equippedWeapon));
            }
        }

        AccessoryManager accessoryManager = popupContext.AccessoryManager;
        if (accessoryManager != null)
        {
            IReadOnlyList<Accessory> equippedAccessories = accessoryManager.EquippedAccessoryList;
            for (int i = 0; i < equippedAccessories.Count; i++)
            {
                Accessory accessory = equippedAccessories[i];
                if (accessory.Data == null || string.IsNullOrWhiteSpace(accessory.RuntimeId))
                {
                    continue;
                }

                currentEntries.Add(EquipmentEntry.CreateAccessory(accessory));
            }
        }
    }

    private void RenderItems()
    {
        listItemBuffer.Clear();
        for (int i = 0; i < currentEntries.Count; i++)
        {
            EquipmentEntry entry = currentEntries[i];
            if (!ShouldShow(entry) || entry.ItemData == null)
            {
                continue;
            }

            listItemBuffer.Add(new EquipmentListItemViewData(
                entry.EntryId,
                entry.ItemData,
                entry));
        }

        inventoryListView.Render(listItemBuffer);
    }

    private void ClearItems()
    {
        listItemBuffer.Clear();
        inventoryListView.Clear();
    }

    private void OnItemClicked(string entryId)
    {
        if (!TryFindEntry(entryId, out EquipmentEntry entry))
        {
            return;
        }

        selectedEntry = entry;
        ApplySelection();
    }

    private void EnsureSelection()
    {
        if (selectedEntry.HasValue && ContainsEntry(selectedEntry.Value.EntryId) && ShouldShow(selectedEntry.Value))
        {
            ApplySelection();
            return;
        }

        selectedEntry = default;
        for (int i = 0; i < currentEntries.Count; i++)
        {
            if (!ShouldShow(currentEntries[i]))
            {
                continue;
            }

            selectedEntry = currentEntries[i];
            ApplySelection();
            return;
        }

        ClearSelectionVisuals();
    }

    private void ApplySelection()
    {
        if (!selectedEntry.HasValue)
        {
            ClearSelectionVisuals();
            return;
        }

        EquipmentEntry entry = selectedEntry.Value;
        ItemDataSO itemData = entry.ItemData;
        if (itemData == null)
        {
            ClearSelectionVisuals();
            return;
        }

        selectedDisplayView.Render(new EquipmentSelectedDisplayData(
            itemData.ItemIcon,
            ResolveSelectedDisplayName(entry)));

        infoDocumentView.Render(BuildItemInfoViewData(entry));

        RefreshActionState();
    }

    private void ClearSelectionVisuals()
    {
        selectedDisplayView.Clear();
        infoDocumentView.Clear();
        RefreshActionState();
    }

    private void RefreshActionState()
    {
        if (!selectedEntry.HasValue || selectedEntry.Value.ItemData == null)
        {
            SetActionsState(false, false, "出售", "合并");
            return;
        }

        EquipmentEntry entry = selectedEntry.Value;
        int sellPrice = GetSellPrice(entry);
        bool canMerge = entry.IsWeapon && entry.RuntimeWeapon != null && WeaponLevelHelper.CanMerge(entry.RuntimeWeapon.Level) && HasMergeTarget(entry);
        SetActionsState(true, canMerge, $"出售 {sellPrice}", "合并");
    }

    private void SetActionsState(bool sellInteractable, bool mergeInteractable, string sellLabel, string mergeLabel)
    {
        sellButton.interactable = sellInteractable;
        mergeButton.interactable = mergeInteractable;

        if (sellText != null)
        {
            sellText.text = sellLabel;
        }

        if (mergeText != null)
        {
            mergeText.text = mergeLabel;
        }
    }

    private void OnSellClicked()
    {
        if (!selectedEntry.HasValue || popupContext == null)
        {
            return;
        }

        EquipmentEntry entry = selectedEntry.Value;
        if (entry.ItemData == null)
        {
            return;
        }

        if (entry.IsWeapon)
        {
            if (popupContext.WeaponsHolder == null || entry.RuntimeWeapon == null)
            {
                return;
            }

            if (!popupContext.WeaponsHolder.RemoveWeapon(entry.RuntimeWeapon))
            {
                return;
            }
        }
        else
        {
            if (popupContext.AccessoryManager == null || !popupContext.AccessoryManager.UnequipAccessoryByRuntimeId(entry.EntryId))
            {
                return;
            }
        }

        popupContext.CurrencyWallet.ChangeAmount(GetSellPrice(entry));
        AudioSfxBridge.RequestPlay(AudioSfxKey.ItemSold);
    }

    private void OnMergeClicked()
    {
        if (!selectedEntry.HasValue || popupContext == null)
        {
            return;
        }

        EquipmentEntry entry = selectedEntry.Value;
        if (!entry.IsWeapon || entry.RuntimeWeapon == null || popupContext.WeaponsHolder == null)
        {
            return;
        }

        if (!WeaponLevelHelper.CanMerge(entry.RuntimeWeapon.Level))
        {
            return;
        }

        if (!TryFindMergeTarget(entry, out EquipmentEntry target))
        {
            return;
        }

        if (popupContext.WeaponsHolder.MergeWeapon(entry.RuntimeWeapon, target.RuntimeWeapon))
        {
            AudioSfxBridge.RequestPlay(AudioSfxKey.WeaponMerged);
        }
    }

    private void OnWeaponTabClicked()
    {
        showWeapons = true;
        ApplyTabVisuals();
        RenderItems();
        EnsureSelection();
    }

    private void OnAccessoryTabClicked()
    {
        showWeapons = false;
        ApplyTabVisuals();
        RenderItems();
        EnsureSelection();
    }

    private void OnCloseClicked()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.UiCancel);
        Handle.CloseAsync(CloseReason.Normal, this.GetCancellationTokenOnDestroy()).Forget();
    }

    private void ApplyTabVisuals()
    {
        SetTabSelected(weaponTabButton, showWeapons);
        SetTabSelected(accessoryTabButton, !showWeapons);
    }

    private static void SetTabSelected(Button button, bool selected)
    {
        if (button == null)
        {
            return;
        }

        Color selectedColor = new Color(0.9607844f, 0.14509805f, 0.47058827f, 1f);
        Color normalColor = new Color(0.42f, 0.42f, 0.46f, 0.9f);
        Graphic graphic = button.targetGraphic != null ? button.targetGraphic : button.GetComponent<Graphic>();
        if (graphic != null)
        {
            graphic.color = selected ? selectedColor : normalColor;
        }

        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            label.color = selected ? selectedColor : new Color(0.68f, 0.68f, 0.72f, 1f);
        }

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = selected ? Color.white : new Color(0.96f, 0.96f, 0.96f, 1f);
        colors.pressedColor = selected ? Color.white : new Color(0.9f, 0.9f, 0.9f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(1f, 1f, 1f, 0.5f);
        button.colors = colors;
    }

    private bool ShouldShow(EquipmentEntry entry)
    {
        return entry.IsWeapon ? showWeapons : !showWeapons;
    }

    private bool ContainsEntry(string entryId)
    {
        return TryFindEntry(entryId, out _);
    }

    private bool TryFindEntry(string entryId, out EquipmentEntry entry)
    {
        for (int i = 0; i < currentEntries.Count; i++)
        {
            if (string.Equals(currentEntries[i].EntryId, entryId, StringComparison.Ordinal))
            {
                entry = currentEntries[i];
                return true;
            }
        }

        entry = default;
        return false;
    }

    private bool TryFindMergeTarget(EquipmentEntry selected, out EquipmentEntry target)
    {
        for (int i = 0; i < currentEntries.Count; i++)
        {
            EquipmentEntry candidate = currentEntries[i];
            if (!candidate.IsWeapon || candidate.RuntimeWeapon == null)
            {
                continue;
            }

            if (string.Equals(candidate.EntryId, selected.EntryId, StringComparison.Ordinal))
            {
                continue;
            }

            if (!ReferenceEquals(candidate.ItemData, selected.ItemData))
            {
                continue;
            }

            if (candidate.RuntimeWeapon == null || selected.RuntimeWeapon == null || candidate.RuntimeWeapon.Level != selected.RuntimeWeapon.Level)
            {
                continue;
            }

            target = candidate;
            return true;
        }

        target = default;
        return false;
    }

    private bool HasMergeTarget(EquipmentEntry selected)
    {
        return TryFindMergeTarget(selected, out _);
    }

    private static string BuildWeaponEntryId(Weapon runtimeWeapon)
    {
        return runtimeWeapon == null ? null : $"WPN_{runtimeWeapon.GetInstanceID()}";
    }

    private ItemInfoViewData BuildItemInfoViewData(EquipmentEntry entry)
    {
        if (entry.RuntimeWeapon != null)
        {
            return itemInfoViewDataBuilder.Build(entry.RuntimeWeapon);
        }

        if (entry.RuntimeAccessory != null && entry.RuntimeAccessory.Data != null)
        {
            return itemInfoViewDataBuilder.Build(entry.RuntimeAccessory.Data);
        }

        return itemInfoViewDataBuilder.Build(entry.ItemData);
    }

    private static string ResolveSelectedDisplayName(EquipmentEntry entry)
    {
        if (entry.RuntimeWeapon != null && entry.RuntimeWeapon.WeaponData != null)
        {
            return ItemNameStyleUtility.GetWeaponDisplayName(
                entry.RuntimeWeapon.WeaponData.ItemName,
                entry.RuntimeWeapon.Tier);
        }

        if (entry.RuntimeAccessory != null && entry.RuntimeAccessory.Data != null)
        {
            return ItemNameStyleUtility.GetAccessoryDisplayName(
                entry.RuntimeAccessory.Data.ItemName,
                entry.RuntimeAccessory.Tier);
        }

        return entry.ItemData != null ? entry.ItemData.ItemName ?? string.Empty : string.Empty;
    }

    private static int GetSellPrice(EquipmentEntry entry)
    {
        ItemDataSO itemData = entry.ItemData;
        return itemData switch
        {
            WeaponDataSO weaponData => WeaponPriceHelper.GetPrice(weaponData.ItemPrice, entry.RuntimeWeapon != null ? entry.RuntimeWeapon.Level : WeaponLevelHelper.MinLevel),
            AccessoryDataSO accessoryData => accessoryData.RecyclePrice,
            _ => itemData != null ? itemData.ItemPrice : 0
        };
    }

    private void ResolveViewParts()
    {
        if (inventoryListView == null)
        {
            Transform viewRoot = FindDeepChild("AllEquipmentContainer");
            inventoryListView = viewRoot != null
                ? viewRoot.GetComponent<EquipmentInventoryListView>()
                : GetComponentInChildren<EquipmentInventoryListView>(true);
        }

        if (selectedDisplayView == null)
        {
            Transform viewRoot = FindDeepChild("SelectedItemStage");
            selectedDisplayView = viewRoot != null
                ? viewRoot.GetComponent<EquipmentSelectedDisplayView>()
                : GetComponentInChildren<EquipmentSelectedDisplayView>(true);
        }

        if (infoDocumentView == null)
        {
            Transform viewRoot = FindDeepChild("InfoPanel");
            infoDocumentView = viewRoot != null
                ? viewRoot.GetComponent<EquipmentInfoDocumentView>()
                : GetComponentInChildren<EquipmentInfoDocumentView>(true);
        }

        if (weaponTabButton == null)
        {
            weaponTabButton = FindButton(DEFAULT_WEAPON_TAB_NAME);
        }

        if (accessoryTabButton == null)
        {
            accessoryTabButton = FindButton(DEFAULT_ACCESSORY_TAB_NAME);
        }

        if (closeButton == null)
        {
            closeButton = FindButton(DEFAULT_CLOSE_BUTTON_NAME);
        }

        if (sellButton == null)
        {
            sellButton = FindButton(DEFAULT_SELL_BUTTON_NAME);
        }

        if (sellText == null)
        {
            Transform label = FindDeepChild($"ActionButtons/{DEFAULT_SELL_BUTTON_NAME}/Label");
            sellText = label != null ? label.GetComponent<TextMeshProUGUI>() : null;
        }

        if (mergeButton == null)
        {
            mergeButton = FindButton(DEFAULT_MERGE_BUTTON_NAME);
        }

        if (mergeText == null)
        {
            Transform label = FindDeepChild($"ActionButtons/{DEFAULT_MERGE_BUTTON_NAME}/Label");
            mergeText = label != null ? label.GetComponent<TextMeshProUGUI>() : null;
        }
    }

    private Transform FindDeepChild(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        if (path.StartsWith("VisualRoot/", StringComparison.Ordinal))
        {
            Transform visualRoot = transform.Find("VisualRoot");
            return visualRoot != null ? visualRoot.Find(path.Substring("VisualRoot/".Length)) : null;
        }

        return transform.Find("VisualRoot/" + path) ?? transform.Find(path);
    }

    private Button FindButton(string name)
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button != null && string.Equals(button.name, name, StringComparison.Ordinal))
            {
                return button;
            }
        }

        return null;
    }

    private void ValidateConfiguration()
    {
        if (inventoryListView == null)
        {
            throw new MissingReferenceException($"{nameof(EquipmentPopup)} '{name}' is missing inventory list view.");
        }

        if (selectedDisplayView == null)
        {
            throw new MissingReferenceException($"{nameof(EquipmentPopup)} '{name}' is missing selected display view.");
        }

        if (infoDocumentView == null)
        {
            throw new MissingReferenceException($"{nameof(EquipmentPopup)} '{name}' is missing info document view.");
        }

        if (weaponTabButton == null)
        {
            throw new MissingReferenceException($"{nameof(EquipmentPopup)} '{name}' is missing weapon tab button.");
        }

        if (accessoryTabButton == null)
        {
            throw new MissingReferenceException($"{nameof(EquipmentPopup)} '{name}' is missing accessory tab button.");
        }

        if (sellButton == null)
        {
            throw new MissingReferenceException($"{nameof(EquipmentPopup)} '{name}' is missing sell button.");
        }

        if (mergeButton == null)
        {
            throw new MissingReferenceException($"{nameof(EquipmentPopup)} '{name}' is missing merge button.");
        }

        if (closeButton == null)
        {
            throw new MissingReferenceException($"{nameof(EquipmentPopup)} '{name}' is missing close button.");
        }
    }

    private readonly struct EquipmentEntry : IHasContentTier
    {
        public EquipmentEntry(string entryId, ItemDataSO itemData, Weapon runtimeWeapon, Accessory runtimeAccessory)
        {
            EntryId = entryId;
            ItemData = itemData;
            RuntimeWeapon = runtimeWeapon;
            RuntimeAccessory = runtimeAccessory;
        }

        public string EntryId { get; }
        public ItemDataSO ItemData { get; }
        public Weapon RuntimeWeapon { get; }
        public Accessory RuntimeAccessory { get; }
        public bool IsWeapon => ItemData != null && ItemData.ItemType == ItemType.Weapon;
        public ContentTier Tier => RuntimeWeapon != null
            ? RuntimeWeapon.Tier
            : RuntimeAccessory != null && RuntimeAccessory.Data != null ? RuntimeAccessory.Tier : ContentTier.Common;

        public static EquipmentEntry CreateWeapon(string entryId, Weapon runtimeWeapon)
        {
            return new EquipmentEntry(entryId, runtimeWeapon != null ? runtimeWeapon.WeaponData : null, runtimeWeapon, default);
        }

        public static EquipmentEntry CreateAccessory(Accessory runtimeAccessory)
        {
            return new EquipmentEntry(runtimeAccessory.RuntimeId, runtimeAccessory.Data, null, runtimeAccessory);
        }
    }
}
