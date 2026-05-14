using System;
using Orange.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class SettingsOptionRow : ViewPartBase
{
    public readonly struct Context
    {
        public Context(string label, Action<int> offsetRequested)
        {
            Label = label;
            OffsetRequested = offsetRequested;
        }

        public string Label { get; }
        public Action<int> OffsetRequested { get; }
    }

    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private Button previousButton;
    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private Button nextButton;

    private Action<int> offsetRequested;

    public Selectable DefaultSelectable => previousButton != null ? previousButton : nextButton;

    public override void Bind(object context)
    {
        if (context is not Context rowContext)
        {
            throw new ArgumentException($"{nameof(SettingsOptionRow)} '{name}' expects {nameof(Context)}.", nameof(context));
        }

        Initialize(rowContext.Label, rowContext.OffsetRequested);
    }

    public override void Unbind()
    {
        offsetRequested = null;
        if (previousButton != null)
        {
            previousButton.onClick.RemoveListener(OnPreviousClicked);
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(OnNextClicked);
        }
    }

    public void Initialize(string label, Action<int> onOffsetRequested)
    {
        offsetRequested = onOffsetRequested;
        if (labelText != null)
        {
            labelText.text = label;
        }

        Validate();
        previousButton.onClick.RemoveListener(OnPreviousClicked);
        nextButton.onClick.RemoveListener(OnNextClicked);
        previousButton.onClick.AddListener(OnPreviousClicked);
        nextButton.onClick.AddListener(OnNextClicked);
    }

    public void SetValue(string value)
    {
        if (valueText != null)
        {
            valueText.text = string.IsNullOrWhiteSpace(value) ? "-" : value;
        }
    }

    public void SetInteractable(bool interactable)
    {
        if (previousButton != null)
        {
            previousButton.interactable = interactable;
        }

        if (nextButton != null)
        {
            nextButton.interactable = interactable;
        }
    }

    public void Validate()
    {
        if (previousButton == null)
        {
            throw new MissingReferenceException($"{nameof(SettingsOptionRow)} '{name}' is missing previous button.");
        }

        if (nextButton == null)
        {
            throw new MissingReferenceException($"{nameof(SettingsOptionRow)} '{name}' is missing next button.");
        }

        if (valueText == null)
        {
            throw new MissingReferenceException($"{nameof(SettingsOptionRow)} '{name}' is missing value text.");
        }
    }

    private void OnPreviousClicked()
    {
        offsetRequested?.Invoke(-1);
    }

    private void OnNextClicked()
    {
        offsetRequested?.Invoke(1);
    }
}
