using System;
using UnityEngine;

public abstract class ConfiguredComponentBinder : MonoBehaviour
{
    protected T ResolveConfiguredComponent<T, TConfig>(TConfig config, Action<T> disableAction, Func<TConfig, Type> resolveType, Action<TConfig, T> applyConfig)
        where T : Behaviour
        where TConfig : ScriptableObject
    {
        if (config == null)
        {
            throw new InvalidOperationException($"{GetType().Name} requires a non-null {typeof(TConfig).Name}.");
        }

        DisableAll(disableAction);

        Type componentType = resolveType(config);
        if (componentType == null || !typeof(T).IsAssignableFrom(componentType))
        {
            throw new InvalidOperationException($"{typeof(TConfig).Name} {config.name} must provide a valid {typeof(T).Name} type.");
        }

        T component = GetComponent(componentType) as T ?? gameObject.AddComponent(componentType) as T;
        if (component == null)
        {
            throw new InvalidOperationException($"{GetType().Name} failed to add or resolve component {componentType.Name}.");
        }

        applyConfig(config, component);
        component.enabled = true;
        return component;
    }

    protected void DisableAll<T>(Action<T> disableAction) where T : Behaviour
    {
        T[] components = GetComponents<T>();
        for (int i = 0; i < components.Length; i++)
        {
            disableAction(components[i]);
        }
    }
}
