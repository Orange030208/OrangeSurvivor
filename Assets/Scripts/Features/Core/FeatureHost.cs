using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Entity), typeof(AttributeManager))]
public class FeatureHost : EntityComponentBase,IHitModifierProvider, IEntityDamageEventReceiver
{
    private Entity owner;
    private AttributeManager AttributeManager;
    private FeatureContext featureContext;
    private readonly Dictionary<string, FeatureHostSourceHandle> installedSources = new();

    public FeatureContext Context => featureContext;

    public FeatureHostSourceHandle GetInstalledSourceHandle(string sourceId)
    {
        installedSources.TryGetValue(sourceId, out FeatureHostSourceHandle handle);
        return handle;
    }

    public void CollectRuntimeEffects<T>(List<T> results)
    {
        if (results == null)
        {
            throw new ArgumentNullException(nameof(results));
        }

        foreach (FeatureHostSourceHandle handle in installedSources.Values)
        {
            for (int i = 0; i < handle.RuntimeEffects.Count; i++)
            {
                if (handle.RuntimeEffects[i] is T effect)
                {
                    results.Add(effect);
                }
            }
        }
    }

    public override Entity Owner => owner;
    public override void Initialize(Entity owner)
    {
        this.owner = owner;
        AttributeManager = GetComponent<AttributeManager>();
        featureContext = new FeatureContext(owner, AttributeManager);
        var featureEffectsProvider = owner.GetComponent<IFeatureEffectsProvider>();
        InstallFeature(owner.RuntimeId, featureEffectsProvider.FeatureEffects);
    }

    public override void OnDisableComponent()
    {
        ClearAllSources();
    }

    public override void OnTick(float deltaTime)
    {
        if (featureContext == null || installedSources.Count == 0)
        {
            return;
        }

        foreach (FeatureHostSourceHandle handle in installedSources.Values)
        {
            for (int i = 0; i < handle.RuntimeEffects.Count; i++)
            {
                FeatureBase effect = handle.RuntimeEffects[i];
                if (effect == null)
                {
                    continue;
                }

                effect.OnUpdate(deltaTime);
            }
        }
    }

    public bool InstallFeature(string sourceId,IReadOnlyList<FeatureBase> featureEffects)
    {
        if (string.IsNullOrWhiteSpace(sourceId) || featureContext == null)
        {
            return false;
        }

        RemoveFeature(sourceId);

        var runtimeEffects = new List<FeatureBase>();
        if (featureEffects != null)
        {
            for (int i = 0; i < featureEffects.Count; i++)
            {
                FeatureBase source = featureEffects[i];
                runtimeEffects.Add(source != null ? source.CreateRuntimeCopy() : null);
            }
        }

        for (int i = 0; i < runtimeEffects.Count; i++)
        {
            FeatureBase effect = runtimeEffects[i];
            if (effect == null)
            {
                continue;
            }

            effect.Context = featureContext;
            effect.SourceId = sourceId;
            effect.OnInstall();
        }

        installedSources[sourceId] = new FeatureHostSourceHandle(sourceId, runtimeEffects);
        return true;
    }
    

    public bool RemoveFeature(string sourceId)
    {
        if (string.IsNullOrWhiteSpace(sourceId) || !installedSources.TryGetValue(sourceId, out FeatureHostSourceHandle handle))
        {
            return false;
        }

        for (int i = 0; i < handle.RuntimeEffects.Count; i++)
        {
            FeatureBase effect = handle.RuntimeEffects[i];
            if (effect == null)
            {
                continue;
            }

            effect.OnUninstall();
            effect.Context =  null;
            effect.SourceId = null;
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
            RemoveFeature(sourceIds[i]);
        }
    }

    public IEnumerable<IHitModifier> GetHitModifiers(HitModifierTiming modifierTiming)
    {
        List<IHitModifier> results = new();
        foreach (FeatureHostSourceHandle handle in installedSources.Values)
        {
            for (int i = 0; i < handle.RuntimeEffects.Count; i++)
            {
                FeatureBase effect = handle.RuntimeEffects[i];
                if (effect is not IHitModifier modifier || modifier.HitModifierTiming != modifierTiming)
                {
                    continue;
                }

                results.Add(modifier);
            }
        }
        return results;
    }

    public void OnOwnerDamageDealt(HitResult result)
    {
        if (result.Source != owner || installedSources.Count == 0)
        {
            return;
        }

        DispatchDamageDealt(result);
    }

    public void OnOwnerDamageReceived(HitResult result)
    {
        if (result.Target != owner || installedSources.Count == 0)
        {
            return;
        }

        DispatchDamageReceived(result);
    }

    private void DispatchDamageDealt(HitResult result)
    {
        foreach (FeatureHostSourceHandle handle in installedSources.Values)
        {
            for (int i = 0; i < handle.RuntimeEffects.Count; i++)
            {
                if (handle.RuntimeEffects[i] is IDamageDealtFeatureEffect effect)
                {
                    effect.OnDamageDealt(result);
                }
            }
        }
    }

    private void DispatchDamageReceived(HitResult result)
    {
        foreach (FeatureHostSourceHandle handle in installedSources.Values)
        {
            for (int i = 0; i < handle.RuntimeEffects.Count; i++)
            {
                if (handle.RuntimeEffects[i] is IDamageReceivedFeatureEffect effect)
                {
                    effect.OnDamageReceived(result);
                }
            }
        }
    }
}

[Serializable]
public sealed class FeatureHostSourceHandle
{
    public string SourceId { get; }
    public List<FeatureBase> RuntimeEffects { get; }

    public FeatureHostSourceHandle(string sourceId, List<FeatureBase> runtimeEffects)
    {
        SourceId = sourceId;
        RuntimeEffects = runtimeEffects;
    }
}
