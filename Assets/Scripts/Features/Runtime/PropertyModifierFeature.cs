using UnityEngine;

[System.Serializable]
public sealed class PropertyModifierFeature : FeatureEffectBase
{
    [SerializeField] private PropModifierData modifier;
    private string sourceId;

    public PropertyModifierFeature()
    {
    }

    public PropertyModifierFeature(string sourceId, PropModifierData modifier)
    {
        this.sourceId = sourceId;
        this.modifier = modifier;
    }

    public override string Description => string.Empty;
    

    public override void OnInstall()
    {
        if (Context?.PropertiesManager == null)
        {
            return;
        }

        Context.PropertiesManager.AddModifier(sourceId, modifier);
    }

    public override void OnUninstall()
    {
        if (Context?.PropertiesManager == null)
        {
            return;
        }

        Context.PropertiesManager.RemoveModifier(sourceId, modifier.propType, modifier.modifierType);
    }
}
