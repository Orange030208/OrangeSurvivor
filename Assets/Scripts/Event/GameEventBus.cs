using System;
using System.Collections.Generic;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
#endif

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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public static bool EnableDebugLog { get; set; }
    public static bool WarnWhenNoListeners { get; set; }
#endif

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
        int listenerCount = GetGlobalListenerCount(eventType);
        TracePublish(eventType, listenerCount, null);

        if (NoArgListeners.TryGetValue(eventType, out Action noArg))
        {
            InvokeListeners(noArg, eventType);
        }

        if (PayloadListeners.TryGetValue(eventType, out Delegate payload))
        {
            InvokeListeners((Action<TEvent>)payload, default, eventType);
        }
    }

    /// <summary>
    /// 发布带参数事件（全局广播）。
    /// </summary>
    public static void Publish<TEvent>(TEvent eventData) where TEvent : struct, IGameEvent
    {
        Type eventType = typeof(TEvent);
        int listenerCount = GetGlobalListenerCount(eventType);
        TracePublish(eventType, listenerCount, null);

        if (PayloadListeners.TryGetValue(eventType, out Delegate payload))
        {
            InvokeListeners((Action<TEvent>)payload, eventData, eventType);
        }

        if (NoArgListeners.TryGetValue(eventType, out Action noArg))
        {
            InvokeListeners(noArg, eventType);
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
        int listenerCount = GetScopedListenerCount(eventType, boxedKey);
        TracePublish(eventType, listenerCount, boxedKey);

        if (NoArgScopedListeners.TryGetValue(eventType, out Dictionary<object, Action> noArgKeyMap)
            && noArgKeyMap.TryGetValue(boxedKey, out Action noArg))
        {
            InvokeListeners(noArg, eventType);
        }

        if (PayloadScopedListeners.TryGetValue(eventType, out Dictionary<object, Delegate> payloadKeyMap)
            && payloadKeyMap.TryGetValue(boxedKey, out Delegate payload))
        {
            InvokeListeners((Action<TEvent>)payload, default, eventType);
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
        int listenerCount = GetScopedListenerCount(eventType, boxedKey);
        TracePublish(eventType, listenerCount, boxedKey);

        if (PayloadScopedListeners.TryGetValue(eventType, out Dictionary<object, Delegate> payloadKeyMap)
            && payloadKeyMap.TryGetValue(boxedKey, out Delegate payload))
        {
            InvokeListeners((Action<TEvent>)payload, eventData, eventType);
        }

        if (NoArgScopedListeners.TryGetValue(eventType, out Dictionary<object, Action> noArgKeyMap)
            && noArgKeyMap.TryGetValue(boxedKey, out Action noArg))
        {
            InvokeListeners(noArg, eventType);
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

    public static int GetListenerCount<TEvent>() where TEvent : struct, IGameEvent
    {
        return GetGlobalListenerCount(typeof(TEvent));
    }

    public static int GetListenerCount<TEvent, TKey>(TKey key) where TEvent : struct, IGameEvent
    {
        object boxedKey = key;
        return boxedKey != null ? GetScopedListenerCount(typeof(TEvent), boxedKey) : 0;
    }

    private static int GetGlobalListenerCount(Type eventType)
    {
        int count = 0;
        if (NoArgListeners.TryGetValue(eventType, out Action noArg))
        {
            count += noArg.GetInvocationList().Length;
        }

        if (PayloadListeners.TryGetValue(eventType, out Delegate payload))
        {
            count += payload.GetInvocationList().Length;
        }

        return count;
    }

    private static int GetScopedListenerCount(Type eventType, object key)
    {
        int count = 0;
        if (NoArgScopedListeners.TryGetValue(eventType, out Dictionary<object, Action> noArgKeyMap)
            && noArgKeyMap.TryGetValue(key, out Action noArg))
        {
            count += noArg.GetInvocationList().Length;
        }

        if (PayloadScopedListeners.TryGetValue(eventType, out Dictionary<object, Delegate> payloadKeyMap)
            && payloadKeyMap.TryGetValue(key, out Delegate payload))
        {
            count += payload.GetInvocationList().Length;
        }

        return count;
    }

    private static void InvokeListeners(Action listeners, Type eventType)
    {
        Delegate[] invocationList = listeners.GetInvocationList();
        for (int i = 0; i < invocationList.Length; i++)
        {
            try
            {
                ((Action)invocationList[i]).Invoke();
            }
            catch (Exception exception)
            {
                LogListenerException(eventType, exception);
            }
        }
    }

    private static void InvokeListeners<TEvent>(Action<TEvent> listeners, TEvent eventData, Type eventType)
        where TEvent : struct, IGameEvent
    {
        Delegate[] invocationList = listeners.GetInvocationList();
        for (int i = 0; i < invocationList.Length; i++)
        {
            try
            {
                ((Action<TEvent>)invocationList[i]).Invoke(eventData);
            }
            catch (Exception exception)
            {
                LogListenerException(eventType, exception);
            }
        }
    }

    private static void LogListenerException(Type eventType, Exception exception)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.LogError($"[GameEventBus] Listener threw while handling {eventType.Name}: {exception}");
#endif
    }

    private static void TracePublish(Type eventType, int listenerCount, object key)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (EnableDebugLog)
        {
            string scopeText = key != null ? $" key={key}" : string.Empty;
            Debug.Log($"[GameEventBus] Publish {eventType.Name}{scopeText}, listeners={listenerCount}");
        }

        if (WarnWhenNoListeners && listenerCount == 0)
        {
            string scopeText = key != null ? $" key={key}" : string.Empty;
            Debug.LogWarning($"[GameEventBus] Publish {eventType.Name}{scopeText} has no listeners.");
        }
#endif
    }
}
