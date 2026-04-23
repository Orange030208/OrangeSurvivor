using System;
using UnityEngine;

public sealed class PropertiesDescriberBinding
{
    private readonly Describer propertiesDescriber;

    private PropertiesManager propertiesManager;

    public PropertiesDescriberBinding(string ownerTypeName, string ownerName, string missingFieldName, Describer propertiesDescriber)
    {
        string resolvedOwnerTypeName = string.IsNullOrWhiteSpace(ownerTypeName) ? nameof(PropertiesDescriberBinding) : ownerTypeName;
        string resolvedOwnerName = string.IsNullOrWhiteSpace(ownerName) ? resolvedOwnerTypeName : ownerName;
        string resolvedMissingFieldName = string.IsNullOrWhiteSpace(missingFieldName) ? nameof(Describer) : missingFieldName;

        this.propertiesDescriber = propertiesDescriber ?? throw new MissingReferenceException($"{resolvedOwnerTypeName} '{resolvedOwnerName}' is missing {resolvedMissingFieldName}.");
    }

    public void Bind(PropertiesManager newPropertiesManager)
    {
        Unbind();
        propertiesManager = newPropertiesManager;
        Refresh();

        if (propertiesManager != null)
        {
            propertiesManager.OnAllPropertiesChanged += OnAllPropertiesChanged;
        }
    }

    public void Unbind()
    {
        if (propertiesManager == null)
        {
            ClearDisplay();
            return;
        }

        propertiesManager.OnAllPropertiesChanged -= OnAllPropertiesChanged;
        propertiesManager = null;
        ClearDisplay();
    }

    private void OnAllPropertiesChanged()
    {
        Refresh();
    }

    private void Refresh()
    {
        propertiesDescriber.Display(propertiesManager);
    }

    private void ClearDisplay()
    {
        propertiesDescriber.Display((IDescribable)null);
    }
}
