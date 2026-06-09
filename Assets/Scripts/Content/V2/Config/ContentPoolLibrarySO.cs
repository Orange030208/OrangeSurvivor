using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "Content Pool Library",
    menuName = ScriptableObjectMenuPaths.SYSTEMS_ROOT + "Content/V2 Content Pool Library",
    order = 1)]
public sealed class ContentPoolLibrarySO : ScriptableObject
{
    [SerializeField] private ContentPoolBinding[] bindings = Array.Empty<ContentPoolBinding>();

    public bool TryGetProfile(ContentPoolKind kind, out ContentPoolProfileSO profile)
    {
        ContentPoolBinding[] items = bindings ?? Array.Empty<ContentPoolBinding>();
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].Kind == kind && items[i].Profile != null)
            {
                profile = items[i].Profile;
                return true;
            }
        }

        profile = null;
        return false;
    }
}

[Serializable]
public sealed class ContentPoolBinding
{
    [SerializeField] private ContentPoolKind kind = ContentPoolKind.Generic;
    [SerializeField] private ContentPoolProfileSO profile;

    public ContentPoolKind Kind => kind;
    public ContentPoolProfileSO Profile => profile;
}
