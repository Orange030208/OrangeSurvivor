using System.Collections.Generic;
using UnityEngine;

public sealed class ContentFactSet
{
    private readonly Dictionary<FactDefinitionSO, ContentFactValue> valuesByDefinition = new();
    // 资产引用是主键，稳定 ID 是补充索引，供运行时 Modifier 和未来存档/调试链路复用。
    private readonly Dictionary<string, ContentFactValue> valuesById = new(System.StringComparer.Ordinal);

    public static ContentFactSet Empty { get; } = new();

    public void Set(FactDefinitionSO definition, ContentFactValue value)
    {
        if (definition == null)
        {
            return;
        }

        if (!IsMatchingValueType(definition.ValueType, value.ValueType))
        {
            Debug.LogWarning(
                $"[ContentFactSet] Fact '{definition.FactId}' expected {definition.ValueType}, received {value.ValueType}.");
            return;
        }

        valuesByDefinition[definition] = value;
        if (!string.IsNullOrWhiteSpace(definition.FactId))
        {
            valuesById[definition.FactId] = value;
        }
    }

    public void Set(string factId, ContentFactValue value)
    {
        if (string.IsNullOrWhiteSpace(factId))
        {
            return;
        }

        valuesById[factId] = value;
    }

    public bool TryGet(FactDefinitionSO definition, out ContentFactValue value)
    {
        value = default;
        if (definition == null)
        {
            return false;
        }

        if (valuesByDefinition.TryGetValue(definition, out value))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(definition.FactId)
               && valuesById.TryGetValue(definition.FactId, out value);
    }

    public bool TryGet(string factId, out ContentFactValue value)
    {
        value = default;
        return !string.IsNullOrWhiteSpace(factId) && valuesById.TryGetValue(factId, out value);
    }

    public bool Has(FactDefinitionSO definition)
    {
        return TryGet(definition, out _);
    }

    private static bool IsMatchingValueType(FactValueType expected, FactValueType actual)
    {
        if (expected == actual)
        {
            return true;
        }

        return (expected == FactValueType.Float && actual == FactValueType.Int)
               || (expected == FactValueType.Int && actual == FactValueType.Float);
    }
}
