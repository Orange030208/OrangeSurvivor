using System;
using System.Collections.Generic;

namespace Orange.Attributes
{
    /// <summary>
    /// 纯 C# 属性结算器，使用外部提供的稳定 Key 标识属性。
    /// 项目侧枚举、显示名、序列化和具体数值语义应放在适配层中。
    /// </summary>
    public sealed class AttributeSystem<TKey>
    where TKey : notnull
    {
        public const int RATIO_SCALE = 10000;

        private readonly HashSet<TKey> knownAttributeIds = new();
        private readonly Dictionary<TKey, int> defaultValues = new();
        private readonly Dictionary<TKey, int> baseValues = new();
        private readonly Dictionary<string, List<AttributeModifier<TKey>>> modifierSources = new();
        private readonly List<AttributeMapping<TKey>> mappings = new();
        private readonly Dictionary<TKey, int> mappedAddValues = new();
        private readonly Dictionary<TKey, int> unmappedCalculatedValues = new();
        private readonly Dictionary<TKey, int> calculatedValues = new();
        private readonly Dictionary<TKey, Action<int>> valueChangedByAttribute = new();

        public event Action ValuesChanged;

        public void Clear()
        {
            knownAttributeIds.Clear();
            defaultValues.Clear();
            baseValues.Clear();
            modifierSources.Clear();
            mappings.Clear();
            mappedAddValues.Clear();
            unmappedCalculatedValues.Clear();
            calculatedValues.Clear();
        }

        public void RegisterAttribute(TKey attributeId, int defaultValue = 0)
        {
            knownAttributeIds.Add(attributeId);
            defaultValues[attributeId] = defaultValue;
        }

        public void AddBaseValue(TKey attributeId, int value)
        {
            knownAttributeIds.Add(attributeId);
            if (baseValues.TryGetValue(attributeId, out int currentValue))
            {
                baseValues[attributeId] = currentValue + value;
                return;
            }

            baseValues[attributeId] = value;
        }

        public void SetMappings(IReadOnlyList<AttributeMapping<TKey>> newMappings)
        {
            mappings.Clear();
            if (newMappings == null)
            {
                return;
            }

            for (int i = 0; i < newMappings.Count; i++)
            {
                AttributeMapping<TKey> mapping = newMappings[i];
                mappings.Add(mapping);
                knownAttributeIds.Add(mapping.SourceAttributeId);
                knownAttributeIds.Add(mapping.TargetAttributeId);
            }
        }

        public void AddModifier(string sourceId, AttributeModifier<TKey> modifier)
        {
            AddModifiers(sourceId, new[] { modifier });
        }

        public void AddModifiers(string sourceId, IReadOnlyList<AttributeModifier<TKey>> modifiers)
        {
            if (string.IsNullOrWhiteSpace(sourceId) || modifiers == null || modifiers.Count == 0)
            {
                return;
            }

            List<AttributeModifier<TKey>> copiedModifiers = new(modifiers.Count);
            for (int i = 0; i < modifiers.Count; i++)
            {
                copiedModifiers.Add(modifiers[i]);
                knownAttributeIds.Add(modifiers[i].AttributeId);
            }

            modifierSources[sourceId] = copiedModifiers;
        }

        public bool RemoveModifier(string sourceId, TKey attributeId, AttributeModifierType modifierType)
        {
            if (string.IsNullOrWhiteSpace(sourceId) ||
                !modifierSources.TryGetValue(sourceId, out List<AttributeModifier<TKey>> modifiers))
            {
                return false;
            }

            int oldCount = modifiers.Count;
            modifiers.RemoveAll(entry =>
                EqualityComparer<TKey>.Default.Equals(entry.AttributeId, attributeId) &&
                entry.ModifierType == modifierType);
            if (modifiers.Count == oldCount)
            {
                return false;
            }

            if (modifiers.Count == 0)
            {
                modifierSources.Remove(sourceId);
            }

            return true;
        }

        public bool RemoveModifiers(string sourceId)
        {
            return !string.IsNullOrWhiteSpace(sourceId) && modifierSources.Remove(sourceId);
        }

        public void SubscribeValueChanged(TKey attributeId, Action<int> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            knownAttributeIds.Add(attributeId);
            if (valueChangedByAttribute.TryGetValue(attributeId, out Action<int> existingHandler))
            {
                valueChangedByAttribute[attributeId] = existingHandler + handler;
                return;
            }

            valueChangedByAttribute[attributeId] = handler;
        }

        public void UnsubscribeValueChanged(TKey attributeId, Action<int> handler)
        {
            if (handler == null ||
                !valueChangedByAttribute.TryGetValue(attributeId, out Action<int> existingHandler))
            {
                return;
            }

            Action<int> newHandler = existingHandler - handler;
            if (newHandler == null)
            {
                valueChangedByAttribute.Remove(attributeId);
                return;
            }

            valueChangedByAttribute[attributeId] = newHandler;
        }

        public void RecalculateAll(bool notifyChanges = true, bool notifyAllWhenUnchanged = false)
        {
            List<TKey> changedAttributeIds = notifyChanges ? new List<TKey>() : null;

            unmappedCalculatedValues.Clear();
            foreach (TKey attributeId in knownAttributeIds)
            {
                int unmappedValue = CalculateFinalValue(attributeId, GetBaseValue(attributeId), 0);
                unmappedCalculatedValues[attributeId] = unmappedValue;
            }

            RebuildMappedAddValues();

            foreach (TKey attributeId in knownAttributeIds)
            {
                int oldValue = calculatedValues.TryGetValue(attributeId, out int existingValue)
                    ? existingValue
                    : GetDefaultValue(attributeId);
                int newValue = CalculateFinalValue(attributeId);
                calculatedValues[attributeId] = newValue;

                if (notifyChanges && oldValue != newValue)
                {
                    changedAttributeIds.Add(attributeId);
                }
            }

            if (!notifyChanges)
            {
                return;
            }

            for (int i = 0; i < changedAttributeIds.Count; i++)
            {
                TKey attributeId = changedAttributeIds[i];
                NotifyValueChanged(attributeId, calculatedValues[attributeId]);
            }

            if (changedAttributeIds.Count > 0 || notifyAllWhenUnchanged)
            {
                ValuesChanged?.Invoke();
            }
        }

        public int GetValue(TKey attributeId)
        {
            return calculatedValues.TryGetValue(attributeId, out int value)
                ? value
                : GetDefaultValue(attributeId);
        }

        public int GetValueWithAdditionalBase(TKey attributeId, int additionalBaseValue)
        {
            int explicitBaseValue = baseValues.TryGetValue(attributeId, out int value) ? value : 0;
            int mappedAddValue = mappedAddValues.TryGetValue(attributeId, out int mappedValue) ? mappedValue : 0;
            return CalculateFinalValue(attributeId, explicitBaseValue + additionalBaseValue, mappedAddValue);
        }

        public int GetBaseValue(TKey attributeId)
        {
            return baseValues.TryGetValue(attributeId, out int value)
                ? value
                : GetDefaultValue(attributeId);
        }

        public int GetDefaultValue(TKey attributeId)
        {
            return defaultValues.TryGetValue(attributeId, out int value) ? value : 0;
        }

        public Dictionary<TKey, int> GetAllValues()
        {
            return new Dictionary<TKey, int>(calculatedValues);
        }

        private int CalculateFinalValue(TKey attributeId)
        {
            int baseValue = GetBaseValue(attributeId);
            int mappedAddValue = mappedAddValues.TryGetValue(attributeId, out int mappedValue) ? mappedValue : 0;
            return CalculateFinalValue(attributeId, baseValue, mappedAddValue);
        }

        private void NotifyValueChanged(TKey attributeId, int value)
        {
            if (valueChangedByAttribute.TryGetValue(attributeId, out Action<int> handler))
            {
                handler.Invoke(value);
            }
        }

        private int CalculateFinalValue(TKey attributeId, int baseValue, int additionalAddValue)
        {
            int addValue = additionalAddValue;
            int baseOnlyMultiplierValue = 0;
            int bonusMultiplierValue = 0;
            int finalMultiplierValue = 0;

            foreach (List<AttributeModifier<TKey>> source in modifierSources.Values)
            {
                for (int i = 0; i < source.Count; i++)
                {
                    AttributeModifier<TKey> entry = source[i];
                    if (!EqualityComparer<TKey>.Default.Equals(entry.AttributeId, attributeId))
                    {
                        continue;
                    }

                    switch (entry.ModifierType)
                    {
                        case AttributeModifierType.Add:
                            addValue += entry.Value;
                            break;
                        case AttributeModifierType.BaseMultiplier:
                            baseOnlyMultiplierValue += entry.Value;
                            break;
                        case AttributeModifierType.BonusMultiplier:
                            bonusMultiplierValue += entry.Value;
                            break;
                        case AttributeModifierType.FinalMultiplier:
                            finalMultiplierValue += entry.Value;
                            break;
                    }
                }
            }

            long baseValueAfterMultiplier = ApplyRatio(baseValue, RATIO_SCALE + baseOnlyMultiplierValue);
            long result = ApplyRatio(baseValueAfterMultiplier + addValue, RATIO_SCALE + bonusMultiplierValue);
            return ClampToInt(ApplyRatio(result, RATIO_SCALE + finalMultiplierValue));
        }

        private void RebuildMappedAddValues()
        {
            mappedAddValues.Clear();
            for (int i = 0; i < mappings.Count; i++)
            {
                AttributeMapping<TKey> mapping = mappings[i];
                if (mapping.ConversionRatio == 0)
                {
                    continue;
                }

                int sourceValue = unmappedCalculatedValues.TryGetValue(
                    mapping.SourceAttributeId,
                    out int calculatedValue)
                    ? calculatedValue
                    : GetDefaultValue(mapping.SourceAttributeId);
                int mappedAddValue = ClampToInt(ApplyRatio(sourceValue, mapping.ConversionRatio));
                AddValue(mappedAddValues, mapping.TargetAttributeId, mappedAddValue);
            }
        }

        private static void AddValue(Dictionary<TKey, int> target, TKey attributeId, int value)
        {
            if (target.TryGetValue(attributeId, out int currentValue))
            {
                target[attributeId] = currentValue + value;
                return;
            }

            target[attributeId] = value;
        }

        private static long ApplyRatio(long value, int ratio)
        {
            long numerator = value * ratio;
            long halfScale = RATIO_SCALE / 2;
            return numerator >= 0
                ? (numerator + halfScale) / RATIO_SCALE
                : (numerator - halfScale) / RATIO_SCALE;
        }

        private static int ClampToInt(long value)
        {
            if (value > int.MaxValue)
            {
                return int.MaxValue;
            }

            if (value < int.MinValue)
            {
                return int.MinValue;
            }

            return (int)value;
        }
    }

}
