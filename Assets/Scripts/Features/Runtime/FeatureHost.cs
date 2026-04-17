using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Entity), typeof(PropertiesManager))]
public class FeatureHost : MonoBehaviour,IHitModifierProvider
{
    private Entity ownerEntity;
    private PropertiesManager propertiesManager;
    private FeatureContext featureContext;
    private readonly Dictionary<string, FeatureHostSourceHandle> installedSources = new();

    public FeatureContext Context => featureContext;

    public FeatureHostSourceHandle GetInstalledSourceHandle(string sourceId)
    {
        installedSources.TryGetValue(sourceId, out FeatureHostSourceHandle handle);
        return handle;
    }

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

                effect.OnUpdate(deltaTime);
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

            effect.Context = featureContext;
            effect.OnInstall();
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

            effect.OnUninstall();
            effect.Context =  null;
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

    public IEnumerable<IHitModifier> GetHitModifiers(HitModifierTiming modifierTiming)
    {
        List<IHitModifier> results = new List<IHitModifier>();
        foreach (FeatureHostSourceHandle handle in installedSources.Values)
        {
            for (int i = 0; i < handle.RuntimeEffects.Count; i++)
            {
                FeatureEffectBase effect = handle.RuntimeEffects[i];
                if (effect == null || !effect.CanModifyHit || effect.HitModifierTiming != modifierTiming)
                {
                    continue;
                }

                results.Add(effect);
            }
        }
        return results;
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
