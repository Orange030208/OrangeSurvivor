using System;
using Orange.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class SettingsRebindRow : ViewPartBase
{
    public readonly struct Context
    {
        public Context(Action<SettingsRebindRow> rebindRequested)
        {
            RebindRequested = rebindRequested;
        }

        public Action<SettingsRebindRow> RebindRequested { get; }
    }

    [SerializeField] private string actionPath;
    [SerializeField] private string compositePartName;
    [SerializeField] private string label;
    [SerializeField] private string controlScheme = "Keyboard";
    [SerializeField] private string bindingGroup;
    [SerializeField] private string requiredControlPath;
    [SerializeField] private string[] cancelControlPaths = Array.Empty<string>();
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private Button rebindButton;

    private Action<SettingsRebindRow> rebindRequested;

    public string ActionPath => actionPath;
    public string CompositePartName => string.IsNullOrWhiteSpace(compositePartName) ? null : compositePartName;
    public string DisplayLabel => $"{label} ({controlScheme})";
    public string ControlScheme => controlScheme;
    public string BindingGroup => bindingGroup;
    public string RequiredControlPath => requiredControlPath;
    public string[] CancelControlPaths => cancelControlPaths ?? Array.Empty<string>();
    public Selectable DefaultSelectable => rebindButton;

    public override void Bind(object context)
    {
        if (context is not Context rowContext)
        {
            throw new ArgumentException($"{nameof(SettingsRebindRow)} '{name}' expects {nameof(Context)}.", nameof(context));
        }

        Initialize(rowContext.RebindRequested);
    }

    public override void Unbind()
    {
        rebindRequested = null;
        if (rebindButton != null)
        {
            rebindButton.onClick.RemoveListener(OnRebindClicked);
        }
    }

    public void Initialize(Action<SettingsRebindRow> onRebindRequested)
    {
        rebindRequested = onRebindRequested;
        Validate();
        RefreshLabel();
        rebindButton.onClick.RemoveListener(OnRebindClicked);
        rebindButton.onClick.AddListener(OnRebindClicked);
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
        if (rebindButton != null)
        {
            rebindButton.interactable = interactable;
        }
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(actionPath))
        {
            throw new MissingReferenceException($"{nameof(SettingsRebindRow)} '{name}' is missing action path.");
        }

        if (string.IsNullOrWhiteSpace(label))
        {
            throw new MissingReferenceException($"{nameof(SettingsRebindRow)} '{name}' is missing label.");
        }

        if (string.IsNullOrWhiteSpace(controlScheme))
        {
            throw new MissingReferenceException($"{nameof(SettingsRebindRow)} '{name}' is missing control scheme.");
        }

        if (valueText == null)
        {
            throw new MissingReferenceException($"{nameof(SettingsRebindRow)} '{name}' is missing value text.");
        }

        if (rebindButton == null)
        {
            throw new MissingReferenceException($"{nameof(SettingsRebindRow)} '{name}' is missing rebind button.");
        }
    }

    private void RefreshLabel()
    {
        if (labelText != null)
        {
            labelText.text = DisplayLabel;
        }
    }

    private void OnRebindClicked()
    {
        rebindRequested?.Invoke(this);
    }
}
