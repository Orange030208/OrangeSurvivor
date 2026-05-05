
namespace Orange.UIFramework
{
    using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
// Inspector 中的一条目标绑定。key 是 Track 使用的稳定名称，target 是实际场景/Prefab Transform。
public sealed class UIMotionTargetBinding
{
    [SerializeField] private string key = UIMotionTargetKeys.SELF;
    [SerializeField] private Transform target;

    public string Key => key;
    public Transform Target => target;
}

[Serializable]
// 负责把 Track 的 targetKey 解析成实际组件，并维护 Initial 状态快照。
// 这样动画配置可以引用“Icon”“Label”等语义目标，而不直接依赖层级路径。
public sealed class UIMotionTargetRegistry
{
    [SerializeField] private List<UIMotionTargetBinding> bindings = new();

    private readonly Dictionary<string, Transform> targetMap = new(StringComparer.Ordinal);
    private readonly Dictionary<string, UIMotionTargetSnapshot> snapshotMap = new(StringComparer.Ordinal);
    private Transform owner;
    private bool initialized;

    public void Initialize(Transform ownerTransform)
    {
        owner = ownerTransform;
        // 初始化时同时记录 SELF 与自定义绑定，保证未显式配置 targetKey 的 Track 可以直接作用于宿主。
        RebuildTargetMap();
        RefreshSnapshots();
        initialized = true;
    }

    public void RefreshSnapshots()
    {
        EnsureTargetMap();
        snapshotMap.Clear();
        // 快照是 Initial 系列取值的基准。刷新后，后续播放会以当前 UI 状态作为新的默认值。
        foreach (KeyValuePair<string, Transform> pair in targetMap)
        {
            if (pair.Value == null || snapshotMap.ContainsKey(pair.Key))
            {
                continue;
            }

            snapshotMap.Add(pair.Key, new UIMotionTargetSnapshot(pair.Value));
        }
    }

    public bool TryGetTarget(string key, out Transform target)
    {
        EnsureTargetMap();
        string resolvedKey = ResolveKey(key);
        return targetMap.TryGetValue(resolvedKey, out target) && target != null;
    }

    public bool TryGetSnapshot(string key, out UIMotionTargetSnapshot snapshot)
    {
        EnsureTargetMap();
        string resolvedKey = ResolveKey(key);
        if (snapshotMap.TryGetValue(resolvedKey, out snapshot) && snapshot != null)
        {
            return true;
        }

        if (!targetMap.TryGetValue(resolvedKey, out Transform target) || target == null)
        {
            snapshot = null;
            return false;
        }

        // 目标新增或首次访问时懒创建快照，兼容运行时才填充的绑定。
        snapshot = new UIMotionTargetSnapshot(target);
        snapshotMap[resolvedKey] = snapshot;
        return true;
    }

    public bool TryGetComponent<TComponent>(string key, out TComponent component)
        where TComponent : Component
    {
        component = null;
        if (!TryGetTarget(key, out Transform target))
        {
            return false;
        }

        component = target.GetComponent<TComponent>();
        return component != null;
    }

    public bool TryGetRectTransform(string key, out RectTransform rectTransform)
    {
        rectTransform = null;
        if (!TryGetTarget(key, out Transform target))
        {
            return false;
        }

        rectTransform = target as RectTransform;
        return rectTransform != null;
    }

    public bool TryGetCanvasGroup(string key, out CanvasGroup canvasGroup)
    {
        return TryGetComponent(key, out canvasGroup);
    }

    public bool TryGetGraphic(string key, out Graphic graphic)
    {
        return TryGetComponent(key, out graphic);
    }

    public bool TryGetImage(string key, out Image image)
    {
        return TryGetComponent(key, out image);
    }

    public bool TryGetText(string key, out TMP_Text text)
    {
        return TryGetComponent(key, out text);
    }

    private void RebuildTargetMap()
    {
        targetMap.Clear();
        if (owner != null)
        {
            // SELF 始终指向 UIMotionPlayer 所在对象，是最常用、也最稳定的默认目标。
            targetMap[UIMotionTargetKeys.SELF] = owner;
        }

        if (bindings == null)
        {
            return;
        }

        for (int i = 0; i < bindings.Count; i++)
        {
            UIMotionTargetBinding binding = bindings[i];
            if (binding == null || binding.Target == null)
            {
                continue;
            }

            string key = ResolveKey(binding.Key);
            // 后配置的同名 key 覆盖前者，便于 Prefab 变体在 Inspector 中重定向目标。
            targetMap[key] = binding.Target;
        }
    }

    private void EnsureTargetMap()
    {
        if (initialized)
        {
            return;
        }

        RebuildTargetMap();
        initialized = true;
    }

    private static string ResolveKey(string key)
    {
        return string.IsNullOrWhiteSpace(key) ? UIMotionTargetKeys.SELF : key.Trim();
    }
}
}
