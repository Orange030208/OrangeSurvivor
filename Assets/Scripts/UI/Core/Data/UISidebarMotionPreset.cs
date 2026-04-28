using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(menuName = ScriptableObjectMenuPaths.UI_SIDEBAR_MOTION_PRESET, fileName = "UISidebarMotionPreset")]
public sealed class UISidebarMotionPreset : UIMotionPreset
{
    [Header("Layout Overrides")]
    [SerializeField] private bool overrideHiddenDirection;
    [SerializeField] private UISidebarEdgeDirection hiddenDirection = UISidebarEdgeDirection.Left;
    [SerializeField] private bool overrideExtraHideOffset;
    [SerializeField] private float extraHideOffset;

    [Header("Enter Overshoot")]
    [SerializeField] private bool useEnterOvershoot = true;
    [SerializeField] [Min(0f)] private float enterOvershootDistance = 36f;
    [SerializeField] [Range(0f, 1f)] private float enterOvershootDurationRatio = 0.78f;
    [SerializeField] private Ease enterOvershootEase = Ease.OutCubic;
    [SerializeField] private Ease enterSettleEase = Ease.OutCubic;

    public bool OverrideHiddenDirection => overrideHiddenDirection;
    public UISidebarEdgeDirection HiddenDirection => hiddenDirection;
    public bool OverrideExtraHideOffset => overrideExtraHideOffset;
    public float ExtraHideOffset => extraHideOffset;
    public bool UseEnterOvershoot => useEnterOvershoot;
    public float EnterOvershootDistance => enterOvershootDistance;
    public float EnterOvershootDurationRatio => enterOvershootDurationRatio;
    public Ease EnterOvershootEase => enterOvershootEase;
    public Ease EnterSettleEase => enterSettleEase;
}
