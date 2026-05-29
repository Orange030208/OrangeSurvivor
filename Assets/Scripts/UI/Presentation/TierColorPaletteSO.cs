using UnityEngine;

[CreateAssetMenu(
    fileName = "Tier Color Palette",
    menuName = ScriptableObjectMenuPaths.PRESENTATION_ROOT + "Colors/Tier Color Palette",
    order = 0)]
public sealed class TierColorPaletteSO : ScriptableObject
{
    [SerializeField] private Color commonColor = new(0.9098039f, 0.9254902f, 0.95686275f, 1f);
    [SerializeField] private Color rareColor = new(0.6039216f, 0.9098039f, 1f, 1f);
    [SerializeField] private Color epicColor = new(0.89411765f, 0.74509805f, 1f, 1f);
    [SerializeField] private Color legendaryColor = new(1f, 0.87058824f, 0.5803922f, 1f);

    public Color GetColor(ContentTier tier)
    {
        return tier switch
        {
            ContentTier.Rare => rareColor,
            ContentTier.Epic => epicColor,
            ContentTier.Legendary => legendaryColor,
            _ => commonColor
        };
    }
}
