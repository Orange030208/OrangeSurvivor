using System;
using UnityEngine;

namespace Orange.UIFramework
{
    [Serializable]
    public sealed class LayerDefinition
    {
        public const string DEFAULT_SORTING_LAYER_NAME = "Default";

        [SerializeField] private ViewLayer layer = ViewLayer.Page;
        [SerializeField] private string rootName = "PageLayer";
        [SerializeField] private bool overrideSorting = true;
        [SerializeField] private string sortingLayerName = DEFAULT_SORTING_LAYER_NAME;
        [SerializeField] private int sortingOrder;
        [SerializeField] private bool blocksRaycasts = true;

        public ViewLayer Layer => layer;
        public string RootName => string.IsNullOrWhiteSpace(rootName) ? $"{layer}Layer" : rootName;
        public bool OverrideSorting => overrideSorting;
        public string SortingLayerName => string.IsNullOrWhiteSpace(sortingLayerName)
            ? DEFAULT_SORTING_LAYER_NAME
            : sortingLayerName.Trim();
        public int SortingOrder => sortingOrder;
        public bool BlocksRaycasts => blocksRaycasts;

        public LayerDefinition()
        {
        }

        public LayerDefinition(ViewLayer layer, int sortingOrder, bool blocksRaycasts)
            : this(layer, sortingOrder, blocksRaycasts, true, DEFAULT_SORTING_LAYER_NAME)
        {
        }

        public LayerDefinition(
            ViewLayer layer,
            int sortingOrder,
            bool blocksRaycasts,
            bool overrideSorting,
            string sortingLayerName)
        {
            this.layer = layer;
            rootName = $"{layer}Layer";
            this.overrideSorting = overrideSorting;
            this.sortingLayerName = string.IsNullOrWhiteSpace(sortingLayerName)
                ? DEFAULT_SORTING_LAYER_NAME
                : sortingLayerName.Trim();
            this.sortingOrder = sortingOrder;
            this.blocksRaycasts = blocksRaycasts;
        }

        internal void Normalize()
        {
            if (string.IsNullOrWhiteSpace(rootName))
            {
                rootName = $"{layer}Layer";
            }

            sortingLayerName = SortingLayerName;
        }
    }
}
