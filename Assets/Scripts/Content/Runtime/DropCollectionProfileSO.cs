using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class DropCollectionProfileEntry
{
    [SerializeField] private string entryId;
    [SerializeField] private CollectionSO collection;
    [SerializeField, Min(0f)] private float baseWeight = 1f;
    [SerializeField] private float luckCoefficient;

    public string EntryId => string.IsNullOrWhiteSpace(entryId)
        ? collection != null ? collection.name : string.Empty
        : entryId;
    public CollectionSO Collection => collection;
    public float BaseWeight => Mathf.Max(0f, baseWeight);
    public float LuckCoefficient => luckCoefficient;
    public bool IsValid => collection != null && BaseWeight > 0f;
}

[CreateAssetMenu(
    fileName = "Drop Collection Profile",
    menuName = ScriptableObjectMenuPaths.DROP_COLLECTION_PROFILE,
    order = 0)]
public sealed class DropCollectionProfileSO : ScriptableObject
{
    [SerializeField] private List<DropCollectionProfileEntry> entries = new();

    public IReadOnlyList<DropCollectionProfileEntry> Entries => entries;

    private void OnValidate()
    {
        entries ??= new List<DropCollectionProfileEntry>();
    }
}
