using System;
using System.Collections.Generic;

namespace Orange.Flows
{
    /// <summary>
    /// Typed key used to exchange run-local data between flow modules.
    /// Key identity is reference based so independently declared keys cannot collide by display name.
    /// </summary>
    public sealed class FlowKey<T>
    {
        public FlowKey(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Flow key name cannot be null or whitespace.", nameof(name));
            }

            Name = name;
        }

        public string Name { get; }

        public override string ToString()
        {
            return Name;
        }
    }

    /// <summary>
    /// Run-local state holder shared by modules in one execution. It intentionally has no global lifetime.
    /// </summary>
    public sealed class FlowBoard
    {
        private readonly Dictionary<object, object> values = new Dictionary<object, object>();
        private readonly List<IFlowModule> pendingNextModules = new List<IFlowModule>();
        private bool acceptingNextModules;

        public void Set<T>(FlowKey<T> key, T value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value), $"Flow board value for '{key.Name}' cannot be null.");
            }

            values[key] = value;
        }

        public bool TryGet<T>(FlowKey<T> key, out T value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (values.TryGetValue(key, out object rawValue) && rawValue is T typedValue)
            {
                value = typedValue;
                return true;
            }

            value = default;
            return false;
        }

        public T Require<T>(FlowKey<T> key)
        {
            if (TryGet(key, out T value))
            {
                return value;
            }

            throw new InvalidOperationException($"Flow board is missing required value '{key?.Name ?? "null"}'.");
        }

        public bool Remove<T>(FlowKey<T> key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return values.Remove(key);
        }

        /// <summary>
        /// Queues a module immediately after the currently executing module. It is valid only while a module runs.
        /// </summary>
        public void InsertNext(IFlowModule module)
        {
            if (module == null)
            {
                throw new ArgumentNullException(nameof(module));
            }

            if (!acceptingNextModules)
            {
                throw new InvalidOperationException("Flow modules can only insert a next step while they are executing.");
            }

            pendingNextModules.Add(module);
        }

        internal void BeginModuleExecution()
        {
            acceptingNextModules = true;
            pendingNextModules.Clear();
        }

        internal List<IFlowModule> EndModuleExecution()
        {
            acceptingNextModules = false;
            if (pendingNextModules.Count == 0)
            {
                return null;
            }

            List<IFlowModule> modules = new List<IFlowModule>(pendingNextModules);
            pendingNextModules.Clear();
            return modules;
        }
    }
}
