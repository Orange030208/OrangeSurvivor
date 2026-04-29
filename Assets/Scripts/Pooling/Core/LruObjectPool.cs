using System;
using System.Collections.Generic;

public sealed class LruObjectPool<T> where T : class
{
    private readonly Func<T> createItem;
    private readonly Action<T> onRent;
    private readonly Action<T, PoolReleaseReason> onReturn;
    private readonly Action<T, PoolReleaseReason> onDiscard;
    private readonly Queue<T> inactiveItems = new();
    private readonly HashSet<T> inactiveSet;
    private readonly LinkedList<T> activeOrder = new();
    private readonly Dictionary<T, LinkedListNode<T>> activeNodes;
    private readonly int maxActiveCount;
    private readonly int maxInactiveCount;
    private readonly bool recycleLeastRecentlyUsedActive;
    private readonly bool destroyOverflowInactive;

    public int ActiveCount => activeNodes.Count;
    public int InactiveCount => inactiveItems.Count;
    public int MaxActiveCount => maxActiveCount;
    public int MaxInactiveCount => maxInactiveCount;

    public LruObjectPool(
        Func<T> createItem,
        int maxActiveCount,
        int maxInactiveCount,
        bool recycleLeastRecentlyUsedActive,
        bool destroyOverflowInactive,
        IEqualityComparer<T> comparer = null,
        Action<T> onRent = null,
        Action<T, PoolReleaseReason> onReturn = null,
        Action<T, PoolReleaseReason> onDiscard = null)
    {
        this.createItem = createItem ?? throw new ArgumentNullException(nameof(createItem));
        this.maxActiveCount = Math.Max(1, maxActiveCount);
        this.maxInactiveCount = Math.Max(0, maxInactiveCount);
        this.recycleLeastRecentlyUsedActive = recycleLeastRecentlyUsedActive;
        this.destroyOverflowInactive = destroyOverflowInactive;
        this.onRent = onRent;
        this.onReturn = onReturn;
        this.onDiscard = onDiscard;

        IEqualityComparer<T> resolvedComparer = comparer ?? EqualityComparer<T>.Default;
        inactiveSet = new HashSet<T>(resolvedComparer);
        activeNodes = new Dictionary<T, LinkedListNode<T>>(resolvedComparer);
    }

    public T Rent()
    {
        T item = null;
        if (ActiveCount >= maxActiveCount)
        {
            item = TakeLeastRecentlyUsedActiveForRent();
            if (item == null)
            {
                return null;
            }
        }

        item ??= TakeInactiveOrCreate();
        if (item == null)
        {
            return null;
        }

        LinkedListNode<T> node = activeOrder.AddLast(item);
        activeNodes[item] = node;
        onRent?.Invoke(item);
        return item;
    }

    public int Preload(int targetInactiveCount)
    {
        int clampedTarget = Math.Max(0, Math.Min(targetInactiveCount, maxInactiveCount));
        int createdCount = 0;

        while (InactiveCount < clampedTarget)
        {
            T item = createItem.Invoke();
            if (item == null)
            {
                break;
            }

            if (!inactiveSet.Add(item))
            {
                continue;
            }

            onReturn?.Invoke(item, PoolReleaseReason.Prewarm);
            inactiveItems.Enqueue(item);
            createdCount++;
        }

        return createdCount;
    }

    public bool Return(T item, PoolReleaseReason reason = PoolReleaseReason.Manual)
    {
        if (item == null || !activeNodes.TryGetValue(item, out LinkedListNode<T> node))
        {
            return false;
        }

        activeOrder.Remove(node);
        activeNodes.Remove(item);
        onReturn?.Invoke(item, reason);

        if (CanStoreInactiveItem())
        {
            inactiveSet.Add(item);
            inactiveItems.Enqueue(item);
            return true;
        }

        onDiscard?.Invoke(item, PoolReleaseReason.InactiveLimit);
        return true;
    }

    public bool Touch(T item)
    {
        if (item == null || !activeNodes.TryGetValue(item, out LinkedListNode<T> node))
        {
            return false;
        }

        activeOrder.Remove(node);
        activeOrder.AddLast(node);
        return true;
    }

    public int ReturnAllActive(PoolReleaseReason reason = PoolReleaseReason.Manual)
    {
        if (activeOrder.Count == 0)
        {
            return 0;
        }

        T[] activeSnapshot = new T[activeOrder.Count];
        activeOrder.CopyTo(activeSnapshot, 0);

        int returnedCount = 0;
        for (int i = 0; i < activeSnapshot.Length; i++)
        {
            if (Return(activeSnapshot[i], reason))
            {
                returnedCount++;
            }
        }

        return returnedCount;
    }

    public int ClearInactive(PoolReleaseReason reason = PoolReleaseReason.Clear)
    {
        int clearedCount = 0;
        while (inactiveItems.Count > 0)
        {
            T item = inactiveItems.Dequeue();
            inactiveSet.Remove(item);
            if (item == null)
            {
                continue;
            }

            onDiscard?.Invoke(item, reason);
            clearedCount++;
        }

        return clearedCount;
    }

    public int ClearAll(PoolReleaseReason reason = PoolReleaseReason.Clear)
    {
        int clearedCount = ClearInactive(reason);

        while (activeOrder.First != null)
        {
            T item = activeOrder.First.Value;
            activeOrder.RemoveFirst();
            activeNodes.Remove(item);
            if (item == null)
            {
                continue;
            }

            onReturn?.Invoke(item, reason);
            onDiscard?.Invoke(item, reason);
            clearedCount++;
        }

        return clearedCount;
    }

    private T TakeLeastRecentlyUsedActiveForRent()
    {
        if (!recycleLeastRecentlyUsedActive)
        {
            return null;
        }

        LinkedListNode<T> leastRecentlyUsedNode = activeOrder.First;
        if (leastRecentlyUsedNode == null)
        {
            return null;
        }

        T item = leastRecentlyUsedNode.Value;
        activeOrder.Remove(leastRecentlyUsedNode);
        activeNodes.Remove(item);
        onReturn?.Invoke(item, PoolReleaseReason.ActiveLimit);
        return item;
    }

    private T TakeInactiveOrCreate()
    {
        while (inactiveItems.Count > 0)
        {
            T item = inactiveItems.Dequeue();
            inactiveSet.Remove(item);
            if (item != null)
            {
                return item;
            }
        }

        return createItem.Invoke();
    }

    private bool CanStoreInactiveItem()
    {
        if (maxInactiveCount <= 0)
        {
            return false;
        }

        return inactiveItems.Count < maxInactiveCount || !destroyOverflowInactive;
    }
}
