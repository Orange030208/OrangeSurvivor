using System;
using UnityEngine;

[Serializable]
public struct ContentFactValue
{
    [SerializeField] private FactValueType valueType;
    [SerializeField] private bool boolValue;
    [SerializeField] private int intValue;
    [SerializeField] private float floatValue;
    [SerializeField] private string stringValue;
    [SerializeField] private UnityEngine.Object objectValue;

    public FactValueType ValueType => valueType;
    public bool BoolValue => boolValue;
    public int IntValue => intValue;
    public float FloatValue => valueType == FactValueType.Int ? intValue : floatValue;
    public string StringValue => stringValue;
    public UnityEngine.Object ObjectValue => objectValue;

    public static ContentFactValue FromBool(bool value)
    {
        return new ContentFactValue { valueType = FactValueType.Bool, boolValue = value };
    }

    public static ContentFactValue FromInt(int value)
    {
        return new ContentFactValue
        {
            valueType = FactValueType.Int,
            intValue = value,
            floatValue = value
        };
    }

    public static ContentFactValue FromFloat(float value)
    {
        return new ContentFactValue { valueType = FactValueType.Float, floatValue = value };
    }

    public static ContentFactValue FromString(string value)
    {
        return new ContentFactValue { valueType = FactValueType.String, stringValue = value ?? string.Empty };
    }

    public static ContentFactValue FromObject(UnityEngine.Object value)
    {
        return new ContentFactValue { valueType = FactValueType.UnityObject, objectValue = value };
    }

    public bool TryGetNumber(out float value)
    {
        if (valueType == FactValueType.Int)
        {
            value = intValue;
            return true;
        }

        if (valueType == FactValueType.Float)
        {
            value = floatValue;
            return true;
        }

        value = 0f;
        return false;
    }

    public bool EqualsValue(ContentFactValue other)
    {
        if (valueType != other.valueType)
        {
            return TryGetNumber(out float leftNumber)
                   && other.TryGetNumber(out float rightNumber)
                   && Mathf.Approximately(leftNumber, rightNumber);
        }

        return valueType switch
        {
            FactValueType.Bool => boolValue == other.boolValue,
            FactValueType.Int => intValue == other.intValue,
            FactValueType.Float => Mathf.Approximately(floatValue, other.floatValue),
            FactValueType.String => string.Equals(stringValue, other.stringValue, StringComparison.Ordinal),
            FactValueType.UnityObject => objectValue == other.objectValue,
            _ => false
        };
    }
}
