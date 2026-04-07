using System;

public struct UpgradeOptionsChangedEvent : IGameEvent
{
    public UpgradeProp[] Props;

    public UpgradeOptionsChangedEvent(UpgradeProp[] props)
    {
        Props = props;
    }
}