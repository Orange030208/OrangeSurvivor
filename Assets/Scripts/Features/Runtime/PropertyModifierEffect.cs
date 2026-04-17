using UnityEngine;

[HideInFeatureMenu]
[System.Serializable]
public sealed class PropertyModifierEffect : FeatureEffectBase
{
    [SerializeField] private PropEntry modifier;
    [HideInInspector]
    [SerializeField] private string sourceId;

    public PropertyModifierEffect()
    {
    }

    public PropertyModifierEffect(string runtimeFeatureId, string sourceId, PropType property, float value)
        : this(runtimeFeatureId, sourceId, new PropEntry(property, value))
    {
    }

    public PropertyModifierEffect(string runtimeFeatureId, string sourceId, PropEntry modifier)
    {
        RuntimeFeatureId = runtimeFeatureId;
        this.sourceId = sourceId;
        this.modifier = modifier;
    }

    public override string FeatureDescription => string.Empty;

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
