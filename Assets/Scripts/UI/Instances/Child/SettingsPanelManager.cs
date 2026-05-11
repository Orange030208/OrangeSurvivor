using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Orange.UIFramework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SettingsPanelManager : ViewPartBase
{
    [Header("显示")]
    [SerializeField] private MonoBehaviour motionSource;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("平台配置")]
    [SerializeField] private PlatformSettingsProfileSO[] platformProfiles = Array.Empty<PlatformSettingsProfileSO>();
    [SerializeField] private Selectable defaultSelectable;

    [Header("设置分区")]
    [SerializeField] private GameObject audioSection;
    [SerializeField] private GameObject displaySection;
    [SerializeField] private GameObject languageSection;
    [SerializeField] private GameObject inputSection;
    [SerializeField] private GameObject touchSection;

    [Header("音量")]
    [SerializeField] private SettingsSliderRow masterVolume;
    [SerializeField] private SettingsSliderRow sfxVolume;
    [SerializeField] private SettingsSliderRow musicVolume;

    [Header("显示设置")]
    [SerializeField] private SettingsOptionRow resolutionRow;
    [SerializeField] private SettingsOptionRow windowModeRow;

    [Header("语言")]
    [SerializeField] private SettingsOptionRow languageRow;

    [Header("输入")]
    [SerializeField] private SettingsRebindRow[] rebindRows = Array.Empty<SettingsRebindRow>();
    [SerializeField] private Button resetBindingsButton;

    [Header("操作")]
    [SerializeField] private Button saveButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private bool applyPreviewImmediately = true;

    private readonly List<DisplayResolutionOption> resolutionOptions = new();
    private readonly List<SettingsRebindRow> activeRebindRows = new();

    private GameSettingsState savedState;
    private GameSettingsState editingState;
    private PlatformSettingsProfileSO activeProfile;
    private IUIRuntimeMotion motion;
    private UIManager uiManager;
    private InputRebindOperation activeRebind;
    private bool visible = true;
    private bool controlsBound;
    private bool displayConfirmationPending;

    private void Awake()
    {
        ResolvePresentationReferences();
        ResolveActiveProfile();
        ValidateConfiguration();
        BindControls();
        ResolveResolutionOptions();
        LoadSavedState();
        ApplyProfileToSections();
        ApplyEditingStateToView();
        GameSettingsService.ApplyAudio(editingState);
        motion?.RefreshDefaults();
        SetHiddenImmediate();
    }

    private void OnDestroy()
    {
        activeRebind?.Cancel();
        activeRebind?.Dispose();
        UnbindControls();
        motion?.Kill();
    }

    private void OnEnable()
    {
        ResolvePresentationReferences();
        ResolveActiveProfile();
        ResolveResolutionOptions();

        if (controlsBound)
        {
            LoadSavedState();
            ApplyProfileToSections();
            ApplyEditingStateToView();
            GameSettingsService.ApplyAudio(editingState);
        }
    }

    private void OnDisable()
    {
        motion?.Kill();
    }

    public bool IsVisible => visible;

    public void ConfigureOwner(UIManager ownerUIManager)
    {
        uiManager = ownerUIManager;
    }

    public Tween SetVisible(bool value)
    {
        ResolvePresentationReferences();
        if (visible == value)
        {
            SetInteractionEnabled(value);
            return null;
        }

        visible = value;
        SetInteractionEnabled(value);
        SelectDefaultControlIfVisible(value);
        return motion?.Play(value ? UIMotionClipIds.SHOW : UIMotionClipIds.HIDE);
    }

    public void SetVisibleImmediate(bool value)
    {
        ResolvePresentationReferences();
        visible = value;
        motion?.SetImmediate(value ? UIMotionClipIds.SHOW : UIMotionClipIds.HIDE);
        SetInteractionEnabled(value);
        SelectDefaultControlIfVisible(value);
    }

    public void SetHiddenImmediate()
    {
        SetVisibleImmediate(false);
    }

    public override async UniTask HideAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!visible)
        {
            SetInteractionEnabled(false);
            return;
        }

        Tween tween = SetVisible(false);
        await tween.WaitForCompletionAsync(cancellationToken);
    }

    public void Save()
    {
        GameSettingsState previousState = savedState.Clone();
        GameSettingsState targetState = editingState.Clone();
        GameInputService inputService = GameInputService.Instance;
        targetState.InputRebindsJson = inputService != null
            ? inputService.SaveBindingOverrides()
            : targetState.InputRebindsJson;
        targetState.Sanitize();

        savedState = targetState.Clone();
        GameSettingsService.Save(savedState);
        GameSettingsService.Apply(savedState, applyDisplay: false, applyInput: true);

        bool displayChanged = !previousState.ToDisplaySnapshot().Equals(targetState.ToDisplaySnapshot());
        if (displayChanged)
        {
            RequestDisplayConfirmationAsync(previousState, targetState).Forget();
        }

        RefreshRebindRows();
        AudioSfxBridge.RequestPlay(AudioSfxKey.UiConfirm);
    }

    public void ResetToDefaults()
    {
        activeRebind?.Cancel();
        activeRebind?.Dispose();
        activeRebind = null;

        editingState = GameSettingsState.Default();
        savedState = editingState.Clone();
        GameInputService inputService = GameInputService.Instance;
        inputService?.ClearBindingOverrides();
        editingState.InputRebindsJson = string.Empty;
        savedState.InputRebindsJson = string.Empty;
        GameSettingsService.Save(savedState);
        GameSettingsService.Apply(savedState, applyDisplay: true, applyInput: true);
        ApplyEditingStateToView();
        AudioSfxBridge.RequestPlay(AudioSfxKey.UiConfirm);
    }

    private void BindControls()
    {
        if (controlsBound)
        {
            return;
        }

        masterVolume.Initialize("总音量", OnMasterVolumeChanged);
        sfxVolume.Initialize("音效", OnSfxVolumeChanged);
        musicVolume.Initialize("音乐", OnMusicVolumeChanged);
        resolutionRow.Initialize("分辨率", OffsetResolution);
        windowModeRow.Initialize("窗口模式", OffsetWindowMode);
        languageRow.Initialize("语言", OffsetLanguage);

        for (int i = 0; i < rebindRows.Length; i++)
        {
            rebindRows[i]?.Initialize(OnRebindClicked);
        }

        resetBindingsButton.onClick.RemoveListener(OnResetBindingsClicked);
        resetBindingsButton.onClick.AddListener(OnResetBindingsClicked);
        saveButton.onClick.RemoveListener(Save);
        resetButton.onClick.RemoveListener(ResetToDefaults);
        saveButton.onClick.AddListener(Save);
        resetButton.onClick.AddListener(ResetToDefaults);
        controlsBound = true;
    }

    private void UnbindControls()
    {
        if (!controlsBound)
        {
            return;
        }

        if (resetBindingsButton != null)
        {
            resetBindingsButton.onClick.RemoveListener(OnResetBindingsClicked);
        }

        if (saveButton != null)
        {
            saveButton.onClick.RemoveListener(Save);
        }

        if (resetButton != null)
        {
            resetButton.onClick.RemoveListener(ResetToDefaults);
        }

        controlsBound = false;
    }

    private void LoadSavedState()
    {
        savedState = GameSettingsService.Load();
        GameInputService inputService = GameInputService.Instance;
        inputService?.LoadBindingOverrides(savedState.InputRebindsJson);
        editingState = savedState.Clone();
        ClampEditingStateToProfile();
    }

    private void ResolveActiveProfile()
    {
        activeProfile = PlatformSettingsProfileSO.SelectProfile(platformProfiles, Application.platform);
    }

    private void ResolveResolutionOptions()
    {
        resolutionOptions.Clear();
        resolutionOptions.AddRange(DisplaySettingsService.GetAvailableResolutions());
    }

    private void OnMasterVolumeChanged(float value)
    {
        editingState.MasterVolume = value;
        ApplyPreviewIfNeeded();
    }

    private void OnSfxVolumeChanged(float value)
    {
        editingState.SfxVolume = value;
        ApplyPreviewIfNeeded();
    }

    private void OnMusicVolumeChanged(float value)
    {
        editingState.MusicVolume = value;
        ApplyPreviewIfNeeded();
    }

    private void OffsetResolution(int offset)
    {
        if (!IsFeatureEnabled(SettingsFeature.DisplayResolution))
        {
            return;
        }

        if (resolutionOptions.Count == 0)
        {
            ResolveResolutionOptions();
        }

        int index = FindResolutionIndex(editingState.ResolutionWidth, editingState.ResolutionHeight);
        int nextIndex = WrapIndex(index + offset, resolutionOptions.Count);
        DisplayResolutionOption option = resolutionOptions[nextIndex];
        editingState.ResolutionWidth = option.Width;
        editingState.ResolutionHeight = option.Height;
        RefreshSettingsView();
        AudioSfxBridge.RequestPlay(AudioSfxKey.UiConfirm);
    }

    private void OffsetWindowMode(int offset)
    {
        if (!IsFeatureEnabled(SettingsFeature.WindowMode))
        {
            return;
        }

        int index = activeProfile.IndexOfWindowMode(editingState.WindowMode);
        editingState.WindowMode = activeProfile.GetWindowModeAt(WrapIndex(index + offset, activeProfile.GetWindowModeCount()));
        RefreshSettingsView();
        AudioSfxBridge.RequestPlay(AudioSfxKey.UiConfirm);
    }

    private void OffsetLanguage(int offset)
    {
        if (!IsFeatureEnabled(SettingsFeature.Language))
        {
            return;
        }

        int index = activeProfile.IndexOfLanguage(editingState.LanguageCode);
        editingState.LanguageCode = activeProfile.GetLanguageAt(WrapIndex(index + offset, activeProfile.GetLanguageCount()));
        RefreshSettingsView();
        AudioSfxBridge.RequestPlay(AudioSfxKey.UiConfirm);
    }

    private void OnResetBindingsClicked()
    {
        activeRebind?.Cancel();
        activeRebind?.Dispose();
        activeRebind = null;

        GameInputService inputService = GameInputService.Instance;
        inputService?.ClearBindingOverrides();
        editingState.InputRebindsJson = string.Empty;
        RefreshRebindRows();
        AudioSfxBridge.RequestPlay(AudioSfxKey.UiConfirm);
    }

    private void OnRebindClicked(SettingsRebindRow row)
    {
        if (row == null || !IsRebindRowAllowed(row))
        {
            return;
        }

        activeRebind?.Cancel();
        activeRebind?.Dispose();

        row.SetValue("按下按键...");
        row.SetInteractable(false);

        GameInputService inputService = GameInputService.Instance;
        InputRebindService.RebindEntry entry = row.Entry;
        activeRebind = InputRebindService.StartInteractiveRebind(
            inputService,
            entry,
            (result, message) =>
            {
                activeRebind?.Dispose();
                activeRebind = null;
                row.SetInteractable(true);

                if (result == InputRebindResult.Success)
                {
                    editingState.InputRebindsJson = inputService != null
                        ? inputService.SaveBindingOverrides()
                        : string.Empty;
                    RefreshRebindRows();
                    AudioSfxBridge.RequestPlay(AudioSfxKey.UiConfirm);
                    return;
                }

                row.SetValue(result == InputRebindResult.Conflict ? "冲突" : "取消");
                DOVirtual.DelayedCall(0.6f, RefreshRebindRows).SetUpdate(true);
                AudioSfxBridge.RequestPlay(AudioSfxKey.UiCancel);
            });
    }

    private void SetInteractionEnabled(bool enabled)
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.interactable = enabled;
        canvasGroup.blocksRaycasts = enabled;
    }

    private void SelectDefaultControlIfVisible(bool enabled)
    {
        if (!enabled)
        {
            return;
        }

        Selectable selectable = ResolveDefaultSelectable();
        if (selectable != null && selectable.gameObject.activeInHierarchy && selectable.IsInteractable())
        {
            EventSystem.current?.SetSelectedGameObject(selectable.gameObject);
        }
    }

    private Selectable ResolveDefaultSelectable()
    {
        if (defaultSelectable != null && defaultSelectable.gameObject.activeInHierarchy && defaultSelectable.IsInteractable())
        {
            return defaultSelectable;
        }

        if (audioSection != null && audioSection.activeInHierarchy)
        {
            return masterVolume.DefaultSelectable;
        }

        if (displaySection != null && displaySection.activeInHierarchy)
        {
            if (resolutionRow.gameObject.activeInHierarchy)
            {
                return resolutionRow.DefaultSelectable;
            }

            if (windowModeRow.gameObject.activeInHierarchy)
            {
                return windowModeRow.DefaultSelectable;
            }
        }

        if (languageSection != null && languageSection.activeInHierarchy)
        {
            return languageRow.DefaultSelectable;
        }

        for (int i = 0; i < activeRebindRows.Count; i++)
        {
            Selectable rowSelectable = activeRebindRows[i].DefaultSelectable;
            if (rowSelectable != null && rowSelectable.gameObject.activeInHierarchy)
            {
                return rowSelectable;
            }
        }

        return resetBindingsButton;
    }

    private void ApplyPreviewIfNeeded()
    {
        if (!applyPreviewImmediately)
        {
            return;
        }

        GameSettingsService.ApplyAudio(editingState);
    }

    private void ApplyEditingStateToView()
    {
        masterVolume.SetValue(editingState.MasterVolume);
        sfxVolume.SetValue(editingState.SfxVolume);
        musicVolume.SetValue(editingState.MusicVolume);
        RefreshSettingsView();
    }

    private void RefreshSettingsView()
    {
        if (editingState == null)
        {
            return;
        }

        resolutionRow.SetValue(editingState.ToDisplaySnapshot().ResolutionLabel);
        windowModeRow.SetValue(DisplaySettingsService.GetWindowModeLabel(editingState.WindowMode));
        languageRow.SetValue(GetLanguageLabel(editingState.LanguageCode));
        RefreshRebindRows();
    }

    private void RefreshRebindRows()
    {
        GameInputService inputService = GameInputService.Instance;
        activeRebindRows.Clear();

        for (int i = 0; i < rebindRows.Length; i++)
        {
            SettingsRebindRow row = rebindRows[i];
            if (row == null)
            {
                continue;
            }

            bool allowed = IsRebindRowAllowed(row);
            row.gameObject.SetActive(allowed);
            row.SetInteractable(allowed);

            if (allowed)
            {
                activeRebindRows.Add(row);
                row.SetValue(InputRebindService.GetDisplayString(inputService, row.Entry));
            }
        }

        resetBindingsButton.gameObject.SetActive(activeRebindRows.Count > 0);
        resetBindingsButton.interactable = activeRebindRows.Count > 0;
    }

    private void ApplyProfileToSections()
    {
        bool showAudio = IsFeatureEnabled(SettingsFeature.Audio);
        bool showResolution = IsFeatureEnabled(SettingsFeature.DisplayResolution);
        bool showWindowMode = IsFeatureEnabled(SettingsFeature.WindowMode);
        bool showDisplay = showResolution || showWindowMode;
        bool showLanguage = IsFeatureEnabled(SettingsFeature.Language);
        bool showInput = IsFeatureEnabled(SettingsFeature.KeyboardRebind) || IsFeatureEnabled(SettingsFeature.GamepadRebind);
        bool showTouch = IsFeatureEnabled(SettingsFeature.TouchControls);

        SetActive(audioSection, showAudio);
        SetActive(displaySection, showDisplay);
        SetActive(languageSection, showLanguage);
        SetActive(inputSection, showInput);
        SetActive(touchSection, showTouch);

        masterVolume.gameObject.SetActive(showAudio);
        sfxVolume.gameObject.SetActive(showAudio);
        musicVolume.gameObject.SetActive(showAudio);
        resolutionRow.gameObject.SetActive(showResolution);
        windowModeRow.gameObject.SetActive(showWindowMode);
        languageRow.gameObject.SetActive(showLanguage);
        resetBindingsButton.gameObject.SetActive(showInput);
        RefreshRebindRows();
    }

    private void ClampEditingStateToProfile()
    {
        if (editingState == null || activeProfile == null)
        {
            return;
        }

        if (IsFeatureEnabled(SettingsFeature.WindowMode))
        {
            editingState.WindowMode = activeProfile.GetWindowModeAt(activeProfile.IndexOfWindowMode(editingState.WindowMode));
        }

        if (IsFeatureEnabled(SettingsFeature.Language))
        {
            editingState.LanguageCode = activeProfile.GetLanguageAt(activeProfile.IndexOfLanguage(editingState.LanguageCode));
        }
    }

    private async UniTask RequestDisplayConfirmationAsync(GameSettingsState previousState, GameSettingsState targetState)
    {
        DisplaySettingsSnapshot previousDisplay = previousState.ToDisplaySnapshot();
        DisplaySettingsSnapshot targetDisplay = targetState.ToDisplaySnapshot();
        DisplaySettingsService.Apply(targetDisplay);

        if (!activeProfile.RequireDisplayConfirmation)
        {
            return;
        }

        if (displayConfirmationPending)
        {
            return;
        }

        displayConfirmationPending = true;
        try
        {
            UIManager manager = ResolveUIManager();
            DisplayConfirmModalContext context = new(previousDisplay, targetDisplay);
            ModalResult<bool> result = await manager.ShowModalAsync<DisplayConfirmModal, bool>(
                context,
                this.GetCancellationTokenOnDestroy());
            if (result.Confirmed && result.Value)
            {
                return;
            }

            RevertDisplaySettings(previousDisplay);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            Debug.LogError($"{nameof(SettingsPanelManager)} '{name}' failed to show display confirmation modal. Reverting display settings.\n{exception}", this);
            RevertDisplaySettings(previousDisplay);
        }
        finally
        {
            displayConfirmationPending = false;
        }
    }

    private void RevertDisplaySettings(DisplaySettingsSnapshot previousDisplay)
    {
        editingState.SetDisplaySnapshot(previousDisplay);
        savedState.SetDisplaySnapshot(previousDisplay);
        GameSettingsService.Save(savedState);
        DisplaySettingsService.Apply(previousDisplay);
        RefreshSettingsView();
    }

    private UIManager ResolveUIManager()
    {
        if (uiManager != null)
        {
            return uiManager;
        }

        if (UIManager.Instance != null)
        {
            return UIManager.Instance;
        }

        throw new MissingReferenceException($"{nameof(SettingsPanelManager)} '{name}' cannot show {nameof(DisplayConfirmModal)} without an owning {nameof(UIManager)}.");
    }

    private bool IsFeatureEnabled(SettingsFeature feature)
    {
        return activeProfile != null && activeProfile.IsEnabled(feature);
    }

    private bool IsRebindRowAllowed(SettingsRebindRow row)
    {
        if (row == null)
        {
            return false;
        }

        if (string.Equals(row.ControlScheme, "Gamepad", StringComparison.OrdinalIgnoreCase))
        {
            return IsFeatureEnabled(SettingsFeature.GamepadRebind);
        }

        return IsFeatureEnabled(SettingsFeature.KeyboardRebind);
    }

    private int FindResolutionIndex(int width, int height)
    {
        for (int i = 0; i < resolutionOptions.Count; i++)
        {
            if (resolutionOptions[i].Width == width && resolutionOptions[i].Height == height)
            {
                return i;
            }
        }

        return 0;
    }

    private static int WrapIndex(int index, int count)
    {
        if (count <= 0)
        {
            return 0;
        }

        int wrapped = index % count;
        return wrapped < 0 ? wrapped + count : wrapped;
    }

    private static string GetLanguageLabel(string languageCode)
    {
        return GameSettingsService.NormalizeLanguageCode(languageCode) == GameSettingsService.ENGLISH_LANGUAGE_CODE
            ? "English"
            : "简体中文";
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }

    private void ValidateConfiguration()
    {
        if (motionSource == null)
        {
            throw new MissingReferenceException($"{nameof(SettingsPanelManager)} '{name}' is missing motion source.");
        }

        if (motion == null)
        {
            throw new MissingComponentException($"{nameof(SettingsPanelManager)} '{name}' motion source must implement {nameof(IUIRuntimeMotion)}.");
        }

        if (canvasGroup == null)
        {
            throw new MissingReferenceException($"{nameof(SettingsPanelManager)} '{name}' is missing canvas group.");
        }

        if (activeProfile == null)
        {
            throw new MissingReferenceException($"{nameof(SettingsPanelManager)} '{name}' is missing platform settings profile.");
        }

        ValidateSection(audioSection, nameof(audioSection));
        ValidateSection(displaySection, nameof(displaySection));
        ValidateSection(languageSection, nameof(languageSection));
        ValidateSection(inputSection, nameof(inputSection));
        ValidateSection(touchSection, nameof(touchSection));
        ValidateObject(masterVolume, nameof(masterVolume));
        ValidateObject(sfxVolume, nameof(sfxVolume));
        ValidateObject(musicVolume, nameof(musicVolume));
        ValidateObject(resolutionRow, nameof(resolutionRow));
        ValidateObject(windowModeRow, nameof(windowModeRow));
        ValidateObject(languageRow, nameof(languageRow));

        masterVolume.Validate();
        sfxVolume.Validate();
        musicVolume.Validate();
        resolutionRow.Validate();
        windowModeRow.Validate();
        languageRow.Validate();

        if (rebindRows == null || rebindRows.Length == 0)
        {
            throw new MissingReferenceException($"{nameof(SettingsPanelManager)} '{name}' is missing rebind rows.");
        }

        for (int i = 0; i < rebindRows.Length; i++)
        {
            if (rebindRows[i] == null)
            {
                throw new MissingReferenceException($"{nameof(SettingsPanelManager)} '{name}' rebind row at index {i} is missing.");
            }

            rebindRows[i].Validate();
        }

        if (resetBindingsButton == null)
        {
            throw new MissingReferenceException($"{nameof(SettingsPanelManager)} '{name}' is missing reset bindings button.");
        }

        if (saveButton == null)
        {
            throw new MissingReferenceException($"{nameof(SettingsPanelManager)} '{name}' is missing save button.");
        }

        if (resetButton == null)
        {
            throw new MissingReferenceException($"{nameof(SettingsPanelManager)} '{name}' is missing reset button.");
        }
    }

    private static void ValidateSection(GameObject section, string fieldName)
    {
        if (section == null)
        {
            throw new MissingReferenceException($"{nameof(SettingsPanelManager)} is missing section '{fieldName}'.");
        }
    }

    private void ValidateObject(UnityEngine.Object value, string fieldName)
    {
        if (value == null)
        {
            throw new MissingReferenceException($"{nameof(SettingsPanelManager)} '{name}' is missing '{fieldName}'.");
        }
    }

    private void ResolvePresentationReferences()
    {
        if (canvasGroup == null)
        {
            TryGetComponent(out canvasGroup);
        }

        if (motionSource != null)
        {
            motion = ResolveRuntimeMotion(motionSource);
            return;
        }

        MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null || ReferenceEquals(behaviour, this))
            {
                continue;
            }

            IUIRuntimeMotion resolvedMotion = ResolveRuntimeMotion(behaviour);
            if (resolvedMotion == null)
            {
                continue;
            }

            motionSource = behaviour;
            motion = resolvedMotion;
            return;
        }
    }

    private IUIRuntimeMotion ResolveRuntimeMotion(MonoBehaviour source)
    {
        if (source == null)
        {
            return null;
        }

        if (source is IUIRuntimeMotion directMotion)
        {
            return directMotion;
        }

        MonoBehaviour[] behaviours = source.GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IUIRuntimeMotion siblingMotion)
            {
                return siblingMotion;
            }
        }

        return null;
    }
}
