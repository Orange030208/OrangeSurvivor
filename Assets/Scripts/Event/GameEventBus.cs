using System;
using System.Collections.Generic;

/// <summary>
/// 全局事件总线（主线程场景）。
/// 支持：
/// 1) 全局广播：所有订阅该事件类型的监听者都会收到
/// 2) 精准派发：仅同 key 的监听者会收到（推荐用于大量实体）
///
/// 说明：
/// - 当实体数量很多时，优先使用带 key 的 Subscribe/Publish，避免“全员广播”。
/// - key 可使用 int/string/enum 等（常见：playerId、monsterId、teamId）。
/// </summary>
public static class GameEventBus
{
    // 全局监听（广播）
    private static readonly Dictionary<Type, Action> NoArgListeners = new Dictionary<Type, Action>();
    private static readonly Dictionary<Type, Delegate> PayloadListeners = new Dictionary<Type, Delegate>();

    // 分组监听（精准派发）
    private static readonly Dictionary<Type, Dictionary<object, Action>> NoArgScopedListeners =
        new Dictionary<Type, Dictionary<object, Action>>();

    private static readonly Dictionary<Type, Dictionary<object, Delegate>> PayloadScopedListeners =
        new Dictionary<Type, Dictionary<object, Delegate>>();

    #region Subscribe

    public static void Subscribe<TEvent>(Action listener) where TEvent : struct, IGameEvent
    {
        if (listener == null) return;

        Type eventType = typeof(TEvent);
        if (NoArgListeners.TryGetValue(eventType, out Action existing))
        {
            NoArgListeners[eventType] = existing + listener;
        }
        else
        {
            NoArgListeners[eventType] = listener;
        }
    }

    public static void Subscribe<TEvent>(Action<TEvent> listener) where TEvent : struct, IGameEvent
    {
        if (listener == null) return;

        Type eventType = typeof(TEvent);
        if (PayloadListeners.TryGetValue(eventType, out Delegate existing))
        {
            PayloadListeners[eventType] = (Action<TEvent>)existing + listener;
        }
        else
        {
            PayloadListeners[eventType] = listener;
        }
    }

    /// <summary>
    /// 按 key 订阅无参事件（精准派发）。
    /// </summary>
    public static void Subscribe<TEvent, TKey>(TKey key, Action listener) where TEvent : struct, IGameEvent
    {
        if (listener == null) return;
        object boxedKey = key;
        if (boxedKey == null) return;

        Type eventType = typeof(TEvent);
        if (!NoArgScopedListeners.TryGetValue(eventType, out Dictionary<object, Action> keyMap))
        {
            keyMap = new Dictionary<object, Action>();
            NoArgScopedListeners[eventType] = keyMap;
        }

        if (keyMap.TryGetValue(boxedKey, out Action existing))
        {
            keyMap[boxedKey] = existing + listener;
        }
        else
        {
            keyMap[boxedKey] = listener;
        }
    }

    /// <summary>
    /// 按 key 订阅带参数事件（精准派发）。
    /// </summary>
    public static void Subscribe<TEvent, TKey>(TKey key, Action<TEvent> listener) where TEvent : struct, IGameEvent
    {
        if (listener == null) return;
        object boxedKey = key;
        if (boxedKey == null) return;

        Type eventType = typeof(TEvent);
        if (!PayloadScopedListeners.TryGetValue(eventType, out Dictionary<object, Delegate> keyMap))
        {
            keyMap = new Dictionary<object, Delegate>();
            PayloadScopedListeners[eventType] = keyMap;
        }

        if (keyMap.TryGetValue(boxedKey, out Delegate existing))
        {
            keyMap[boxedKey] = (Action<TEvent>)existing + listener;
        }
        else
        {
            keyMap[boxedKey] = listener;
        }
    }

    #endregion

    #region Unsubscribe

    public static void Unsubscribe<TEvent>(Action listener) where TEvent : struct, IGameEvent
    {
        if (listener == null) return;

        Type eventType = typeof(TEvent);
        if (!NoArgListeners.TryGetValue(eventType, out Action existing)) return;

        existing -= listener;
        if (existing == null)
        {
            NoArgListeners.Remove(eventType);
        }
        else
        {
            NoArgListeners[eventType] = existing;
        }
    }

    public static void Unsubscribe<TEvent>(Action<TEvent> listener) where TEvent : struct, IGameEvent
    {
        if (listener == null) return;

        Type eventType = typeof(TEvent);
        if (!PayloadListeners.TryGetValue(eventType, out Delegate existing)) return;

        Action<TEvent> typed = (Action<TEvent>)existing;
        typed -= listener;

        if (typed == null)
        {
            PayloadListeners.Remove(eventType);
        }
        else
        {
            PayloadListeners[eventType] = typed;
        }
    }

