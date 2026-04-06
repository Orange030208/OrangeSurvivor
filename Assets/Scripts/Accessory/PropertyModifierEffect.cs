using Survivors.Player;
using UnityEngine;

namespace Survivors.Accessory
{
    public class PropertyModifierEffect : AccessoryEffectBase
    {
        private PropType targetProperty;
        private float modifierValue;
        private string sourceId;

        public PropertyModifierEffect(string effectId, PropType property, float value)
        {
            EffectId = effectId;
            targetProperty = property;
            modifierValue = value;
            sourceId = $"Effect_{effectId}";
        }

        public override void OnEquip(GameObject owner, PropertiesManager propertiesManager)
        {
            if (propertiesManager == null) return;
            propertiesManager.AddAdditiveModifier(sourceId, targetProperty, modifierValue);
        }

        public override void OnUnequip(GameObject owner, PropertiesManager propertiesManager)
        {
            if (propertiesManager == null) return;
            propertiesManager.RemoveAdditiveModifier(sourceId, targetProperty);
        }
    }
}
