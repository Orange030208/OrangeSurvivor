using System;
using UnityEngine;

namespace Orange.UIFramework
{
    [Serializable]
    public sealed class ViewDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private ViewKind kind = ViewKind.Page;
        [SerializeField] private ViewLayer layer = ViewLayer.Page;
        [SerializeField] private GameObject prefab;
        [SerializeField] private bool singleton = true;
        [SerializeField] private bool cacheOnClose = true;
        [SerializeField] private bool trackInBackStack = true;
        [SerializeField] private bool closeOnBackgroundClick;
        [Min(0)]
        [SerializeField] private int warmupCount;
        [SerializeField] private int maxCachedInstancesOverride = -1;
        [SerializeField] private bool allowDuplicateViewType;

        public string Id => id ?? string.Empty;
        public ViewKind Kind => kind;
        public ViewLayer Layer => layer;
        public GameObject Prefab => prefab;
        public bool Singleton => singleton;
        public bool CacheOnClose => cacheOnClose;
        public bool TrackInBackStack => trackInBackStack;
        public bool CloseOnBackgroundClick => closeOnBackgroundClick;
        public int WarmupCount => warmupCount;
        public int MaxCachedInstancesOverride => maxCachedInstancesOverride;
        public bool AllowDuplicateViewType => allowDuplicateViewType;

        public bool TryGetViewType(out Type viewType)
        {
            viewType = null;
            ViewBase view = prefab != null ? prefab.GetComponent<ViewBase>() : null;
            if (view == null)
            {
                return false;
            }

            viewType = view.GetType();
            return true;
        }

        internal void Normalize()
        {
            id = id?.Trim() ?? string.Empty;
            warmupCount = Mathf.Max(0, warmupCount);
            if (maxCachedInstancesOverride < -1)
            {
                maxCachedInstancesOverride = -1;
            }

            if (kind == ViewKind.Page && layer == ViewLayer.Background)
            {
                layer = ViewLayer.Page;
            }
        }
    }
}
