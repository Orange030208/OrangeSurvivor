using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class SettingsRebindRow : MonoBehaviour
{
    [SerializeField] private string actionPath;
    [SerializeField] private string compositePartName;
    [SerializeField] private string label;
    [SerializeField] private string controlScheme = "Keyboard";
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private Button rebindButton;

    private Action<SettingsRebindRow> rebindRequested;

    public InputRebindService.RebindEntry Entry =>
        new(actionPath, string.IsNullOrWhiteSpace(compositePartName) ? null : compositePartName, label, controlScheme);

    public string ControlScheme => controlScheme;
    public Selectable DefaultSelectable => rebindButton;

    public void Configure(InputRebindService.RebindEntry entry)
    {
        actionPath = entry.ActionPath;
        compositePartName = entry.CompositePartName;
        label = entry.Label;
        controlScheme = entry.ControlScheme;
        RefreshLabel();
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
            labelText.text = Entry.DisplayLabel;
        }
    }

    private void OnRebindClicked()
    {
        rebindRequested?.Invoke(this);
    }
}
