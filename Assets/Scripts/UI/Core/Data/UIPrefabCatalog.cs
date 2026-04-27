using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = ScriptableObjectMenuPaths.UI_PREFAB_CATALOG, fileName = "UIPrefabCatalog")]
public sealed class UIPrefabCatalog : ScriptableObject
{
    [SerializeField] private List<UIPrefabEntry> entries = new List<UIPrefabEntry>();

    public IReadOnlyList<UIPrefabEntry> Entries => entries;
}
