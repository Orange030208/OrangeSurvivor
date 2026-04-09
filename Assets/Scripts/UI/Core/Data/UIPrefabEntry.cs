using System;
using UnityEngine;

[Serializable]
public sealed class UIPrefabEntry
{
    public UILayerType layerType = UILayerType.Default;
    public GameObject prefab;
    public bool singleton = true;
    public bool cacheOnClose = true;
    public bool trackInBackStack = true;
    public int warmupCount;
    public int maxCachedInstancesOverride = -1;

    [Header("Transition")]
    public bool useCustomTransition;
    public UIPageTransitionSettings customOpenTransition = new UIPageTransitionSettings();
    public UIPageTransitionSettings customCloseTransition = new UIPageTransitionSettings();
}
