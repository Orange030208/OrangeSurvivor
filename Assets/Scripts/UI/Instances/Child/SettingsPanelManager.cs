using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SettingsPanelManager : MonoBehaviour, IPointerClickHandler
{
    private const string MASTER_VOLUME_KEY = "Settings.MasterVolume";
    private const string SFX_VOLUME_KEY = "Settings.SfxVolume";
    private const string MUSIC_VOLUME_KEY = "Settings.MusicVolume";
    private const string USE_MASTER_VOLUME_KEY = "Settings.UseMasterVolume";
    private const int DEFAULT_SELECTED_STEP = 10;

    [Header("音量")]
    [SerializeField] private VolumeStepControl masterVolume;
    [SerializeField] private VolumeStepControl sfxVolume;
    [SerializeField] private VolumeStepControl musicVolume;

    [Header("总音量开关")]
    [SerializeField] private UIClickTarget useMasterVolumeYesButton;
    [SerializeField] private Image useMasterVolumeYesMark;
    [SerializeField] private UIClickTarget useMasterVolumeNoButton;
    [SerializeField] private Image useMasterVolumeNoMark;
    [SerializeField] private Sprite defaultToggleSprite;
    [SerializeField] private Sprite selectedToggleSprite;

    [Header("操作")]
    [SerializeField] private UIClickTarget saveButton;
    [SerializeField] private UIClickTarget resetButton;
    [SerializeField] private bool applyPreviewImmediately = true;

    private SettingsPanelState savedState;
    private SettingsPanelState editingState;
    private bool clickTargetsBound;

    private void Awake()
    {
        ValidateConfiguration();
        BindClickTargets();
        masterVolume.Initialize(OnMasterVolumeChanged);
        sfxVolume.Initialize(OnSfxVolumeChanged);
        musicVolume.Initialize(OnMusicVolumeChanged);
        LoadSavedState();
        ApplyEditingStateToView();
        ApplyAudioSettings(editingState);
    }

    private void OnDestroy()
    {
        UnbindClickTargets();
    }

    private void OnEnable()
    {
        LoadSavedState();
        ApplyEditingStateToView();
        ApplyAudioSettings(editingState);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        HandleVolumePointer(eventData);
    }

    public void Save()
    {
        savedState = editingState;
        PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, savedState.MasterVolume);
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, savedState.SfxVolume);
        PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, savedState.MusicVolume);
        PlayerPrefs.SetInt(USE_MASTER_VOLUME_KEY, savedState.UseMasterVolume ? 1 : 0);
        PlayerPrefs.Save();
        ApplyAudioSettings(savedState);
        AudioSfxBridge.RequestPlay(AudioSfxKey.WoodenButtonClicked);
    }

    public void ResetToDefaults()
    {
        editingState = SettingsPanelState.Default;
        savedState = editingState;
        ApplyEditingStateToView();
        ApplyAudioSettings(editingState);

        PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, savedState.MasterVolume);
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, savedState.SfxVolume);
        PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, savedState.MusicVolume);
        PlayerPrefs.SetInt(USE_MASTER_VOLUME_KEY, savedState.UseMasterVolume ? 1 : 0);
        PlayerPrefs.Save();
        AudioSfxBridge.RequestPlay(AudioSfxKey.WoodenButtonClicked);
    }

    private void BindClickTargets()
    {
        if (clickTargetsBound)
        {
            return;
        }

        useMasterVolumeYesButton.OnClicked += OnUseMasterVolumeYesClicked;
        useMasterVolumeNoButton.OnClicked += OnUseMasterVolumeNoClicked;
        saveButton.OnClicked += Save;
        resetButton.OnClicked += ResetToDefaults;
        clickTargetsBound = true;
    }

    private void UnbindClickTargets()
    {
        if (!clickTargetsBound)
        {
            return;
        }

        if (useMasterVolumeYesButton != null)
        {
            useMasterVolumeYesButton.OnClicked -= OnUseMasterVolumeYesClicked;
        }

        if (useMasterVolumeNoButton != null)
        {
            useMasterVolumeNoButton.OnClicked -= OnUseMasterVolumeNoClicked;
        }

        if (saveButton != null)
        {
            saveButton.OnClicked -= Save;
        }

        if (resetButton != null)
        {
            resetButton.OnClicked -= ResetToDefaults;
        }

        clickTargetsBound = false;
    }

    private void LoadSavedState()
    {
        savedState = new SettingsPanelState(
            PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, AudioConstants.DEFAULT_VOLUME),
            PlayerPrefs.GetFloat(SFX_VOLUME_KEY, AudioConstants.DEFAULT_VOLUME),
            PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, AudioConstants.DEFAULT_VOLUME),
            PlayerPrefs.GetInt(USE_MASTER_VOLUME_KEY, 1) == 1);

        editingState = savedState;
    }

    private void HandleVolumePointer(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        GameObject raycastTarget = eventData.pointerCurrentRaycast.gameObject;
        if (masterVolume.TrySetValueFromRaycast(raycastTarget))
        {
            return;
        }

        if (sfxVolume.TrySetValueFromRaycast(raycastTarget))
        {
            return;
        }

        musicVolume.TrySetValueFromRaycast(raycastTarget);
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

    private void OnUseMasterVolumeYesClicked()
    {
        SetUseMasterVolume(true);
    }

    private void OnUseMasterVolumeNoClicked()
    {
        SetUseMasterVolume(false);
    }

    private void SetUseMasterVolume(bool value)
    {
        if (editingState.UseMasterVolume == value)
        {
            return;
        }

        editingState.UseMasterVolume = value;
        RefreshUseMasterVolumeToggle();
        ApplyPreviewIfNeeded();
        AudioSfxBridge.RequestPlay(AudioSfxKey.WoodenButtonClicked);
    }

    private void ApplyPreviewIfNeeded()
    {
        if (!applyPreviewImmediately)
        {
            return;
        }

        ApplyAudioSettings(editingState);
    }

    private void ApplyEditingStateToView()
    {
        masterVolume.SetValue(editingState.MasterVolume);
        sfxVolume.SetValue(editingState.SfxVolume);
        musicVolume.SetValue(editingState.MusicVolume);
        RefreshUseMasterVolumeToggle();
    }

    private void RefreshUseMasterVolumeToggle()
    {
        SetToggleSelected(useMasterVolumeYesMark, editingState.UseMasterVolume);
        SetToggleSelected(useMasterVolumeNoMark, !editingState.UseMasterVolume);
    }

    private void SetToggleSelected(Image mark, bool selected)
    {
        if (mark == null)
        {
            return;
        }

        mark.sprite = selected ? selectedToggleSprite : defaultToggleSprite;
    }

    private static void ApplyAudioSettings(SettingsPanelState state)
    {
        AudioManager audioManager = AudioManager.Instance;
        if (audioManager == null)
        {
            return;
        }

        audioManager.SetMasterVolume(state.UseMasterVolume ? state.MasterVolume : AudioConstants.MIN_VOLUME);
        audioManager.SetSfxVolume(state.SfxVolume);
        audioManager.SetMusicVolume(state.MusicVolume);
    }

    private void ValidateConfiguration()
    {
        masterVolume.Validate($"{nameof(SettingsPanelManager)} '{name}' master volume");
        sfxVolume.Validate($"{nameof(SettingsPanelManager)} '{name}' sfx volume");
        musicVolume.Validate($"{nameof(SettingsPanelManager)} '{name}' music volume");

        if (useMasterVolumeYesButton == null)
        {
            throw new MissingReferenceException($"{nameof(SettingsPanelManager)} '{name}' is missing use master volume yes button.");
        }

        if (useMasterVolumeYesMark == null)
        {
            throw new MissingReferenceException($"{nameof(SettingsPanelManager)} '{name}' is missing use master volume yes mark.");
        }

        if (useMasterVolumeNoButton == null)
        {
            throw new MissingReferenceException($"{nameof(SettingsPanelManager)} '{name}' is missing use master volume no button.");
        }

        if (useMasterVolumeNoMark == null)
        {
            throw new MissingReferenceException($"{nameof(SettingsPanelManager)} '{name}' is missing use master volume no mark.");
        }

        if (defaultToggleSprite == null)
        {
            throw new MissingReferenceException($"{nameof(SettingsPanelManager)} '{name}' is missing default toggle sprite.");
        }

        if (selectedToggleSprite == null)
        {
            throw new MissingReferenceException($"{nameof(SettingsPanelManager)} '{name}' is missing selected toggle sprite.");
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

    [Serializable]
    private struct SettingsPanelState
    {
        public float MasterVolume;
        public float SfxVolume;
        public float MusicVolume;
        public bool UseMasterVolume;

        public SettingsPanelState(float masterVolume, float sfxVolume, float musicVolume, bool useMasterVolume)
        {
            MasterVolume = Mathf.Clamp01(masterVolume);
            SfxVolume = Mathf.Clamp01(sfxVolume);
            MusicVolume = Mathf.Clamp01(musicVolume);
            UseMasterVolume = useMasterVolume;
        }

        public static SettingsPanelState Default => new(
            AudioConstants.DEFAULT_VOLUME,
            AudioConstants.DEFAULT_VOLUME,
            AudioConstants.DEFAULT_VOLUME,
            true);
    }

    [Serializable]
    private sealed class VolumeStepControl
    {
        [SerializeField] private RectTransform inputArea;
        [SerializeField] private Image[] steps = Array.Empty<Image>();
        [SerializeField] private VolumeStepSpriteSet activeSprites = new();
        [SerializeField] private VolumeStepSpriteSet inactiveSprites = new();

        private Action<float> valueChanged;
        private int selectedStep = DEFAULT_SELECTED_STEP;
        private bool initialized;

        public void Initialize(Action<float> onValueChanged)
        {
            valueChanged = onValueChanged;
            ResolveSteps();
            if (steps == null || steps.Length == 0)
            {
                throw new MissingReferenceException($"{nameof(SettingsPanelManager)} volume control is missing step images.");
            }

            initialized = true;
            Refresh();
        }

        public void SetValue(float normalizedValue)
        {
            SetSelectedStep(NormalizedToStep(normalizedValue), false);
        }

        public void Validate(string context)
        {
            if (inputArea == null)
            {
                throw new MissingReferenceException($"{context} is missing input area.");
            }

            if (activeSprites == null)
            {
                throw new MissingReferenceException($"{context} is missing active sprite set.");
            }

            if (inactiveSprites == null)
            {
                throw new MissingReferenceException($"{context} is missing inactive sprite set.");
            }

            activeSprites.Validate($"{context} active sprites");
            inactiveSprites.Validate($"{context} inactive sprites");
        }

        private void ResolveSteps()
        {
            if ((steps != null && steps.Length > 0) || inputArea == null)
            {
                return;
            }

            Transform stepRoot = inputArea.Find("Horizontal");
            if (stepRoot == null)
            {
                stepRoot = inputArea;
            }

            int childCount = stepRoot.childCount;
            Image[] resolvedSteps = new Image[childCount];
            int resolvedCount = 0;
            for (int i = 0; i < childCount; i++)
            {
                if (stepRoot.GetChild(i).TryGetComponent(out Image stepImage))
                {
                    resolvedSteps[resolvedCount] = stepImage;
                    resolvedCount++;
                }
            }

            if (resolvedCount == resolvedSteps.Length)
            {
                steps = resolvedSteps;
                return;
            }

            Array.Resize(ref resolvedSteps, resolvedCount);
            steps = resolvedSteps;
        }

        public bool TrySetValueFromRaycast(GameObject raycastTarget)
        {
            if (raycastTarget == null || steps == null)
            {
                return false;
            }

            for (int i = 0; i < steps.Length; i++)
            {
                Image step = steps[i];
                if (step == null || step.gameObject != raycastTarget)
                {
                    continue;
                }

                SetSelectedStep(i + 1, true);
                return true;
            }

            return false;
        }

        private float GetValue()
        {
            if (steps == null || steps.Length == 0)
            {
                return 0f;
            }

            return (float)selectedStep / steps.Length;
        }

        private int NormalizedToStep(float normalizedValue)
        {
            int stepCount = steps != null ? steps.Length : 0;
            if (stepCount <= 0)
            {
                return 0;
            }

            float clampedValue = Mathf.Clamp01(normalizedValue);
            return Mathf.Clamp(Mathf.RoundToInt(clampedValue * stepCount), 0, stepCount);
        }

        private void SetSelectedStep(int step, bool notify)
        {
            int stepCount = steps != null ? steps.Length : 0;
            int clampedStep = Mathf.Clamp(step, 0, stepCount);
            if (selectedStep == clampedStep)
            {
                return;
            }

            selectedStep = clampedStep;
            Refresh();

            if (notify)
            {
                valueChanged?.Invoke(GetValue());
            }
        }

        private void Refresh()
        {
            if (!initialized || steps == null)
            {
                return;
            }

            for (int i = 0; i < steps.Length; i++)
            {
                Image step = steps[i];
                if (step == null)
                {
                    continue;
                }

                VolumeStepSpriteSet spriteSet = i < selectedStep ? activeSprites : inactiveSprites;
                step.sprite = spriteSet.GetSprite(i, steps.Length);
            }
        }
    }

    [Serializable]
    private sealed class VolumeStepSpriteSet
    {
        [SerializeField] private Sprite left;
        [SerializeField] private Sprite middleOdd;
        [SerializeField] private Sprite middleEven;
        [SerializeField] private Sprite right;

        public Sprite GetSprite(int stepIndex, int stepCount)
        {
            if (stepIndex <= 0)
            {
                return left;
            }

            if (stepIndex >= stepCount - 1)
            {
                return right;
            }

            return stepIndex % 2 == 1 ? middleOdd : middleEven;
        }

        public void Validate(string context)
        {
            if (left == null)
            {
                throw new MissingReferenceException($"{context} is missing left sprite.");
            }

            if (middleOdd == null)
            {
                throw new MissingReferenceException($"{context} is missing middle odd sprite.");
            }

            if (middleEven == null)
            {
                throw new MissingReferenceException($"{context} is missing middle even sprite.");
            }

            if (right == null)
            {
                throw new MissingReferenceException($"{context} is missing right sprite.");
            }
        }
    }
}
