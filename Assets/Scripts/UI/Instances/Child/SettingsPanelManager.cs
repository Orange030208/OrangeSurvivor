using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Orange.Input;
using Orange.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SettingsPanelManager : PopupBase
{
    private enum SettingsCategory
    {
        Audio,
        Display,
        Control,
        Gameplay,
        Language
    }

    public readonly struct Context
    {
        public Context(UIManager ownerUIManager)
        {
            OwnerUIManager = ownerUIManager;
        }

        public UIManager OwnerUIManager { get; }
    }

    [Header("显示")]
    [SerializeField] private MonoBehaviour motionSource;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("平台配置")]
    [SerializeField] private PlatformSettingsProfileSO[] platformProfiles = Array.Empty<PlatformSettingsProfileSO>();
    [SerializeField] private Selectable defaultSelectable;

    [Header("导航")]
    [SerializeField] private TextMeshProUGUI sectionTitle;
    [SerializeField] private Sprite tabDefaultSprite;
    [SerializeField] private Sprite tabSelectedSprite;
    [SerializeField] private Color tabDefaultContentColor = new(0.13f, 0.68f, 1f, 1f);
    [SerializeField] private Color tabSelectedContentColor = new(1f, 0.22f, 0.68f, 1f);
    [SerializeField] private Button audioTabButton;
    [SerializeField] private Button displayTabButton;
    [SerializeField] private Button controlTabButton;
    [SerializeField] private Button gameplayTabButton;
    [SerializeField] private Button languageTabButton;
    [SerializeField] private Button closeButton;

    [Header("设置分区")]
    [SerializeField] private GameObject audioSection;
    [SerializeField] private GameObject displaySection;
    [SerializeField] private GameObject languageSection;
    [SerializeField] private GameObject inputSection;
    [SerializeField] private GameObject gameplaySection;
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
    private SettingsCategory currentCategory = SettingsCategory.Audio;
    private bool visible = true;
    private bool controlsBound;
    private bool displayConfirmationPending;

    protected override void Awake()
    {
        base.Awake();
        ResolvePresentationReferences();
        ResolveActiveProfile();
        ValidateConfiguration();
        ResolveResolutionOptions();
        LoadSavedState();
        ApplyProfileToSections();
        ApplyEditingStateToView();
        ApplyAudioPreview();
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
            ApplyAudioPreview();
        }
    }

    private void OnDisable()
    {
        motion?.Kill();
    }

    public bool IsVisible => visible;

    protected override async UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
    {
        BindContext(context.Payload);
        BindControls();
        LoadSavedState();
        ApplyProfileToSections();
        ApplyEditingStateToView();
        ApplyAudioPreview();

        Tween tween = SetVisible(true);
        await tween.WaitForCompletionAsync(cancellationToken);
    }

    protected override UniTask OnClosingAsync(CloseReason reason, CancellationToken cancellationToken)
    {
        activeRebind?.Cancel();
        activeRebind?.Dispose();
        activeRebind = null;
        return HideAsync(cancellationToken);
    }

    protected override void OnClosed(CloseReason reason)
    {
        UnbindControls();
        ConfigureOwner(null);
        SetHiddenImmediate();
    }

    private void BindContext(object context)
    {
        if (context == null)
        {
            ConfigureOwner(OwnerUIManager);
            return;
        }

        if (context is not Context panelContext)
        {
            throw new ArgumentException($"{nameof(SettingsPanelManager)} '{name}' expects {nameof(Context)}.", nameof(context));
        }

        ConfigureOwner(panelContext.OwnerUIManager);
    }

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

    public async UniTask HideAsync(CancellationToken cancellationToken)
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
        GameInput input = GameInput.Instance;
        targetState.InputRebindsJson = input != null
            ? input.SaveBindingOverrides()
            : targetState.InputRebindsJson;
        targetState.Sanitize();

        savedState = targetState.Clone();
        GameSettingsService.Save(savedState);
        input?.SaveBindingOverridesToStore();
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
        GameInput input = GameInput.Instance;
        input?.ClearBindingOverrides();
        input?.ClearBindingOverrideStore();
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

        masterVolume.Bind(new SettingsSliderRow.Context("总音量", OnMasterVolumeChanged));
        sfxVolume.Bind(new SettingsSliderRow.Context("音效", OnSfxVolumeChanged));
        musicVolume.Bind(new SettingsSliderRow.Context("音乐", OnMusicVolumeChanged));
        resolutionRow.Bind(new SettingsOptionRow.Context("分辨率", OffsetResolution));
        windowModeRow.Bind(new SettingsOptionRow.Context("窗口模式", OffsetWindowMode));
        languageRow.Bind(new SettingsOptionRow.Context("语言", OffsetLanguage));

        for (int i = 0; i < rebindRows.Length; i++)
        {
            rebindRows[i]?.Bind(new SettingsRebindRow.Context(OnRebindClicked));
        }

        resetBindingsButton.onClick.RemoveListener(OnResetBindingsClicked);
        resetBindingsButton.onClick.AddListener(OnResetBindingsClicked);
        AddTabListener(audioTabButton, SelectAudioCategory);
        AddTabListener(displayTabButton, SelectDisplayCategory);
        AddTabListener(controlTabButton, SelectControlCategory);
        AddTabListener(gameplayTabButton, SelectGameplayCategory);
        AddTabListener(languageTabButton, SelectLanguageCategory);
        AddButtonListener(closeButton, OnCloseClicked);
        resetButton.onClick.RemoveListener(ResetToDefaults);
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

        if (resetButton != null)
        {
            resetButton.onClick.RemoveListener(ResetToDefaults);
        }

        RemoveTabListener(audioTabButton, SelectAudioCategory);
        RemoveTabListener(displayTabButton, SelectDisplayCategory);
        RemoveTabListener(controlTabButton, SelectControlCategory);
        RemoveTabListener(gameplayTabButton, SelectGameplayCategory);
        RemoveTabListener(languageTabButton, SelectLanguageCategory);
        RemoveButtonListener(closeButton, OnCloseClicked);

        masterVolume?.Unbind();
        sfxVolume?.Unbind();
        musicVolume?.Unbind();
        resolutionRow?.Unbind();
        windowModeRow?.Unbind();
        languageRow?.Unbind();

        if (rebindRows != null)
        {
            for (int i = 0; i < rebindRows.Length; i++)
            {
                rebindRows[i]?.Unbind();
            }
        }

        controlsBound = false;
    }

    private void LoadSavedState()
    {
        savedState = GameSettingsService.Load();
        GameInput input = GameInput.Instance;
        input?.LoadBindingOverrides(savedState.InputRebindsJson);
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
        SaveAndApplyAudio();
    }

    private void OnSfxVolumeChanged(float value)
    {
        editingState.SfxVolume = value;
        SaveAndApplyAudio();
    }

    private void OnMusicVolumeChanged(float value)
    {
        editingState.MusicVolume = value;
        SaveAndApplyAudio();
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

        if (resolutionOptions.Count == 0)
        {
            return;
        }

        int index = FindResolutionIndex(editingState.ResolutionWidth, editingState.ResolutionHeight);
        int nextIndex = WrapIndex(index + offset, resolutionOptions.Count);
        DisplayResolutionOption option = resolutionOptions[nextIndex];
        editingState.ResolutionWidth = option.Width;
        editingState.ResolutionHeight = option.Height;
        RefreshSettingsView();
        SaveAndApplyDisplay(previousState: savedState.Clone());
        AudioSfxBridge.RequestPlay(AudioSfxKey.UiConfirm);
    }

    private void OffsetWindowMode(int offset)
    {
        if (!IsFeatureEnabled(SettingsFeature.WindowMode))
        {
            return;
        }

        int index = activeProfile.IndexOfWindowMode(editingState.WindowMode);
        GameSettingsState previousState = savedState.Clone();
        editingState.WindowMode = activeProfile.GetWindowModeAt(WrapIndex(index + offset, activeProfile.GetWindowModeCount()));
        RefreshSettingsView();
        SaveAndApplyDisplay(previousState);
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
        SaveAndApplyLanguage();
        AudioSfxBridge.RequestPlay(AudioSfxKey.UiConfirm);
    }

    private void OnResetBindingsClicked()
    {
        activeRebind?.Cancel();
        activeRebind?.Dispose();
        activeRebind = null;

        GameInput input = GameInput.Instance;
        input?.ClearBindingOverrides();
        input?.ClearBindingOverrideStore();
        editingState.InputRebindsJson = string.Empty;
        savedState.InputRebindsJson = string.Empty;
        GameSettingsService.Save(savedState);
        GameSettingsService.Apply(savedState, applyDisplay: false, applyInput: true);
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

        GameInput input = GameInput.Instance;
        InputRebindEntry entry = row.Entry;
        activeRebind = InputRebindService.StartInteractiveRebind(
            input,
            entry,
            (result, message) =>
            {
                activeRebind?.Dispose();
                activeRebind = null;
                row.SetInteractable(true);

                if (result == InputRebindResult.Success)
                {
                    editingState.InputRebindsJson = input != null
                        ? input.SaveBindingOverrides()
                        : string.Empty;
                    savedState.InputRebindsJson = editingState.InputRebindsJson;
                    GameSettingsService.Save(savedState);
                    input?.SaveBindingOverridesToStore();
                    GameSettingsService.Apply(savedState, applyDisplay: false, applyInput: true);
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

    private void SaveAndApplyAudio()
    {
        editingState.Sanitize();
        savedState.MasterVolume = editingState.MasterVolume;
        savedState.SfxVolume = editingState.SfxVolume;
        savedState.MusicVolume = editingState.MusicVolume;
        GameSettingsService.Save(savedState);
        ApplyAudioPreview();
    }

    private void ApplyAudioPreview()
    {
        if (!applyPreviewImmediately)
        {
            return;
        }

        GameSettingsService.ApplyAudio(editingState);
    }

    private void SaveAndApplyDisplay(GameSettingsState previousState)
    {
        editingState.Sanitize();
        savedState.SetDisplaySnapshot(editingState.ToDisplaySnapshot());
        GameSettingsService.Save(savedState);

        bool displayChanged = !previousState.ToDisplaySnapshot().Equals(savedState.ToDisplaySnapshot());
        if (displayChanged)
        {
            RequestDisplayConfirmationAsync(previousState, savedState.Clone()).Forget();
        }
    }

    private void SaveAndApplyLanguage()
    {
        editingState.Sanitize();
        savedState.LanguageCode = editingState.LanguageCode;
        GameSettingsService.Save(savedState);
        GameSettingsService.ApplyLanguage(savedState.LanguageCode).Forget();
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
        GameInput input = GameInput.Instance;
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
                row.SetValue(InputRebindService.GetDisplayString(input, row.Entry));
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

        SetTabAvailable(audioTabButton, showAudio);
        SetTabAvailable(displayTabButton, showDisplay);
        SetTabAvailable(controlTabButton, showInput);
        SetTabAvailable(gameplayTabButton, true);
        SetTabAvailable(languageTabButton, showLanguage);
        masterVolume.gameObject.SetActive(showAudio);
        sfxVolume.gameObject.SetActive(showAudio);
        musicVolume.gameObject.SetActive(showAudio);
        resolutionRow.gameObject.SetActive(showResolution);
        windowModeRow.gameObject.SetActive(showWindowMode);
        languageRow.gameObject.SetActive(showLanguage);
        resetBindingsButton.gameObject.SetActive(showInput);
        RefreshRebindRows();
        SelectCategory(ResolveInitialCategory());
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

    private void SelectAudioCategory()
    {
        SelectCategory(SettingsCategory.Audio);
    }

    private void SelectDisplayCategory()
    {
        SelectCategory(SettingsCategory.Display);
    }

    private void SelectControlCategory()
    {
        SelectCategory(SettingsCategory.Control);
    }

    private void SelectGameplayCategory()
    {
        SelectCategory(SettingsCategory.Gameplay);
    }

    private void SelectLanguageCategory()
    {
        SelectCategory(SettingsCategory.Language);
    }

    private void SelectCategory(SettingsCategory category)
    {
        if (!IsCategoryAvailable(category))
        {
            category = ResolveInitialCategory();
        }

        currentCategory = category;
        SetActive(audioSection, category == SettingsCategory.Audio && IsCategoryAvailable(SettingsCategory.Audio));
        SetActive(displaySection, category == SettingsCategory.Display && IsCategoryAvailable(SettingsCategory.Display));
        SetActive(inputSection, category == SettingsCategory.Control && IsCategoryAvailable(SettingsCategory.Control));
        SetActive(gameplaySection, category == SettingsCategory.Gameplay && IsCategoryAvailable(SettingsCategory.Gameplay));
        SetActive(touchSection, category == SettingsCategory.Gameplay && IsCategoryAvailable(SettingsCategory.Gameplay));
        SetActive(languageSection, category == SettingsCategory.Language && IsCategoryAvailable(SettingsCategory.Language));
        RefreshTabSelection();
        RefreshSectionTitle();
        SelectDefaultControlIfVisible(visible);
    }

    private SettingsCategory ResolveInitialCategory()
    {
        if (IsCategoryAvailable(currentCategory))
        {
            return currentCategory;
        }

        if (IsCategoryAvailable(SettingsCategory.Audio))
        {
            return SettingsCategory.Audio;
        }

        if (IsCategoryAvailable(SettingsCategory.Display))
        {
            return SettingsCategory.Display;
        }

        if (IsCategoryAvailable(SettingsCategory.Control))
        {
            return SettingsCategory.Control;
        }

        if (IsCategoryAvailable(SettingsCategory.Gameplay))
        {
            return SettingsCategory.Gameplay;
        }

        return SettingsCategory.Language;
    }

    private bool IsCategoryAvailable(SettingsCategory category)
    {
        return category switch
        {
            SettingsCategory.Audio => IsFeatureEnabled(SettingsFeature.Audio),
            SettingsCategory.Display => IsFeatureEnabled(SettingsFeature.DisplayResolution) || IsFeatureEnabled(SettingsFeature.WindowMode),
            SettingsCategory.Control => IsFeatureEnabled(SettingsFeature.KeyboardRebind) || IsFeatureEnabled(SettingsFeature.GamepadRebind),
            SettingsCategory.Gameplay => true,
            SettingsCategory.Language => IsFeatureEnabled(SettingsFeature.Language),
            _ => false
        };
    }

    private void RefreshSectionTitle()
    {
        if (sectionTitle == null)
        {
            return;
        }

        sectionTitle.text = currentCategory switch
        {
            SettingsCategory.Audio => "音频设置",
            SettingsCategory.Display => "画面设置",
            SettingsCategory.Control => "控制设置",
            SettingsCategory.Gameplay => "游戏设置",
            SettingsCategory.Language => "语言设置",
            _ => "设置"
        };
    }

    private void RefreshTabSelection()
    {
        SetTabSelected(audioTabButton, currentCategory == SettingsCategory.Audio);
        SetTabSelected(displayTabButton, currentCategory == SettingsCategory.Display);
        SetTabSelected(controlTabButton, currentCategory == SettingsCategory.Control);
        SetTabSelected(gameplayTabButton, currentCategory == SettingsCategory.Gameplay);
        SetTabSelected(languageTabButton, currentCategory == SettingsCategory.Language);
    }

    private static void SetTabAvailable(Button button, bool available)
    {
        if (button == null)
        {
            return;
        }

        button.gameObject.SetActive(available);
        button.interactable = available;
    }

    private void SetTabSelected(Button button, bool selected)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.targetGraphic as Image;
        if (image != null)
        {
            Sprite sprite = selected ? tabSelectedSprite : tabDefaultSprite;
            if (sprite != null)
            {
                image.sprite = sprite;
                image.color = Color.white;
            }
            else
            {
                image.color = selected
                    ? new Color(1f, 0.17f, 0.68f, 0.92f)
                    : new Color(0.03f, 0.06f, 0.16f, 0.82f);
            }
        }

        ApplyTabContentColor(button, selected ? tabSelectedContentColor : tabDefaultContentColor);
    }

    private static void ApplyTabContentColor(Button button, Color color)
    {
        TextMeshProUGUI[] texts = button.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            texts[i].color = color;
        }

        Image[] images = button.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            if (button.targetGraphic != null && ReferenceEquals(images[i], button.targetGraphic))
            {
                continue;
            }

            images[i].color = color;
        }
    }

    private void OnCloseClicked()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.UiCancel);
        Handle.CloseAsync(CloseReason.Normal, this.GetCancellationTokenOnDestroy()).Forget();
    }

    private static void AddTabListener(Button button, UnityEngine.Events.UnityAction action)
    {
        AddButtonListener(button, action);
    }

    private static void RemoveTabListener(Button button, UnityEngine.Events.UnityAction action)
    {
        RemoveButtonListener(button, action);
    }

    private static void AddButtonListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private static void RemoveButtonListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
        {
            button.onClick.RemoveListener(action);
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
        ValidateSection(gameplaySection, nameof(gameplaySection));
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
