using System;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public struct CardQualityPresentationProfile
{
    [FormerlySerializedAs("rarity")]
    [SerializeField] private CardQuality quality;
    [SerializeField] private string presentationKey;
    [SerializeField] private AudioSfxKey revealSfxKey;
    [SerializeField] private AudioSfxKey selectSfxKey;
    [FormerlySerializedAs("titleColor")]
    [FormerlySerializedAs("toneColor")]
    [SerializeField] [InspectorName("主色调")] private Color mainColor;
    [SerializeField] [InspectorName("标题颜色")] private Color titleColor;
    [SerializeField] [InspectorName("背景图片")] private Sprite backgroundSprite;
    [FormerlySerializedAs("borderSprite")]
    [SerializeField] [InspectorName("Icon边框图片")] private Sprite iconFrameSprite;
    [SerializeField] [InspectorName("Icon背景图片")] private Sprite iconBackgroundSprite;
    [SerializeField] [Range(0f, 1f)] [InspectorName("背景透明度")] private float backgroundAlpha;
    [SerializeField] [Min(0f)] [InspectorName("阴影大小")] [Tooltip("以 Prefab 原始阴影大小为 1 的倍率；0 或旧资产缺省值会按 1 处理。")]
    private float shadowScale;
    [SerializeField] [Min(0f)] [InspectorName("泛光大小")] [Tooltip("以 Prefab 原始泛光大小为 1 的倍率；0 或旧资产缺省值会按 1 处理。")]
    private float glowScale;

    public CardQualityPresentationProfile(
        CardQuality quality,
        string presentationKey,
        AudioSfxKey revealSfxKey,
        AudioSfxKey selectSfxKey,
        Color mainColor,
        Sprite backgroundSprite,
        float backgroundAlpha)
        : this(
            quality,
            presentationKey,
            revealSfxKey,
            selectSfxKey,
            mainColor,
            mainColor,
            backgroundSprite,
            null,
            null,
            backgroundAlpha,
            1f,
            1f)
    {
    }

    public CardQualityPresentationProfile(
        CardQuality quality,
        string presentationKey,
        AudioSfxKey revealSfxKey,
        AudioSfxKey selectSfxKey,
        Color mainColor,
        Color titleColor,
        Sprite backgroundSprite,
        Sprite iconFrameSprite,
        Sprite iconBackgroundSprite,
        float backgroundAlpha)
        : this(
            quality,
            presentationKey,
            revealSfxKey,
            selectSfxKey,
            mainColor,
            titleColor,
            backgroundSprite,
            iconFrameSprite,
            iconBackgroundSprite,
            backgroundAlpha,
            1f,
            1f)
    {
    }

    public CardQualityPresentationProfile(
        CardQuality quality,
        string presentationKey,
        AudioSfxKey revealSfxKey,
        AudioSfxKey selectSfxKey,
        Color mainColor,
        Color titleColor,
        Sprite backgroundSprite,
        Sprite iconFrameSprite,
        Sprite iconBackgroundSprite,
        float backgroundAlpha,
        float shadowScale,
        float glowScale)
    {
        this.quality = quality;
        this.presentationKey = presentationKey;
        this.revealSfxKey = revealSfxKey;
        this.selectSfxKey = selectSfxKey;
        this.mainColor = mainColor;
        this.titleColor = titleColor;
        this.backgroundSprite = backgroundSprite;
        this.iconFrameSprite = iconFrameSprite;
        this.iconBackgroundSprite = iconBackgroundSprite;
        this.backgroundAlpha = Mathf.Clamp01(backgroundAlpha);
        this.shadowScale = NormalizeScale(shadowScale);
        this.glowScale = NormalizeScale(glowScale);
    }

    [Obsolete("Use the constructor that separates main color and title color.")]
    public CardQualityPresentationProfile(
        CardQuality quality,
        string presentationKey,
        AudioSfxKey revealSfxKey,
        AudioSfxKey selectSfxKey,
        Color toneColor,
        Sprite backgroundSprite,
        float backgroundAlpha,
        bool _)
        : this(
            quality,
            presentationKey,
            revealSfxKey,
            selectSfxKey,
            toneColor,
            backgroundSprite,
            backgroundAlpha)
    {
    }

    public CardQuality Quality => quality;
    public string PresentationKey => presentationKey;
    public AudioSfxKey RevealSfxKey => revealSfxKey;
    public AudioSfxKey SelectSfxKey => selectSfxKey;
    public Color MainColor => mainColor;
    public Color TitleColor => titleColor;
    public Sprite BackgroundSprite => backgroundSprite;
    public Sprite IconFrameSprite => iconFrameSprite;
    public Sprite IconBackgroundSprite => iconBackgroundSprite;
    public float BackgroundAlpha => backgroundAlpha;
    public float ShadowScale => shadowScale;
    public float GlowScale => glowScale;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(presentationKey))
        {
            presentationKey = quality.ToString();
        }

        if (titleColor.a <= 0f)
        {
            titleColor = mainColor;
        }

        backgroundAlpha = Mathf.Clamp01(backgroundAlpha);
        shadowScale = NormalizeScale(shadowScale);
        glowScale = NormalizeScale(glowScale);
    }

    private static float NormalizeScale(float scale)
    {
        return scale > 0f ? scale : 1f;
    }
}
