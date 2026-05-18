#if UNITY_EDITOR
using System;
using System.Collections.Generic;

[Serializable]
public sealed class BuffJsonFile
{
    public List<BuffJsonBuff> buffs = new();
}

[Serializable]
public sealed class BuffJsonBuff
{
    public string buffId;
    public string displayName;
    public string description;
    public string polarity;
    public string durationPolicy;
    public float durationSeconds;
    public int maxStackCount;
    public string refreshMode;
    public string overflowMode;
    public List<BuffJsonFeature> specialFeatures = new();
}

[Serializable]
public sealed class BuffJsonFeature
{
    public string type;
    public BuffJsonPropModifier modifier;
    public BuffJsonDamageOverTime damageOverTime;
}

[Serializable]
public sealed class BuffJsonPropModifier
{
    public string propType;
    public string modifierType;
    public float value;
}

[Serializable]
public sealed class BuffJsonDamageOverTime
{
    public float damagePerSecond;
    public float tickIntervalSeconds;
}
#endif
