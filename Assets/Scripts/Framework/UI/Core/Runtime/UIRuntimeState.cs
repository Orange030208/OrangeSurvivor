using UnityEngine.Scripting.APIUpdating;

namespace AXR.Framework.UI
{
    using System;
using System.Collections.Generic;

public sealed class UIRuntimeState
{
    private readonly Dictionary<string, Type> instanceToPageType = new Dictionary<string, Type>();
    private readonly Dictionary<Type, Stack<string>> pageTypeToInstances = new Dictionary<Type, Stack<string>>();
    private readonly Stack<string> backStack = new Stack<string>();

    public IReadOnlyDictionary<string, Type> InstanceToPageType => instanceToPageType;

    public void Register(Type pageType, string instanceId, bool trackInBackStack)
    {
        if (pageType == null)
        {
            throw new ArgumentNullException(nameof(pageType), "Register failed: pageType is null.");
        }

        if (string.IsNullOrWhiteSpace(instanceId))
        {
            throw new ArgumentException("Register failed: instanceId is null or empty.", nameof(instanceId));
        }

        instanceToPageType[instanceId] = pageType;

        if (!pageTypeToInstances.TryGetValue(pageType, out Stack<string> instances))
        {
            instances = new Stack<string>();
            pageTypeToInstances.Add(pageType, instances);
        }

        instances.Push(instanceId);

        if (trackInBackStack)
        {
            backStack.Push(instanceId);
        }
    }

    public bool TryGetLastInstance(Type pageType, out string instanceId)
    {
        instanceId = string.Empty;
        if (pageType == null)
        {
            return false;
        }

        if (!pageTypeToInstances.TryGetValue(pageType, out Stack<string> instances) || instances.Count == 0)
        {
            return false;
        }

        instanceId = instances.Peek();
        return true;
    }

    public void Remove(string instanceId)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            return;
        }

        if (!instanceToPageType.TryGetValue(instanceId, out Type pageType))
        {
            return;
        }

        instanceToPageType.Remove(instanceId);

        if (pageTypeToInstances.TryGetValue(pageType, out Stack<string> instances))
        {
            var tempStack = new Stack<string>();
            while (instances.Count > 0)
            {
                string top = instances.Pop();
                if (top != instanceId)
                {
                    tempStack.Push(top);
                }
            }

            while (tempStack.Count > 0)
            {
                instances.Push(tempStack.Pop());
            }
        }
    }

    public bool TryPopTopBackStack(out string instanceId)
    {
        instanceId = string.Empty;
        if (backStack.Count == 0)
        {
            return false;
        }

        instanceId = backStack.Pop();
        return true;
    }

    public bool TryGetTopOpenInstance(out string instanceId)
    {
        instanceId = string.Empty;
        foreach (string candidate in backStack)
        {
            if (instanceToPageType.ContainsKey(candidate))
            {
                instanceId = candidate;
                return true;
            }
        }

        return false;
    }

    public int GetOpenCount(Type pageType)
    {
        if (pageType == null)
        {
            return 0;
        }

        if (!pageTypeToInstances.TryGetValue(pageType, out Stack<string> instances))
        {
            return 0;
        }

        return instances.Count;
    }

    public string[] GetBackStackSnapshot()
    {
        return backStack.ToArray();
    }

    public string[] GetOpenInstancesForPageType(Type pageType)
    {
        if (pageType == null)
        {
            return Array.Empty<string>();
        }

        if (!pageTypeToInstances.TryGetValue(pageType, out Stack<string> instances) || instances.Count == 0)
        {
            return Array.Empty<string>();
        }

        return instances.ToArray();
    }

    public void Clear()
    {
        instanceToPageType.Clear();
        pageTypeToInstances.Clear();
        backStack.Clear();
    }
}
}
