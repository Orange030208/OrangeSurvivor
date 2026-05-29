using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 暂停菜单页面本体：负责按钮绑定、属性同步以及暂停栏内容面板的切换动画。
/// 页面本身不直接管理暂停恢复流程，只发意图事件，关闭时先等待内容面板收起。
/// </summary>
public class GamePauseMenu : PageBase
{
    private const string PROPERTIES_POPUP_GROUP_ID = "properties";
    private const string EQUIPMENT_POPUP_GROUP_ID = "equipment";

    [Header("暂停栏按钮")]
    [SerializeField] private Button statusButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button menuButton;
    [SerializeField] private Button equipmentButton;

    private GamePauseMenuContext currentContext;
    private bool buttonEventsBound;
    private ViewHandle<SettingsPanelManager> settingsPanelHandle;
    private ViewHandle<PropertiesPopup> propertiesPopupHandle;
    private ViewHandle<EquipmentPopup> equipmentPopupHandle;

    protected override void Awake()
    {
        base.Awake();
        ValidateConfiguration();
    }

    protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
    {
        currentContext = context.GetPayload<GamePauseMenuContext>()
            ?? throw new InvalidOperationException($"{nameof(GamePauseMenu)} requires {nameof(GamePauseMenuContext)} payload.");
        BindButtonEvents();
        SelectDefaultControl();
        return UniTask.CompletedTask;
    }

    protected override async UniTask OnClosingAsync(CloseReason reason, CancellationToken cancellationToken)
    {
        await CloseEquipmentPopupAsync(CloseReason.Cancel, cancellationToken);
        await ClosePropertiesPopupAsync(CloseReason.Cancel, cancellationToken);
        await CloseSettingsPanelAsync(CloseReason.Cancel, cancellationToken);
    }

    protected override void OnClosed(CloseReason reason)
    {
        UnbindButtonEvents();
        settingsPanelHandle = default;
        propertiesPopupHandle = default;
        equipmentPopupHandle = default;
        currentContext = null;
    }

    private void BindButtonEvents()
    {
        if (buttonEventsBound)
        {
            return;
        }

        continueButton.onClick.AddListener(OnContinueClicked);
        restartButton.onClick.AddListener(OnRestartClicked);
        menuButton.onClick.AddListener(OnMenuClicked);
        if (statusButton != null)
        {
            statusButton.onClick.AddListener(OnStatusClicked);
        }

        settingsButton.onClick.AddListener(OnSettingsClicked);
        equipmentButton.onClick.AddListener(OnEquipmentClicked);
        buttonEventsBound = true;
    }

    private void UnbindButtonEvents()
    {
        if (!buttonEventsBound)
        {
            return;
        }

        continueButton.onClick.RemoveListener(OnContinueClicked);
        restartButton.onClick.RemoveListener(OnRestartClicked);
        menuButton.onClick.RemoveListener(OnMenuClicked);
        if (statusButton != null)
        {
            statusButton.onClick.RemoveListener(OnStatusClicked);
        }

        settingsButton.onClick.RemoveListener(OnSettingsClicked);
        equipmentButton.onClick.RemoveListener(OnEquipmentClicked);
        buttonEventsBound = false;
    }

