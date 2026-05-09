using System;
using UnityEngine;

/// <summary>
/// 当前场景运行时内容 Provider 的访问入口。
/// 这个静态入口只接受 Bootstrap 显式初始化，不做 Resources 路径加载。
/// </summary>
public static class GameContentRuntime
{
    private static IGameContentProvider provider;

    public static bool IsInitialized => provider != null;

    /// <summary>
    /// 获取当前 Provider；如果场景没有正确挂载 Bootstrap，会抛出可定位的错误。
    /// 只有可降级的表现逻辑才应使用 <see cref="TryGetProvider"/>。
    /// </summary>
    public static IGameContentProvider Provider
    {
        get
        {
            if (TryGetProvider(out IGameContentProvider resolvedProvider))
            {
                return resolvedProvider;
            }

            throw new InvalidOperationException(
                $"{nameof(GameContentRuntime)} has not been initialized. " +
                $"Add {nameof(GameContentBootstrap)} to the active scene and assign a {nameof(GameContentCatalogSO)}.");
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        // 兼容关闭 Domain Reload 的 Play Mode 配置，避免上一次运行的 Provider 泄漏到下一次。
        provider = null;
    }

    /// <summary>
    /// 设置当前 Provider。正常情况下只应由 <see cref="GameContentBootstrap"/> 或测试装配调用。
    /// </summary>
    public static void SetProvider(IGameContentProvider nextProvider)
    {
        provider = nextProvider ?? throw new ArgumentNullException(nameof(nextProvider));
    }

    /// <summary>
    /// 仅当调用方仍然拥有当前 Provider 时才清理，避免旧场景卸载时误清掉新场景的 Provider。
    /// </summary>
    public static void ClearProvider(IGameContentProvider expectedProvider)
    {
        if (provider == expectedProvider)
        {
            provider = null;
        }
    }

    /// <summary>
    /// 尝试解析当前 Provider。若 Awake 顺序滞后，会查找场景里的 Bootstrap 显式初始化；
    /// 这里不会退回到任何路径加载。
    /// </summary>
    public static bool TryGetProvider(out IGameContentProvider resolvedProvider)
    {
        if (provider != null)
        {
            resolvedProvider = provider;
            return true;
        }

        GameContentBootstrap bootstrap = UnityEngine.Object.FindFirstObjectByType<GameContentBootstrap>();
        if (bootstrap != null && bootstrap.TryInitializeRuntime())
        {
            resolvedProvider = provider;
            return provider != null;
        }

        resolvedProvider = null;
        return false;
    }

    public static bool TryGetPropPresentationEntry(PropType propType, out PropPresentationEntry entry)
    {
        return TryGetPropPresentationEntry(propType.ToString(), out entry);
    }

    public static bool TryGetPropPresentationEntry(string propName, out PropPresentationEntry entry)
    {
        if (TryGetProvider(out IGameContentProvider resolvedProvider) &&
            resolvedProvider.PropPresentationCatalog != null &&
            resolvedProvider.PropPresentationCatalog.TryGetEntry(propName, out entry))
        {
            return true;
        }

        entry = default;
        return false;
    }

    public static Sprite GetPropIcon(PropType propType)
    {
        return TryGetPropPresentationEntry(propType, out PropPresentationEntry entry) ? entry.Icon : null;
    }

    public static string GetPropDisplayName(PropType propType)
    {
        return TryGetPropPresentationEntry(propType, out PropPresentationEntry entry) &&
               !string.IsNullOrWhiteSpace(entry.ChineseName)
            ? entry.ChineseName
            : propType.ToString();
    }

    public static string GetPropDescription(PropType propType)
    {
        return TryGetPropPresentationEntry(propType, out PropPresentationEntry entry)
            ? entry.Description
            : string.Empty;
    }
}
