using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class ItemQualityVisualResolver
{
    private const string DEFAULT_RESOURCE_PATH = "Data/UI/Item Quality Visual Config";
    private const int MIN_WEAPON_LEVEL = 1;
    private const int MAX_ACCESSORY_RARITY = 3;

    private static readonly ItemQualityVisualStyle[] DefaultWeaponStyles =
    {
        CreateDefaultStyle(1, "Common", new Color32(172, 172, 172, 255), false, false),
        CreateDefaultStyle(2, "Uncommon", new Color32(86, 186, 105, 255), false, false),
        CreateDefaultStyle(3, "Rare", new Color32(77, 140, 255, 255), false, false),
        CreateDefaultStyle(4, "Epic", new Color32(163, 104, 255, 255), true, false),
        CreateDefaultStyle(5, "Legendary", new Color32(255, 166, 52, 255), true, true),
        CreateDefaultStyle(6, "Mythic", new Color32(255, 86, 86, 255), true, true)
    };

    private static readonly ItemQualityVisualStyle[] DefaultAccessoryStyles =
    {
        CreateDefaultStyle(0, "Normal", new Color32(180, 210, 255, 255), false, false),
        CreateDefaultStyle(1, "Fine", new Color32(210, 180, 255, 255), false, false),
        CreateDefaultStyle(2, "Rare", new Color32(255, 220, 180, 255), true, false),
        CreateDefaultStyle(3, "Legendary", new Color32(255, 190, 190, 255), true, true)
    };

    private static ItemQualityVisualConfigSO cachedConfig;
    private static bool configLoadAttempted;

    public static ItemQualityVisualStyle Resolve(ItemDataSO itemData, int qualityValue)
    {
        if (itemData == null)
        {
            return DefaultWeaponStyles[0];
        }

        return Resolve(itemData.ItemType, qualityValue);
    }

    public static ItemQualityVisualStyle Resolve(ItemType itemType, int qualityValue)
    {
        if (TryResolveConfiguredStyle(itemType, qualityValue, out ItemQualityVisualStyle configuredStyle))
        {
            return configuredStyle;
        }

        return ResolveDefaultStyle(itemType, qualityValue);
    }

    public static ItemQualityVisualStyle GetDefaultWeaponLevelStyle(int level)
    {
        return ResolveDefaultStyle(ItemType.Weapon, level);
    }

    public static ItemQualityVisualStyle GetDefaultAccessoryRarityStyle(int rarity)
    {
        return ResolveDefaultStyle(ItemType.Accessory, rarity);
    }

    public static void Apply(
        Component owner,
        ItemDataSO itemData,
        int qualityValue,
        Image iconImage,
        TextMeshProUGUI nameText,
        Graphic[] primaryGraphics,
        Graphic[] secondaryGraphics = null,
        Graphic[] glowGraphics = null,
        GameObject[] premiumEffectObjects = null,
        GameObject[] pulseEffectObjects = null)
    {
        if (itemData == null)
        {
            return;
        }

        ItemQualityVisualStyle style = Resolve(itemData, qualityValue).WithFallbacks();

        if (iconImage != null)
        {
            iconImage.sprite = itemData.ItemIcon;
        }

        if (nameText != null)
        {
            nameText.color = style.NameColor;
        }

        ResetGraphicColors(primaryGraphics);
        ResetGraphicColors(secondaryGraphics);
        ResetGraphicColors(glowGraphics);
        SetObjectsActive(premiumEffectObjects, false);
        SetObjectsActive(pulseEffectObjects, false);

        if (owner == null)
        {
            return;
        }

        ItemQualityRuntimeEffectController runtimeEffectController = owner.GetComponent<ItemQualityRuntimeEffectController>();
        if (runtimeEffectController == null)
        {
            runtimeEffectController = owner.gameObject.AddComponent<ItemQualityRuntimeEffectController>();
        }

        runtimeEffectController.Apply(style, iconImage, nameText);
    }

    private static bool TryResolveConfiguredStyle(ItemType itemType, int qualityValue, out ItemQualityVisualStyle style)
    {
        ItemQualityVisualConfigSO config = GetConfig();
        if (config == null)
        {
            style = default;
            return false;
        }

        switch (itemType)
        {
            case ItemType.Weapon:
                return config.TryGetWeaponLevelStyle(qualityValue, out style);
            case ItemType.Accessory:
                return config.TryGetAccessoryRarityStyle(qualityValue, out style);
            default:
                style = default;
                return false;
        }
    }

    private static ItemQualityVisualStyle ResolveDefaultStyle(ItemType itemType, int qualityValue)
    {
        switch (itemType)
        {
            case ItemType.Accessory:
                return DefaultAccessoryStyles[Mathf.Clamp(qualityValue, 0, MAX_ACCESSORY_RARITY)];
            case ItemType.Weapon:
            default:
                int weaponIndex = Mathf.Clamp(WeaponLevelHelper.ClampLevel(qualityValue) - MIN_WEAPON_LEVEL, 0, DefaultWeaponStyles.Length - 1);
                return DefaultWeaponStyles[weaponIndex];
        }
    }

    private static ItemQualityVisualConfigSO GetConfig()
    {
        if (!configLoadAttempted)
        {
            cachedConfig = Resources.Load<ItemQualityVisualConfigSO>(DEFAULT_RESOURCE_PATH);
            configLoadAttempted = true;
        }

        return cachedConfig;
    }

    private static void ResetGraphicColors(Graphic[] graphics)
    {
        if (graphics == null)
        {
            return;
        }

        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic graphic = graphics[i];
            if (graphic == null)
            {
                continue;
            }

            Color neutralColor = graphic.color;
            neutralColor.r = 1f;
            neutralColor.g = 1f;
            neutralColor.b = 1f;
            graphic.color = neutralColor;
        }
    }

    private static void SetObjectsActive(GameObject[] gameObjects, bool isActive)
    {
        if (gameObjects == null)
        {
            return;
        }

        for (int i = 0; i < gameObjects.Length; i++)
        {
            if (gameObjects[i] == null)
            {
                continue;
            }

            gameObjects[i].SetActive(isActive);
        }
    }

    private static ItemQualityVisualStyle CreateDefaultStyle(int qualityValue, string qualityLabel, Color primaryColor, bool enablePremiumEffect, bool enablePulseEffect)
    {
        Color secondaryColor = Color.Lerp(primaryColor, Color.white, 0.35f);
        Color nameColor = Color.Lerp(primaryColor, Color.white, 0.5f);
        Color glowColor = Color.Lerp(primaryColor, Color.white, 0.15f);

        return new ItemQualityVisualStyle(
            qualityValue,
            qualityLabel,
            primaryColor,
            secondaryColor,
            nameColor,
            glowColor,
            enablePremiumEffect,
            enablePulseEffect);
    }
}

