using System;
using System.Collections.Generic;

namespace UniversalUI.Core.Runtime
{
    public sealed class UIRuntimeState
    {
        private readonly Dictionary<string, Type> instanceToPageType = new Dictionary<string, Type>();
        private readonly Dictionary<Type, Stack<string>> pageTypeToInstances = new Dictionary<Type, Stack<string>>();
        private readonly Stack<string> backStack = new Stack<string>();

        /// <summary>
        /// 注册新打开的 UI 页面实例，并可选择加入返回栈。
        /// </summary>
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

        /// <summary>
        /// 获取指定页面最新的有效实例。
        /// </summary>
        public bool TryGetLastInstance(Type pageType, out string instanceId)
        {
            instanceId = string.Empty;
            if (pageType == null)
            {
                return false;
            }

            if (!pageTypeToInstances.TryGetValue(pageType, out Stack<string> instances))
            {
                return false;
            }

            while (instances.Count > 0)
            {
                string top = instances.Peek();
                if (instanceToPageType.ContainsKey(top))
                {
                    instanceId = top;
                    return true;
                }

                instances.Pop();
            }

            return false;
        }

        /// <summary>
        /// 从运行态记录中移除指定实例。
        /// </summary>
        public bool Remove(string instanceId)
        {
            return instanceToPageType.Remove(instanceId);
        }

        /// <summary>
        /// 从返回栈弹出顶部有效实例。
        /// </summary>
        public bool TryPopTopBackStack(out string instanceId)
        {
            instanceId = string.Empty;

            while (backStack.Count > 0)
            {
                string candidate = backStack.Pop();
                if (instanceToPageType.ContainsKey(candidate))
                {
                    instanceId = candidate;
                    return true;
                }
            }

            return false;
        }
    }
}
