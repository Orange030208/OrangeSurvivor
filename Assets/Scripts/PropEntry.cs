using System;

[Serializable]
public struct PropEntry
{
    public PropType propType;
    public float value;
    
    public PropEntry(PropType propType, float value)
    {
        this.propType = propType;
        this.value = value;
    }
}