[DisallowMultipleComponent]
internal sealed class ItemQualityRuntimeEffectController : MonoBehaviour
{
    private const string ICON_EFFECT_SHADER_RESOURCE_PATH = "Shaders/UI/ItemQualityIconEffect";
    private const string ITEM_ICON_EFFECT_MATERIAL_RESOURCE_PATH = "Materials/UI/WeaponQualityIconEffect";
    private const string ICON_EFFECT_SHADER_NAME = "UI/Item Quality Icon Effect";

    private static readonly int IconOutlineColorId = Shader.PropertyToID("_OutlineColor");
    private static readonly int IconOutlineThicknessId = Shader.PropertyToID("_OutlineThickness");
    private static readonly int IconOutlineSoftnessId = Shader.PropertyToID("_OutlineSoftness");

    private static readonly int TmpOutlineColorId = Shader.PropertyToID("_OutlineColor");
    private static readonly int TmpOutlineWidthId = Shader.PropertyToID("_OutlineWidth");
    private static readonly int TmpOutlineSoftnessId = Shader.PropertyToID("_OutlineSoftness");
    private static readonly int TmpGlowColorId = Shader.PropertyToID("_GlowColor");
    private static readonly int TmpGlowOffsetId = Shader.PropertyToID("_GlowOffset");
    private static readonly int TmpGlowInnerId = Shader.PropertyToID("_GlowInner");
    private static readonly int TmpGlowOuterId = Shader.PropertyToID("_GlowOuter");
    private static readonly int TmpGlowPowerId = Shader.PropertyToID("_GlowPower");

