using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public sealed class PropertyModifierFeature : FeatureBase
{
    [SerializeField] private PropModifierData modifier;
    private string runtimeSourceId;

    public PropModifierData Modifier => modifier;

    public PropertyModifierFeature()
    {
    }

    public PropertyModifierFeature(PropModifierData modifier)
    {
        this.modifier = modifier;
    }

    public PropertyModifierFeature(string sourceId, PropModifierData modifier)
    {
        SourceId = sourceId;
        this.modifier = modifier;
    }

    public override string Title => modifier.GetDisplayName();
    public override string Description => modifier.GetDisplayValueText();

    public override void OnInstall()
    {
        if (Context?.PropertiesManager == null)
        {
            return;
        }

        runtimeSourceId = ResolveRuntimeSourceId();
        Context.PropertiesManager.AddModifier(runtimeSourceId, modifier);
    }

    public override void OnUninstall()
    {
        if (Context?.PropertiesManager == null)
        {
            return;
        }

        Context.PropertiesManager.RemoveModifier(ResolveRuntimeSourceId(), modifier.propType, modifier.modifierType);
    }

    public override IEnumerable<DescriptorInfo> GetExtraInfos()
    {
        yield return new DescriptorInfo(modifier.GetDisplayName(), modifier.GetDisplayValueText());
    }

    private string ResolveRuntimeSourceId()
    {
        if (!string.IsNullOrWhiteSpace(runtimeSourceId))
        {
            return runtimeSourceId;
        }

        return string.IsNullOrWhiteSpace(SourceId)
            ? $"{nameof(PropertyModifierFeature)}_{GetHashCode()}"
            : $"{SourceId}:{nameof(PropertyModifierFeature)}_{GetHashCode()}";
    }
}
