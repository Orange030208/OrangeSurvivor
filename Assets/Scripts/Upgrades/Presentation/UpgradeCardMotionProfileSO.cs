using UnityEngine;

[CreateAssetMenu(
    fileName = "Upgrade Card Motion Profile",
    menuName = ScriptableObjectMenuPaths.UPGRADE_CARD_MOTION_PROFILE,
    order = 0)]
public sealed class UpgradeCardMotionProfileSO : ScriptableObject
{
    [Header("UIMotion片段")]
    [SerializeField] private string revealClipId;
    [SerializeField] private string hoverInClipId;
    [SerializeField] private string hoverOutClipId;
    [SerializeField] private string pressClipId;
    [SerializeField] private string releaseClipId;
    [SerializeField] private string selectClipId;
    [Tooltip("可选。为空时中断复位只恢复控制器记录的根节点Transform，不调用UIMotion采样，避免没有Visible/Show片段的资产刷警告。")]
    [SerializeField] private string restClipId = string.Empty;

    [Header("复用与中断")]
    [Tooltip("配置新卡牌内容时刷新UIMotion默认快照，避免对象复用后保留上一张卡的交互状态。")]
    [SerializeField] private bool refreshDefaultsOnConfigure;

    [Tooltip("对象隐藏或点击流程被打断时，采样到稳定可见状态后再刷新默认快照。")]
    [SerializeField] private bool resetToRestClipWhenInterrupted;

    [Tooltip("卡牌配置新内容后是否播放Show片段。关闭后只刷新状态并恢复运行时浮动。")]
    [SerializeField] private bool playRevealOnConfigure;

    [Header("运行时卡牌动态")]
    [SerializeField] private bool enableIdleFloat;
    [SerializeField] [Min(0f)] private float idleFloatAmplitude;
    [SerializeField] [Min(0.05f)] private float idleFloatDuration;

    [SerializeField] private bool enablePointerTilt;
    [SerializeField] [Range(0f, 18f)] private float hoverTiltAngle;
    [SerializeField] [Min(0.01f)] private float hoverTiltDuration;
    [SerializeField] [Min(0.01f)] private float hoverReturnDuration;

    [Header("视觉层动态")]
    [SerializeField] private bool enableVisualLayerDynamics;
    [SerializeField] [Range(0f, 1f)] private float glowIdleAlpha;
    [SerializeField] [Range(0f, 1f)] private float glowHoverAlpha;
    [SerializeField] [Range(0f, 1f)] private float glowPressAlpha;
    [SerializeField] [Range(0f, 1f)] private float glowSelectAlpha;
    [SerializeField] [Range(0f, 1f)] private float shadowIdleAlpha;
    [SerializeField] [Range(0f, 1f)] private float shadowHoverAlpha;
    [SerializeField] [Range(0f, 1f)] private float shadowPressAlpha;
    [SerializeField] private Vector2 shadowHoverOffset;
    [SerializeField] private Vector2 shadowPressOffset;
    [SerializeField] [Min(0.01f)] private float visualLayerTweenDuration;

    public string RevealClipId => NormalizeClipId(revealClipId);
    public string HoverInClipId => NormalizeClipId(hoverInClipId);
    public string HoverOutClipId => NormalizeClipId(hoverOutClipId);
    public string PressClipId => NormalizeClipId(pressClipId);
    public string ReleaseClipId => NormalizeClipId(releaseClipId);
    public string SelectClipId => NormalizeClipId(selectClipId);
    public string RestClipId => NormalizeClipId(restClipId);
    public bool RefreshDefaultsOnConfigure => refreshDefaultsOnConfigure;
    public bool ResetToRestClipWhenInterrupted => resetToRestClipWhenInterrupted;
    public bool PlayRevealOnConfigure => playRevealOnConfigure;
    public bool EnableIdleFloat => enableIdleFloat;
    public float IdleFloatAmplitude => Mathf.Max(0f, idleFloatAmplitude);
    public float IdleFloatDuration => Mathf.Max(0.05f, idleFloatDuration);
    public bool EnablePointerTilt => enablePointerTilt;
    public float HoverTiltAngle => Mathf.Max(0f, hoverTiltAngle);
    public float HoverTiltDuration => Mathf.Max(0.01f, hoverTiltDuration);
    public float HoverReturnDuration => Mathf.Max(0.01f, hoverReturnDuration);
    public bool EnableVisualLayerDynamics => enableVisualLayerDynamics;
    public float GlowIdleAlpha => Mathf.Clamp01(glowIdleAlpha);
    public float GlowHoverAlpha => Mathf.Clamp01(glowHoverAlpha);
    public float GlowPressAlpha => Mathf.Clamp01(glowPressAlpha);
    public float GlowSelectAlpha => Mathf.Clamp01(glowSelectAlpha);
    public float ShadowIdleAlpha => Mathf.Clamp01(shadowIdleAlpha);
    public float ShadowHoverAlpha => Mathf.Clamp01(shadowHoverAlpha);
    public float ShadowPressAlpha => Mathf.Clamp01(shadowPressAlpha);
    public Vector2 ShadowHoverOffset => shadowHoverOffset;
    public Vector2 ShadowPressOffset => shadowPressOffset;
    public float VisualLayerTweenDuration => Mathf.Max(0.01f, visualLayerTweenDuration);

    private static string NormalizeClipId(string clipId)
    {
        return string.IsNullOrWhiteSpace(clipId) ? string.Empty : clipId.Trim();
    }
}
