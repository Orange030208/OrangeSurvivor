using Survivors.Player;
using UnityEngine;

namespace Survivors.Accessory
{
    public class PropertyModifierEffect : AccessoryEffectBase
    {
        private readonly PropType targetProperty;
        private readonly float modifierValue;
        private readonly string sourceId;

        public PropertyModifierEffect(string effectId, string sourceId, PropType property, float value)
        {
            EffectId = effectId;
            this.sourceId = sourceId;
            targetProperty = property;
            modifierValue = value;
        }

        public override void OnEquip(GameObject owner, PropertiesManager propertiesManager)
        {
            if (propertiesManager == null) return;
            propertiesManager.AddBonusModifier(sourceId, targetProperty, modifierValue);
        }

        public override void OnUnequip(GameObject owner, PropertiesManager propertiesManager)
        {
            if (propertiesManager == null) return;
            propertiesManager.RemoveBonusModifier(sourceId, targetProperty);
        }
    }
}
