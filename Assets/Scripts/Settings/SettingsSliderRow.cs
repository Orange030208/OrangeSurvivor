using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class SettingsSliderRow : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI valueText;

    private Action<float> valueChanged;
    private bool suppressNotify;

    public Selectable DefaultSelectable => slider;

    public void Initialize(string label, Action<float> onValueChanged)
    {
        valueChanged = onValueChanged;
        if (labelText != null)
        {
            labelText.text = label;
        }

        Validate();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.onValueChanged.RemoveListener(OnSliderValueChanged);
        slider.onValueChanged.AddListener(OnSliderValueChanged);
    }

    public void SetValue(float value)
    {
        Validate();
        suppressNotify = true;
        slider.value = Mathf.Clamp01(value);
        suppressNotify = false;
        RefreshValueText(slider.value);
    }

    public void SetInteractable(bool interactable)
    {
        if (slider != null)
        {
            slider.interactable = interactable;
        }
    }

    public void Validate()
    {
        if (slider == null)
        {
            throw new MissingReferenceException($"{nameof(SettingsSliderRow)} '{name}' is missing slider.");
        }

        if (valueText == null)
        {
            throw new MissingReferenceException($"{nameof(SettingsSliderRow)} '{name}' is missing value text.");
        }
    }

    private void OnSliderValueChanged(float value)
    {
        RefreshValueText(value);
        if (!suppressNotify)
        {
            valueChanged?.Invoke(Mathf.Clamp01(value));
        }
    }

    private void RefreshValueText(float value)
    {
        if (valueText != null)
        {
            valueText.text = $"{Mathf.RoundToInt(Mathf.Clamp01(value) * 100f)}%";
        }
    }
}