    private static Shader cachedIconEffectShader;
    private static bool iconShaderLoadAttempted;
    private static Material cachedItemIconEffectMaterial;
    private static bool itemIconMaterialLoadAttempted;

    private Image currentIconImage;
    private Material originalIconMaterial;
    private Material iconMaterialInstance;

    private TextMeshProUGUI currentNameText;
    private Material originalNameMaterial;
    private Material nameMaterialInstance;

    private Color baseNameOutlineColor;

    public void Apply(ItemQualityVisualStyle style, Image iconImage, TextMeshProUGUI nameText)
    {
        TrackTargets(iconImage, nameText);
        ConfigureIconMaterial(style, currentIconImage);
        ConfigureNameMaterial(style, currentNameText);
        enabled = false;
    }

    private void OnDestroy()
    {
        DetachIconMaterial(true);
        DetachNameMaterial(true);
    }

    private void TrackTargets(Image iconImage, TextMeshProUGUI nameText)
    {
        if (currentIconImage != iconImage)
        {
            DetachIconMaterial(true);
            currentIconImage = iconImage;
            originalIconMaterial = iconImage != null ? iconImage.material : null;
        }

        if (currentNameText != nameText)
        {
            DetachNameMaterial(true);
            currentNameText = nameText;
            originalNameMaterial = nameText != null ? nameText.fontSharedMaterial : null;
        }
    }

    private void ConfigureIconMaterial(ItemQualityVisualStyle style, Image iconImage)
    {
        if (iconImage == null || iconImage.sprite == null)
        {
            DetachIconMaterial(false);
            return;
        }

        Material itemIconBaseMaterial = GetItemIconEffectMaterial();
        Shader iconShader = GetIconEffectShader();
        if (iconShader == null)
        {
            DetachIconMaterial(false);
            return;
        }

        if (iconMaterialInstance == null
            || iconMaterialInstance.shader != iconShader)
        {
            if (iconMaterialInstance != null)
            {
                Destroy(iconMaterialInstance);
            }

            iconMaterialInstance = itemIconBaseMaterial != null
                ? new Material(itemIconBaseMaterial)
                : new Material(iconShader);
            iconMaterialInstance.name = itemIconBaseMaterial != null
                ? $"{itemIconBaseMaterial.name} (Runtime)"
                : $"{iconShader.name} (Runtime)";
        }

        float qualityIntensity = GetQualityIntensity(style);
        float outlineThickness = Mathf.Lerp(0.28f, 0.62f, qualityIntensity);
        float outlineSoftness = Mathf.Lerp(0.01f, 0.08f, qualityIntensity);
        Color outlineColor = WithAlpha(BoostColor(style.PrimaryColor, 1.28f, 0.94f), 1f);

        iconMaterialInstance.SetColor(IconOutlineColorId, outlineColor);
        iconMaterialInstance.SetFloat(IconOutlineThicknessId, outlineThickness);
        iconMaterialInstance.SetFloat(IconOutlineSoftnessId, outlineSoftness);

        iconImage.material = iconMaterialInstance;
    }

