using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public abstract class ContentPoolModifier : FeatureBase, IContentPoolModifier
{
    [SerializeField] private int priority;
    [SerializeField] private string targetScopeId;
    [SerializeField] private bool affectAllScopes;

    public virtual int Priority => priority;

    public override void OnInstall()
    {
        ContentPoolModifierRegistry.Register(this);
    }

    public override void OnUninstall()
    {
        ContentPoolModifierRegistry.Unregister(this);
    }

    public virtual bool AffectsContext(ContentRollContext context)
    {
        if (affectAllScopes)
        {
            return true;
        }

        return string.Equals(
            ContentPoolScopeIds.Normalize(context?.ScopeId),
            ContentPoolScopeIds.Normalize(targetScopeId),
            StringComparison.Ordinal);
    }

    public abstract void ModifyCandidates(ContentRollContext context, List<ContentPoolCandidate> candidates);
}
