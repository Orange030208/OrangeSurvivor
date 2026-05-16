#if UNITY_EDITOR
using System;
using System.Collections.Generic;

[Serializable]
public sealed class UpgradeCardJsonFile
{
    public List<UpgradeCardJsonCard> cards = new();
}

[Serializable]
public sealed class UpgradeCardJsonCard
{
    public string cardId;
    public string title;
    public string rarity;
    public List<string> tags = new();
    public string description;
    public List<UpgradeCardJsonFeature> specialFeatures = new();
}

[Serializable]
public sealed class UpgradeCardJsonFeature
{
    public string type;
    public UpgradeCardJsonModifier modifier;
}

[Serializable]
public sealed class UpgradeCardJsonModifier
{
    public string propType;
    public string modifierType;
    public float value;
}
#endif
