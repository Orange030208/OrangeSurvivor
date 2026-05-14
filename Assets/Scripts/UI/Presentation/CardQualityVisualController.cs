using Orange.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardQualityVisualController : ViewPartBase
{
    [Header("自动绑定")]
    [SerializeField] private bool autoResolveReferences = true;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image iconFrameImage;
    [SerializeField] private Image iconBackgroundImage;
    [SerializeField] private Image glowImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private RectTransform shadowRect;
    [SerializeField] private RectTransform glowRect;

    private bool capturedBaseVisuals;
    private Color baseBackgroundColor;
    private Color baseIconFrameColor;
    private Color baseIconBackgroundColor;
    private Color baseGlowColor;
    private Color baseTitleColor;
    private Sprite baseBackgroundSprite;
    private Sprite baseIconFrameSprite;
    private Sprite baseIconBackgroundSprite;
    private Vector3 baseShadowScale = Vector3.one;
    private Vector3 baseGlowScale = Vector3.one;

    private void Awake()
    {
        ResolveReferencesIfNeeded();
        CaptureBaseVisualsIfNeeded();
    }

    private void OnValidate()
    {
        if (autoResolveReferences)
        {
            ResolveReferencesIfNeeded();
        }
    }

    public void Apply(CardQualityPresentationProfile profile)
    {
        profile.Validate();
        ResolveReferencesIfNeeded();
        CaptureBaseVisualsIfNeeded();

        if (backgroundImage != null)
        {
            if (profile.BackgroundSprite != null)
            {
                backgroundImage.sprite = profile.BackgroundSprite;
            }

            backgroundImage.color = ResolveBackgroundColor(profile.BackgroundAlpha);
        }

        if (titleText != null)
        {
            titleText.color = profile.TitleColor;
        }

        if (iconFrameImage != null && profile.IconFrameSprite != null)
        {
            iconFrameImage.sprite = profile.IconFrameSprite;
            iconFrameImage.color = baseIconFrameColor;
        }

        if (iconBackgroundImage != null && profile.IconBackgroundSprite != null)
        {
            iconBackgroundImage.sprite = profile.IconBackgroundSprite;
            iconBackgroundImage.color = baseIconBackgroundColor;
        }

        if (glowImage != null)
        {
            glowImage.color = ResolveLayerColor(profile.MainColor, glowImage);
        }

        ApplyLayerScale(shadowRect, baseShadowScale, profile.ShadowScale);
        ApplyLayerScale(glowRect, baseGlowScale, profile.GlowScale);
    }

    public bool Apply(CardQuality quality)
    {
        CardQualityPresentationCatalogSO catalog = GameContentRuntime.TryGetProvider(out IGameContentProvider provider)
            ? provider.CardQualityPresentationCatalog
            : null;
        if (catalog == null || !catalog.TryGetProfile(quality, out CardQualityPresentationProfile profile))
        {
            return false;
        }

        Apply(profile);
        return true;
    }

    public void ResetVisuals()
    {
        ResolveReferencesIfNeeded();
        CaptureBaseVisualsIfNeeded();

        if (backgroundImage != null)
        {
            backgroundImage.sprite = baseBackgroundSprite;
            backgroundImage.color = baseBackgroundColor;
        }

        if (titleText != null)
        {
            titleText.color = baseTitleColor;
        }

        if (iconFrameImage != null)
        {
            iconFrameImage.sprite = baseIconFrameSprite;
            iconFrameImage.color = baseIconFrameColor;
        }

        if (iconBackgroundImage != null)
        {
            iconBackgroundImage.sprite = baseIconBackgroundSprite;
            iconBackgroundImage.color = baseIconBackgroundColor;
        }

        if (glowImage != null)
        {
            glowImage.color = baseGlowColor;
        }

        if (shadowRect != null)
        {
            shadowRect.localScale = baseShadowScale;
        }

        if (glowRect != null)
        {
            glowRect.localScale = baseGlowScale;
        }
    }

    private void CaptureBaseVisualsIfNeeded()
    {
        if (capturedBaseVisuals)
        {
            return;
        }

        if (backgroundImage != null)
        {
            baseBackgroundSprite = backgroundImage.sprite;
            baseBackgroundColor = backgroundImage.color;
        }

        if (iconFrameImage != null)
        {
            baseIconFrameSprite = iconFrameImage.sprite;
            baseIconFrameColor = iconFrameImage.color;
        }

        if (iconBackgroundImage != null)
        {
            baseIconBackgroundSprite = iconBackgroundImage.sprite;
            baseIconBackgroundColor = iconBackgroundImage.color;
        }

        if (glowImage != null)
        {
            baseGlowColor = glowImage.color;
        }

        if (titleText != null)
        {
            baseTitleColor = titleText.color;
        }

        if (shadowRect != null)
        {
            baseShadowScale = shadowRect.localScale;
        }

        if (glowRect != null)
        {
            baseGlowScale = glowRect.localScale;
        }

        capturedBaseVisuals = true;
    }

    private void ResolveReferencesIfNeeded()
    {
        if (!autoResolveReferences)
        {
            return;
        }

        if (backgroundImage == null)
        {
            backgroundImage = FindImageByName("Background");
        }

        if (iconFrameImage == null)
        {
            iconFrameImage = FindImageByName("IconFrame");
        }

        if (iconBackgroundImage == null)
        {
            iconBackgroundImage = FindImageByName("IconBackground");
        }

        if (glowImage == null)
        {
            glowImage = FindImageByName("Glow");
        }

        if (titleText == null)
        {
            titleText = FindTextByName("NameText");
        }

        if (shadowRect == null)
        {
            shadowRect = FindRectTransformByName("Shadow");
        }

        if (glowRect == null)
        {
            glowRect = glowImage != null
                ? glowImage.rectTransform
                : FindRectTransformByName("Glow");
        }
    }

    private Image FindImageByName(string targetName)
    {
        Transform target = FindChildByName(transform, targetName);
        return target != null ? target.GetComponent<Image>() : null;
    }

    private TMP_Text FindTextByName(string targetName)
    {
        Transform target = FindChildByName(transform, targetName);
        return target != null ? target.GetComponent<TMP_Text>() : null;
    }

    private RectTransform FindRectTransformByName(string targetName)
    {
        Transform target = FindChildByName(transform, targetName);
        return target != null ? target.GetComponent<RectTransform>() : null;
    }

    private static void ApplyLayerScale(RectTransform target, Vector3 baseScale, float scale)
    {
        if (target == null)
        {
            return;
        }

        float normalizedScale = scale > 0f ? scale : 1f;
        target.localScale = new Vector3(
            baseScale.x * normalizedScale,
            baseScale.y * normalizedScale,
            baseScale.z);
    }

    private static Color ResolveLayerColor(Color sourceColor, Component layerComponent)
    {
        Color resolvedColor = sourceColor;
        if (layerComponent != null && layerComponent.GetComponent<CanvasGroup>() != null)
        {
            // CanvasGroup 负责动态透明度时，Image 本身保持完整颜色，避免双重 alpha 让光效过暗。
            resolvedColor.a = 1f;
        }

        return resolvedColor;
    }

    private static Color ResolveBackgroundColor(float alpha)
    {
        return new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
    }

    private static Transform FindChildByName(Transform root, string targetName)
    {
        if (root == null || string.IsNullOrWhiteSpace(targetName))
        {
            return null;
        }

        if (root.name == targetName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildByName(root.GetChild(i), targetName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
