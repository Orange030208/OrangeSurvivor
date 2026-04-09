using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = UIFrameworkConstants.SETTINGS_MENU_PATH, fileName = "UIFrameworkSettings")]
public sealed class UIFrameworkSettings : ScriptableObject
{
    [Header("Runtime")]
    [SerializeField] private string instanceIdPrefix = UIFrameworkConstants.DEFAULT_INSTANCE_ID_PREFIX;

    [Header("Root")]
    [SerializeField] private string rootName = "UIRoot";
    [SerializeField] private bool dontDestroyOnLoading = true;
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private RenderMode renderMode = RenderMode.ScreenSpaceOverlay;
    [SerializeField] private int rootSortingOrder;
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);
    [SerializeField] private float matchWidthOrHeight = 0.5f;

    [Header("Pooling")]
    [SerializeField] private bool enablePooling = true;
    [SerializeField] private int maxCachedInstancesPerPage = 3;

    [Header("Default Transition")]
    [SerializeField] private UIPageTransitionSettings defaultOpenTransition = new UIPageTransitionSettings();
    [SerializeField] private UIPageTransitionSettings defaultCloseTransition = new UIPageTransitionSettings
    {
        transitionType = UITransitionType.Fade,
        duration = 0.16f,
        ease = DG.Tweening.Ease.InCubic,
        offset = 60f,
        startScale = 0.98f,
        fade = true
    };

    [Header("Layers")]
    [SerializeField] private List<UILayerDefinition> layers = CreateDefaultLayers();

    public string InstanceIdPrefix => instanceIdPrefix;
    public string RootName => rootName;
    public bool DontDestroyOnLoading => dontDestroyOnLoading;
    public bool UseUnscaledTime => useUnscaledTime;
    public RenderMode RenderMode => renderMode;
    public int RootSortingOrder => rootSortingOrder;
    public Vector2 ReferenceResolution => referenceResolution;
    public float MatchWidthOrHeight => matchWidthOrHeight;
    public bool EnablePooling => enablePooling;
    public int MaxCachedInstancesPerPage => maxCachedInstancesPerPage;
    public UIPageTransitionSettings DefaultOpenTransition => defaultOpenTransition;
    public UIPageTransitionSettings DefaultCloseTransition => defaultCloseTransition;
    public IReadOnlyList<UILayerDefinition> Layers => layers;

    private static List<UILayerDefinition> CreateDefaultLayers()
    {
        return new List<UILayerDefinition>
        {
            new UILayerDefinition { layerType = UILayerType.Background, sortingOrder = UIFrameworkConstants.LAYER_BACKGROUND_SORTING_ORDER },
            new UILayerDefinition { layerType = UILayerType.SceneOverlay, sortingOrder = UIFrameworkConstants.LAYER_SCENE_OVERLAY_SORTING_ORDER },
            new UILayerDefinition { layerType = UILayerType.Default, sortingOrder = UIFrameworkConstants.LAYER_DEFAULT_SORTING_ORDER },
            new UILayerDefinition { layerType = UILayerType.Popup, sortingOrder = UIFrameworkConstants.LAYER_POPUP_SORTING_ORDER },
            new UILayerDefinition { layerType = UILayerType.System, sortingOrder = UIFrameworkConstants.LAYER_SYSTEM_SORTING_ORDER },
            new UILayerDefinition { layerType = UILayerType.Debug, sortingOrder = UIFrameworkConstants.LAYER_DEBUG_SORTING_ORDER }
        };
    }
}
