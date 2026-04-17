public struct RequestActiveBuffSnapshotEvent : IGameEvent
{
}

public struct ActiveBuffSnapshotChangedEvent : IGameEvent
{
    public ActiveBuffSnapshot[] Buffs;

    public ActiveBuffSnapshotChangedEvent(ActiveBuffSnapshot[] buffs)
    {
        Buffs = buffs;
    }
}

public struct BuffStackChangedEvent : IGameEvent
{
    public Entity Owner;
    public BuffDataSO BuffData;
    public int PreviousStackCount;
    public int CurrentStackCount;

    public BuffStackChangedEvent(Entity owner, BuffDataSO buffData, int previousStackCount, int currentStackCount)
    {
        Owner = owner;
        BuffData = buffData;
        PreviousStackCount = previousStackCount;
        CurrentStackCount = currentStackCount;
    }
}

public struct BuffStackRemovedEvent : IGameEvent
{
    public Entity Owner;
    public BuffDataSO BuffData;
    public int RemovedStackCount;
    public int RemainingStackCount;

    public BuffStackRemovedEvent(Entity owner, BuffDataSO buffData, int removedStackCount, int remainingStackCount)
    {
        Owner = owner;
        BuffData = buffData;
        RemovedStackCount = removedStackCount;
        RemainingStackCount = remainingStackCount;
    }
}

public struct BuffStackExpiredEvent : IGameEvent
{
    public Entity Owner;
    public BuffDataSO BuffData;
    public int ExpiredStackCount;
    public int RemainingStackCount;

    public BuffStackExpiredEvent(Entity owner, BuffDataSO buffData, int expiredStackCount, int remainingStackCount)
    {
        Owner = owner;
        BuffData = buffData;
        ExpiredStackCount = expiredStackCount;
        RemainingStackCount = remainingStackCount;
    }
}
