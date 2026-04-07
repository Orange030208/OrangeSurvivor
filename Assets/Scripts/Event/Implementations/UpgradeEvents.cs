using System;

public struct UpgradeOptionsChangedEvent : IGameEvent
{
    public UpgradeProp[] Props;

    public UpgradeOptionsChangedEvent(UpgradeProp[] props)
    {
        Props = props;
    }
}

public struct UpgradeContainerClickedEvent : IGameEvent
{
    public int ContainerIndex;
    public PropType PropType;
    public float Value;

    public UpgradeContainerClickedEvent(int containerIndex, PropType propType, float value)
    {
        ContainerIndex = containerIndex;
        PropType = propType;
        Value = value;
    }
}