using UnityEngine;

[HideInFeatureMenu]
[System.Serializable]
public sealed class PropertyModifierEffect : FeatureEffectBase
{
    [SerializeField] private PropModifierData modifier;
    [HideInInspector]
    [SerializeField] private string sourceId;

    public PropertyModifierEffect()
    {
    }

    public PropertyModifierEffect(string sourceId, PropModifierData modifier)
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
