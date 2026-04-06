using System.Collections.Generic;
using UnityEngine;

namespace UniversalUI.Core.Data
{
    [CreateAssetMenu(menuName = UIFrameworkConstants.CATALOG_MENU_PATH, fileName = "UIPrefabCatalog")]
    public sealed class UIPrefabCatalog : ScriptableObject
    {
        [SerializeField] private List<UIPrefabEntry> entries = new List<UIPrefabEntry>();

        public IReadOnlyList<UIPrefabEntry> Entries => entries;
    }
}