    public static void Unsubscribe<TEvent, TKey>(TKey key, Action listener) where TEvent : struct, IGameEvent
    {
        if (listener == null) return;
        object boxedKey = key;
        if (boxedKey == null) return;

        Type eventType = typeof(TEvent);
        if (!NoArgScopedListeners.TryGetValue(eventType, out Dictionary<object, Action> keyMap)) return;
        if (!keyMap.TryGetValue(boxedKey, out Action existing)) return;

        existing -= listener;
        if (existing == null)
        {
            keyMap.Remove(boxedKey);
            if (keyMap.Count == 0) NoArgScopedListeners.Remove(eventType);
        }
        else
        {
            keyMap[boxedKey] = existing;
        }
    }

    public static void Unsubscribe<TEvent, TKey>(TKey key, Action<TEvent> listener) where TEvent : struct, IGameEvent
    {
        if (listener == null) return;
        object boxedKey = key;
        if (boxedKey == null) return;

        Type eventType = typeof(TEvent);
        if (!PayloadScopedListeners.TryGetValue(eventType, out Dictionary<object, Delegate> keyMap)) return;
        if (!keyMap.TryGetValue(boxedKey, out Delegate existing)) return;

        Action<TEvent> typed = (Action<TEvent>)existing;
        typed -= listener;

        if (typed == null)
        {
            keyMap.Remove(boxedKey);
            if (keyMap.Count == 0) PayloadScopedListeners.Remove(eventType);
        }
        else
        {
            keyMap[boxedKey] = typed;
        }
    }

    #endregion

    #region Publish

    /// <summary>
    /// 发布无参事件（全局广播）。
    /// </summary>
    public static void Publish<TEvent>() where TEvent : struct, IGameEvent
    {
        Type eventType = typeof(TEvent);

        if (NoArgListeners.TryGetValue(eventType, out Action noArg))
        {
            noArg.Invoke();
        }

        if (PayloadListeners.TryGetValue(eventType, out Delegate payload))
        {
            ((Action<TEvent>)payload).Invoke(default);
        }
    }

    /// <summary>
    /// 发布带参数事件（全局广播）。
    /// </summary>
    public static void Publish<TEvent>(TEvent eventData) where TEvent : struct, IGameEvent
    {
        Type eventType = typeof(TEvent);

        if (PayloadListeners.TryGetValue(eventType, out Delegate payload))
        {
            ((Action<TEvent>)payload).Invoke(eventData);
        }

        if (NoArgListeners.TryGetValue(eventType, out Action noArg))
        {
            noArg.Invoke();
        }
    }

    /// <summary>
    /// 按 key 发布无参事件（精准派发）。
    /// </summary>
    public static void Publish<TEvent, TKey>(TKey key) where TEvent : struct, IGameEvent
    {
        object boxedKey = key;
        if (boxedKey == null) return;

        Type eventType = typeof(TEvent);

        if (NoArgScopedListeners.TryGetValue(eventType, out Dictionary<object, Action> noArgKeyMap)
            && noArgKeyMap.TryGetValue(boxedKey, out Action noArg))
        {
            noArg.Invoke();
        }

        if (PayloadScopedListeners.TryGetValue(eventType, out Dictionary<object, Delegate> payloadKeyMap)
            && payloadKeyMap.TryGetValue(boxedKey, out Delegate payload))
        {
            ((Action<TEvent>)payload).Invoke(default);
        }
    }

    /// <summary>
    /// 按 key 发布带参数事件（精准派发）。
    /// </summary>
    public static void Publish<TEvent, TKey>(TKey key, TEvent eventData) where TEvent : struct, IGameEvent
    {
        object boxedKey = key;
        if (boxedKey == null) return;

        Type eventType = typeof(TEvent);

        if (PayloadScopedListeners.TryGetValue(eventType, out Dictionary<object, Delegate> payloadKeyMap)
            && payloadKeyMap.TryGetValue(boxedKey, out Delegate payload))
        {
            ((Action<TEvent>)payload).Invoke(eventData);
        }

        if (NoArgScopedListeners.TryGetValue(eventType, out Dictionary<object, Action> noArgKeyMap)
            && noArgKeyMap.TryGetValue(boxedKey, out Action noArg))
        {
            noArg.Invoke();
        }
    }

    #endregion

    public static void Clear()
    {
        NoArgListeners.Clear();
        PayloadListeners.Clear();
        NoArgScopedListeners.Clear();
        PayloadScopedListeners.Clear();
    }
}
