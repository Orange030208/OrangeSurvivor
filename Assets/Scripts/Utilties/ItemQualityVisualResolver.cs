using UnityEngine;

public static class ItemQualityVisualResolver
{
    private const int MIN_WEAPON_LEVEL = 1;
    private const int MAX_ACCESSORY_RARITY = 3;

    private static readonly ItemQualityVisualStyle[] DefaultWeaponStyles =
    {
        CreateDefaultStyle(1, "Common", new Color32(172, 172, 172, 255)),
        CreateDefaultStyle(2, "Rare", new Color32(77, 140, 255, 255)),
        CreateDefaultStyle(3, "Epic", new Color32(163, 104, 255, 255)),
        CreateDefaultStyle(4, "Legendary", new Color32(255, 166, 52, 255))
    };

    private static readonly ItemQualityVisualStyle[] DefaultAccessoryStyles =
    {
        CreateDefaultStyle((int)AccessoryRarity.Common, "Common", new Color32(184, 199, 209, 255)),
        CreateDefaultStyle((int)AccessoryRarity.Rare, "Rare", new Color32(64, 140, 242, 255)),
        CreateDefaultStyle((int)AccessoryRarity.Epic, "Epic", new Color32(168, 97, 230, 255)),
        CreateDefaultStyle((int)AccessoryRarity.Legendary, "Legendary", new Color32(255, 168, 46, 255))
    };

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
        SpriteRenderer spriteRenderer)
    {
        if (itemData == null)
        {
            return;
        }

        ItemQualityVisualStyle style = Resolve(itemData, qualityValue);

        if (owner == null)
        {
            return;
        }

        ItemQualitySpriteEffectController controller = owner.GetComponent<ItemQualitySpriteEffectController>();
        if (controller == null)
        {
            controller = owner.gameObject.AddComponent<ItemQualitySpriteEffectController>();
        }

        controller.Apply(style, spriteRenderer);
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
        return GameContentRuntime.TryGetProvider(out IGameContentProvider provider)
            ? provider.ItemQualityVisualConfig
            : null;
    }

    private static ItemQualityVisualStyle CreateDefaultStyle(int qualityValue, string qualityLabel, Color primaryColor)
    {
        return new ItemQualityVisualStyle(qualityValue, qualityLabel, primaryColor);
    }
}

internal static class ItemQualityShaderHelper
{
    internal static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
    internal static readonly int OutlineThicknessId = Shader.PropertyToID("_OutlineThickness");
    internal static readonly int OutlineSoftnessId = Shader.PropertyToID("_OutlineSoftness");

    internal static float GetQualityIntensity(ItemQualityVisualStyle style)
    {
        return style.QualityValue switch
        {
            <= 0 => 0.22f,
            1 => 0.34f,
            2 => 0.62f,
            3 => 0.78f,
            _ => 0.92f
        };
    }

    internal static Color BoostColor(Color color, float saturationMultiplier, float valueMultiplier)
    {
        Color.RGBToHSV(color, out float hue, out float saturation, out float value);
        saturation = Mathf.Clamp01(saturation * saturationMultiplier + 0.02f);
        value = Mathf.Clamp01(value * valueMultiplier);
        Color boostedColor = Color.HSVToRGB(hue, saturation, value);
        boostedColor.a = color.a;
        return boostedColor;
    }

    internal static Color WithAlpha(Color color, float alpha)
    {
        color.a = Mathf.Clamp01(alpha);
        return color;
    }
}

[DisallowMultipleComponent]
internal sealed class ItemQualitySpriteEffectController : MonoBehaviour
{
    public void Apply(ItemQualityVisualStyle style, SpriteRenderer spriteRenderer)
    {
        if (spriteRenderer == null || spriteRenderer.material == null)
        {
            return;
        }

        float qualityIntensity = ItemQualityShaderHelper.GetQualityIntensity(style);
        float outlineThickness = Mathf.Lerp(0.28f, 0.62f, qualityIntensity);
        float outlineSoftness = Mathf.Lerp(0.01f, 0.08f, qualityIntensity);
        Color outlineColor = ItemQualityShaderHelper.WithAlpha(
            ItemQualityShaderHelper.BoostColor(style.PrimaryColor, 1.28f, 0.94f), 1f);

        spriteRenderer.material.SetColor(ItemQualityShaderHelper.OutlineColorId, outlineColor);
        spriteRenderer.material.SetFloat(ItemQualityShaderHelper.OutlineThicknessId, outlineThickness);
        spriteRenderer.material.SetFloat(ItemQualityShaderHelper.OutlineSoftnessId, outlineSoftness);
    }
}
