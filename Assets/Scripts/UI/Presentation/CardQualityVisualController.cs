using Orange.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardQualityVisualController : ViewPartBase
{
    [Header("自动绑定")]
    [SerializeField] private bool autoResolveReferences = true;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image borderImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image glowImage;
    [SerializeField] private Image shadowImage;
    [SerializeField] private TMP_Text titleText;

    [Header("强度")]
    [SerializeField] [Range(0f, 1f)] private float backgroundColorStrength = 0.58f;
    [SerializeField] [Range(0f, 1f)] private float iconTintStrength = 0.22f;

    private bool capturedBaseColors;
    private Color baseBackgroundColor;
    private Color baseBorderColor;
    private Color baseIconColor;
    private Color baseGlowColor;
    private Color baseShadowColor;
    private Color baseTitleColor;
    private Vector2 baseGlowSizeDelta;

    private void Awake()
    {
        ResolveReferencesIfNeeded();
        CaptureBaseColorsIfNeeded();
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
        ResolveReferencesIfNeeded();
        CaptureBaseColorsIfNeeded();

        if (backgroundImage != null)
        {
            backgroundImage.color = Color.Lerp(
                baseBackgroundColor,
                profile.BackgroundColor,
                backgroundColorStrength);
        }

        if (borderImage != null)
        {
            borderImage.color = profile.BorderColor;
        }

        if (iconImage != null)
        {
            iconImage.color = Color.Lerp(
                baseIconColor,
                profile.IconTintColor,
                iconTintStrength);
        }

        if (titleText != null)
        {
            titleText.color = profile.TitleColor;
        }

        if (glowImage != null)
        {
            glowImage.color = ResolveLayerColor(profile.GlowColor, glowImage);
            ApplyGlowRange(profile.GlowScaleMultiplier);
        }

        if (shadowImage != null)
        {
            shadowImage.color = ResolveLayerColor(profile.ShadowColor, shadowImage);
        }
    }

    public bool Apply(CardQuality quality)
    {
        CardQualityPresentationCatalogSO catalog = ResourcesManager.GetCardQualityPresentationCatalog();
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
        CaptureBaseColorsIfNeeded();

        if (backgroundImage != null)
        {
            backgroundImage.color = baseBackgroundColor;
        }

        if (borderImage != null)
        {
            borderImage.color = baseBorderColor;
        }

        if (iconImage != null)
        {
            iconImage.color = baseIconColor;
        }

        if (titleText != null)
        {
            titleText.color = baseTitleColor;
        }

        if (glowImage != null)
        {
            glowImage.color = baseGlowColor;
            glowImage.rectTransform.sizeDelta = baseGlowSizeDelta;
        }

        if (shadowImage != null)
        {
            shadowImage.color = baseShadowColor;
        }
    }

    private void CaptureBaseColorsIfNeeded()
    {
        if (capturedBaseColors)
        {
            return;
        }

        if (backgroundImage != null)
        {
            baseBackgroundColor = backgroundImage.color;
        }

        if (borderImage != null)
        {
            baseBorderColor = borderImage.color;
        }

        if (iconImage != null)
        {
            baseIconColor = iconImage.color;
        }

        if (glowImage != null)
        {
            baseGlowColor = glowImage.color;
            baseGlowSizeDelta = glowImage.rectTransform.sizeDelta;
        }

        if (shadowImage != null)
        {
            baseShadowColor = shadowImage.color;
        }

        if (titleText != null)
        {
            baseTitleColor = titleText.color;
        }

        capturedBaseColors = true;
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

        if (borderImage == null)
        {
            borderImage = FindImageByName("Border");
        }

        if (iconImage == null)
        {
            iconImage = FindImageByName("Item Icon");
        }

        if (glowImage == null)
        {
            glowImage = FindImageByName("Glow");
        }

        if (shadowImage == null)
        {
            shadowImage = FindImageByName("Shadow");
        }

        if (titleText == null)
        {
            titleText = FindTextByName("NameText");
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

    private void ApplyGlowRange(float scaleMultiplier)
    {
        if (glowImage == null)
        {
            return;
        }

        float resolvedMultiplier = Mathf.Max(0.1f, scaleMultiplier);
        glowImage.rectTransform.sizeDelta = baseGlowSizeDelta * resolvedMultiplier;
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
