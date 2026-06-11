using System;
using System.Collections.Generic;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
#endif

/// <summary>
/// 全局事件总线（主线程场景）。
/// 当前仅支持按事件类型的全局广播。
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

    #endregion

    #region Publish

    /// <summary>
    /// 发布无参事件（全局广播）。
    /// </summary>
    public static void Publish<TEvent>() where TEvent : struct, IGameEvent
    {
        Type eventType = typeof(TEvent);
        int listenerCount = GetGlobalListenerCount(eventType);
        TracePublish(eventType, listenerCount);

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
        TracePublish(eventType, listenerCount);

        if (PayloadListeners.TryGetValue(eventType, out Delegate payload))
        {
            InvokeListeners((Action<TEvent>)payload, eventData, eventType);
        }

        if (NoArgListeners.TryGetValue(eventType, out Action noArg))
        {
            InvokeListeners(noArg, eventType);
        }
    }

    #endregion

    public static void Clear()
    {
        NoArgListeners.Clear();
        PayloadListeners.Clear();
    }

    public static int GetListenerCount<TEvent>() where TEvent : struct, IGameEvent
    {
        return GetGlobalListenerCount(typeof(TEvent));
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

    private static void TracePublish(Type eventType, int listenerCount)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (EnableDebugLog)
        {
            Debug.Log($"[GameEventBus] Publish {eventType.Name}, listeners={listenerCount}");
        }

        if (WarnWhenNoListeners && listenerCount == 0)
        {
            Debug.LogWarning($"[GameEventBus] Publish {eventType.Name} has no listeners.");
        }
#endif
    }
}