    private void OnContinueClicked()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.UiConfirm);
        GameEventBus.Publish<PauseMenuContinueClickedEvent>();
    }

    private void OnRestartClicked()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.UiCancel);
        Debug.Log($"{nameof(GamePauseMenu)} restart entry is visible but the restart flow is not connected yet.", this);
    }

    private void OnMenuClicked()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.UiCancel);
        GameEventBus.Publish<PauseMenuReturnToMenuClickedEvent>();
    }

    private void OnStatusClicked()
    {
        TogglePropertiesPopupAsync().Forget();
    }

    private void OnSettingsClicked()
    {
        ToggleSettingsPanelAsync().Forget();
    }

    private void OnEquipmentClicked()
    {
        ToggleEquipmentPopupAsync().Forget();
    }

    private async UniTask ToggleSettingsPanelAsync()
    {
        if (IsSettingsPanelOpen())
        {
            AudioSfxBridge.RequestPlay(AudioSfxKey.UiCancel);
            await CloseSettingsPanelAsync(CloseReason.Normal, this.GetCancellationTokenOnDestroy());
            return;
        }

        AudioSfxBridge.RequestPlay(AudioSfxKey.UiConfirm);
        settingsPanelHandle = await OwnerUIManager.ShowPopupAsync<SettingsPanelManager>(
            new SettingsPanelManager.Context(OwnerUIManager),
            CreateSettingsPopupOptions(),
            this.GetCancellationTokenOnDestroy());
        ClearSettingsHandleWhenClosedAsync(settingsPanelHandle).Forget();
    }

    private async UniTask TogglePropertiesPopupAsync()
    {
        if (IsPropertiesPopupOpen())
        {
            AudioSfxBridge.RequestPlay(AudioSfxKey.UiCancel);
            await ClosePropertiesPopupAsync(CloseReason.Normal, this.GetCancellationTokenOnDestroy());
            return;
        }

        if (currentContext == null)
        {
            Debug.LogWarning($"{nameof(GamePauseMenu)} cannot open properties popup without a pause menu context.", this);
            return;
        }

        AudioSfxBridge.RequestPlay(AudioSfxKey.UiConfirm);
        propertiesPopupHandle = await OwnerUIManager.ShowPopupAsync<PropertiesPopup>(
            currentContext.PropertiesManager,
            CreatePropertiesPopupOptions(),
            this.GetCancellationTokenOnDestroy());
        ClearPropertiesHandleWhenClosedAsync(propertiesPopupHandle).Forget();
    }

    private async UniTask ToggleEquipmentPopupAsync()
    {
        if (IsEquipmentPopupOpen())
        {
            AudioSfxBridge.RequestPlay(AudioSfxKey.UiCancel);
            await CloseEquipmentPopupAsync(CloseReason.Normal, this.GetCancellationTokenOnDestroy());
            return;
        }

        if (currentContext == null || !TryCreateEquipmentContext(currentContext.Player, out EquipmentPopupContext equipmentContext))
        {
            Debug.LogWarning($"{nameof(GamePauseMenu)} cannot open equipment popup without player equipment dependencies.", this);
            return;
        }

        AudioSfxBridge.RequestPlay(AudioSfxKey.UiConfirm);
        equipmentPopupHandle = await OwnerUIManager.ShowPopupAsync<EquipmentPopup>(
            equipmentContext,
            CreateEquipmentPopupOptions(),
            this.GetCancellationTokenOnDestroy());
        ClearEquipmentHandleWhenClosedAsync(equipmentPopupHandle).Forget();
    }

    private async UniTask CloseSettingsPanelAsync(CloseReason reason, CancellationToken cancellationToken)
    {
        if (!IsSettingsPanelOpen())
        {
            settingsPanelHandle = default;
            return;
        }

        ViewHandle<SettingsPanelManager> handle = settingsPanelHandle;
        settingsPanelHandle = default;
        await handle.CloseAsync(reason, cancellationToken);
    }

    private async UniTask ClosePropertiesPopupAsync(CloseReason reason, CancellationToken cancellationToken)
    {
        if (!IsPropertiesPopupOpen())
        {
            propertiesPopupHandle = default;
            return;
        }

        ViewHandle<PropertiesPopup> handle = propertiesPopupHandle;
        propertiesPopupHandle = default;
        await handle.CloseAsync(reason, cancellationToken);
    }

    private async UniTask CloseEquipmentPopupAsync(CloseReason reason, CancellationToken cancellationToken)
    {
        if (!IsEquipmentPopupOpen())
        {
            equipmentPopupHandle = default;
            return;
        }

        ViewHandle<EquipmentPopup> handle = equipmentPopupHandle;
        equipmentPopupHandle = default;
        await handle.CloseAsync(reason, cancellationToken);
    }

    private bool IsSettingsPanelOpen()
    {
        return settingsPanelHandle.IsValid && settingsPanelHandle.View != null && settingsPanelHandle.View.IsOpen;
    }

    private bool IsPropertiesPopupOpen()
    {
        return propertiesPopupHandle.IsValid && propertiesPopupHandle.View != null && propertiesPopupHandle.View.IsOpen;
    }

    private bool IsEquipmentPopupOpen()
    {
        return equipmentPopupHandle.IsValid && equipmentPopupHandle.View != null && equipmentPopupHandle.View.IsOpen;
    }

    private async UniTaskVoid ClearSettingsHandleWhenClosedAsync(ViewHandle<SettingsPanelManager> handle)
    {
        await handle.ClosedTask;
        if (settingsPanelHandle.IsValid && settingsPanelHandle.InstanceId == handle.InstanceId)
        {
            settingsPanelHandle = default;
        }
    }

    private async UniTaskVoid ClearPropertiesHandleWhenClosedAsync(ViewHandle<PropertiesPopup> handle)
    {
        await handle.ClosedTask;
        if (propertiesPopupHandle.IsValid && propertiesPopupHandle.InstanceId == handle.InstanceId)
        {
            propertiesPopupHandle = default;
        }
    }

    private async UniTaskVoid ClearEquipmentHandleWhenClosedAsync(ViewHandle<EquipmentPopup> handle)
    {
        await handle.ClosedTask;
        if (equipmentPopupHandle.IsValid && equipmentPopupHandle.InstanceId == handle.InstanceId)
        {
            equipmentPopupHandle = default;
        }
    }

    private static PopupOptions CreateSettingsPopupOptions()
    {
        return new PopupOptions(
            closeOnOutsideClick: false,
            showBackdrop: true,
            groupId: "settings",
            replaceSameGroup: true,
            trackInStack: true);
    }

    private static PopupOptions CreatePropertiesPopupOptions()
    {
        return new PopupOptions(
            closeOnOutsideClick: false,
            showBackdrop: true,
            groupId: PROPERTIES_POPUP_GROUP_ID,
            replaceSameGroup: true,
            trackInStack: true);
    }

    private static PopupOptions CreateEquipmentPopupOptions()
    {
        return new PopupOptions(
            closeOnOutsideClick: true,
            showBackdrop: true,
            groupId: EQUIPMENT_POPUP_GROUP_ID,
            replaceSameGroup: true,
            trackInStack: true,
            preferredAnchor: FloatingViewAnchor.Center);
    }

    private static bool TryCreateEquipmentContext(Player player, out EquipmentPopupContext equipmentContext)
    {
        equipmentContext = null;
        if (player == null)
        {
            return false;
        }

        WeaponsHolder weaponsHolder = player.GetComponent<WeaponsHolder>();
        AccessoryManager accessoryManager = player.GetComponent<AccessoryManager>();
        CurrencyWallet currencyWallet = player.GetComponent<CurrencyWallet>();
        if (weaponsHolder == null || accessoryManager == null || currencyWallet == null)
        {
            return false;
        }

        equipmentContext = new EquipmentPopupContext(weaponsHolder, accessoryManager, currencyWallet);
        return true;
    }

    private void SelectDefaultControl()
    {
        if (continueButton == null)
        {
            return;
        }

        EventSystem.current?.SetSelectedGameObject(continueButton.gameObject);
    }

    private void ValidateConfiguration()
    {
        if (continueButton == null)
        {
            throw new MissingReferenceException($"{nameof(GamePauseMenu)} '{name}' is missing continue button.");
        }

        if (restartButton == null)
        {
            throw new MissingReferenceException($"{nameof(GamePauseMenu)} '{name}' is missing restart button.");
        }

        if (menuButton == null)
        {
            throw new MissingReferenceException($"{nameof(GamePauseMenu)} '{name}' is missing menu button.");
        }

        if (settingsButton == null)
        {
            throw new MissingReferenceException($"{nameof(GamePauseMenu)} '{name}' is missing settings button.");
        }

        if (statusButton == null)
        {
            throw new MissingReferenceException($"{nameof(GamePauseMenu)} '{name}' is missing status button.");
        }

        if (equipmentButton == null)
        {
            throw new MissingReferenceException($"{nameof(GamePauseMenu)} '{name}' is missing equipment button.");
        }
    }
}
