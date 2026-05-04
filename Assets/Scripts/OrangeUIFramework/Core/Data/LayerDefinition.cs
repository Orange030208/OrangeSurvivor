using System;
using UnityEngine;

namespace Orange.UIFramework
{
    [Serializable]
    public sealed class LayerDefinition
    {
        [SerializeField] private ViewLayer layer = ViewLayer.Page;
        [SerializeField] private string rootName = "PageLayer";
        [SerializeField] private int sortingOrder;
        [SerializeField] private bool blocksRaycasts = true;

        public ViewLayer Layer => layer;
        public string RootName => string.IsNullOrWhiteSpace(rootName) ? $"{layer}Layer" : rootName;
        public int SortingOrder => sortingOrder;
        public bool BlocksRaycasts => blocksRaycasts;

        public LayerDefinition()
        {
        }

        public LayerDefinition(ViewLayer layer, int sortingOrder, bool blocksRaycasts)
        {
            this.layer = layer;
            rootName = $"{layer}Layer";
            this.sortingOrder = sortingOrder;
            this.blocksRaycasts = blocksRaycasts;
        }

        internal void Normalize()
        {
            if (string.IsNullOrWhiteSpace(rootName))
            {
                rootName = $"{layer}Layer";
            }
        }
    }
}
