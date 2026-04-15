using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Entity), typeof(PropertiesManager))]
public class FeatureHost : MonoBehaviour
{
    private Entity ownerEntity;
    private PropertiesManager propertiesManager;
    private FeatureContext featureContext;
    private readonly Dictionary<string, FeatureHostSourceHandle> installedSources = new();

    public Entity OwnerEntity => ownerEntity;
    public FeatureContext Context => featureContext;

    private void Awake()
    {
        ownerEntity = GetComponent<Entity>();
        propertiesManager = GetComponent<PropertiesManager>();
        featureContext = new FeatureContext(ownerEntity, propertiesManager);
    }

    private void OnDisable()
    {
        ClearAllSources();
    }

    private void Update()
    {
        if (!GameSimulation.IsRunning)
        {
            return;
        }

        if (featureContext == null || installedSources.Count == 0)
        {
            return;
        }

        float deltaTime = Time.deltaTime;
        foreach (FeatureHostSourceHandle handle in installedSources.Values)
        {
            for (int i = 0; i < handle.RuntimeEffects.Count; i++)
            {
                FeatureEffectBase effect = handle.RuntimeEffects[i];
                if (effect == null)
                {
                    continue;
                }

                effect.OnUpdate(featureContext, deltaTime);
            }
        }
    }

    public bool InstallSource(string sourceId, IRuntimeFeatureSource source)
    {
        if (string.IsNullOrWhiteSpace(sourceId) || source == null || featureContext == null || ownerEntity == null)
        {
            return false;
        }

        RemoveSource(sourceId);

        var runtimeEffects = new List<FeatureEffectBase>(source.CreateRuntimeFeatureEffects(sourceId));
        for (int i = 0; i < runtimeEffects.Count; i++)
        {
            FeatureEffectBase effect = runtimeEffects[i];
            if (effect == null)
            {
                continue;
            }

            effect.OnInstall(featureContext);
        }

        installedSources[sourceId] = new FeatureHostSourceHandle(sourceId, runtimeEffects);
        return true;
    }

    public bool RemoveSource(string sourceId)
    {
        if (string.IsNullOrWhiteSpace(sourceId) || !installedSources.TryGetValue(sourceId, out FeatureHostSourceHandle handle))
        {
            return false;
        }

        for (int i = 0; i < handle.RuntimeEffects.Count; i++)
        {
            FeatureEffectBase effect = handle.RuntimeEffects[i];
            if (effect == null)
            {
                continue;
            }

            effect.OnUninstall(featureContext);
        }

        installedSources.Remove(sourceId);
        return true;
    }

    public void ClearAllSources()
    {
        string[] sourceIds = new string[installedSources.Count];
        installedSources.Keys.CopyTo(sourceIds, 0);
        for (int i = 0; i < sourceIds.Length; i++)
        {
            RemoveSource(sourceIds[i]);
        }
    }
}

[Serializable]
public sealed class FeatureHostSourceHandle
{
    public string SourceId { get; }
    public List<FeatureEffectBase> RuntimeEffects { get; }

    public FeatureHostSourceHandle(string sourceId, List<FeatureEffectBase> runtimeEffects)
    {
        SourceId = sourceId;
        RuntimeEffects = runtimeEffects;
    }
}