    private void ConfigureNameMaterial(ItemQualityVisualStyle style, TextMeshProUGUI nameText)
    {
        if (nameText == null)
        {
            DetachNameMaterial(false);
            return;
        }

        Material sourceMaterial = originalNameMaterial != null ? originalNameMaterial : nameText.fontSharedMaterial;
        if (sourceMaterial == null)
        {
            DetachNameMaterial(false);
            return;
        }

        if (nameMaterialInstance == null || nameMaterialInstance.shader != sourceMaterial.shader)
        {
            if (nameMaterialInstance != null)
            {
                Destroy(nameMaterialInstance);
            }

            nameMaterialInstance = new Material(sourceMaterial)
            {
                name = $"{sourceMaterial.name} (Quality Runtime)"
            };
        }

        float qualityIntensity = GetQualityIntensity(style);
        float outlineWidth = Mathf.Lerp(0.075f, 0.135f, qualityIntensity);
        float outlineSoftness = Mathf.Lerp(0f, 0.012f, qualityIntensity);

        baseNameOutlineColor = WithAlpha(BoostColor(style.PrimaryColor, 1.24f, 0.9f), 1f);

        nameMaterialInstance.EnableKeyword("OUTLINE_ON");
        nameMaterialInstance.DisableKeyword("GLOW_ON");
        nameMaterialInstance.SetColor(TmpOutlineColorId, baseNameOutlineColor);
        nameMaterialInstance.SetFloat(TmpOutlineWidthId, outlineWidth);
        nameMaterialInstance.SetFloat(TmpOutlineSoftnessId, outlineSoftness);
        nameMaterialInstance.SetColor(TmpGlowColorId, Color.clear);
        nameMaterialInstance.SetFloat(TmpGlowOffsetId, 0f);
        nameMaterialInstance.SetFloat(TmpGlowInnerId, 0f);
        nameMaterialInstance.SetFloat(TmpGlowOuterId, 0f);
        nameMaterialInstance.SetFloat(TmpGlowPowerId, 1f);

        nameText.fontSharedMaterial = nameMaterialInstance;
        nameText.UpdateMeshPadding();
        nameText.SetMaterialDirty();
    }

    private void DetachIconMaterial(bool clearTarget)
    {
        if (currentIconImage != null)
        {
            currentIconImage.material = originalIconMaterial;
        }

        if (iconMaterialInstance != null)
        {
            Destroy(iconMaterialInstance);
            iconMaterialInstance = null;
        }

        if (!clearTarget)
        {
            return;
        }

        currentIconImage = null;
        originalIconMaterial = null;
    }

    private void DetachNameMaterial(bool clearTarget)
    {
        if (currentNameText != null)
        {
            currentNameText.fontSharedMaterial = originalNameMaterial;
            currentNameText.UpdateMeshPadding();
            currentNameText.SetMaterialDirty();
        }

        if (nameMaterialInstance != null)
        {
            Destroy(nameMaterialInstance);
            nameMaterialInstance = null;
        }

        if (!clearTarget)
        {
            return;
        }

        currentNameText = null;
        originalNameMaterial = null;
    }

    private static Shader GetIconEffectShader()
    {
        if (!iconShaderLoadAttempted)
        {
            cachedIconEffectShader = Resources.Load<Shader>(ICON_EFFECT_SHADER_RESOURCE_PATH);
            if (cachedIconEffectShader == null)
            {
                cachedIconEffectShader = Shader.Find(ICON_EFFECT_SHADER_NAME);
            }

            iconShaderLoadAttempted = true;
        }

        return cachedIconEffectShader;
    }

    private static Material GetItemIconEffectMaterial()
    {
        if (!itemIconMaterialLoadAttempted)
        {
            cachedItemIconEffectMaterial = Resources.Load<Material>(ITEM_ICON_EFFECT_MATERIAL_RESOURCE_PATH);
            itemIconMaterialLoadAttempted = true;
        }

        return cachedItemIconEffectMaterial;
    }

    private static float GetQualityIntensity(ItemQualityVisualStyle style)
    {
        float qualityIntensity = style.QualityValue switch
        {
            <= 0 => 0.22f,
            1 => 0.34f,
            2 => 0.48f,
            3 => 0.62f,
            4 => 0.78f,
            _ => 0.92f
        };

        if (style.EnablePremiumEffect)
        {
            return Mathf.Max(0.78f, qualityIntensity);
        }

        return qualityIntensity;
    }

    private static Color BoostColor(Color color, float saturationMultiplier, float valueMultiplier)
    {
        Color.RGBToHSV(color, out float hue, out float saturation, out float value);
        saturation = Mathf.Clamp01(saturation * saturationMultiplier + 0.02f);
        value = Mathf.Clamp01(value * valueMultiplier);
        Color boostedColor = Color.HSVToRGB(hue, saturation, value);
        boostedColor.a = color.a;
        return boostedColor;
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = Mathf.Clamp01(alpha);
        return color;
    }
}
