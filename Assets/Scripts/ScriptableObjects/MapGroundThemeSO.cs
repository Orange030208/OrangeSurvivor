using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "Map Ground Theme", menuName = "SO/Map/Ground Theme", order = 0)]
public class MapGroundThemeSO : ScriptableObject
{
    [Header("Fallback")]
    [SerializeField] private TileBase fallbackGroundTile;
    [SerializeField] private TileBase fallbackWallTile;

    [Header("Tile Pools")]
    [SerializeField] private TileBase[] baseTiles;
    [SerializeField] private TileBase[] tuftTiles;
    [SerializeField] private TileBase[] whiteFlowerTiles;
    [SerializeField] private TileBase[] yellowFlowerTiles;

    [Header("Macro Layout")]
    [Range(0f, 1f)]
    [SerializeField] private float tuftRegionWeight = 0.4f;
    [Range(0f, 1f)]
    [SerializeField] private float whiteFlowerRegionWeight = 0.08f;
    [Range(0f, 1f)]
    [SerializeField] private float yellowFlowerRegionWeight = 0.03f;
    [SerializeField] private float macroNoiseScale = 0.055f;
    [SerializeField] private float microNoiseScale = 0.16f;

    [Header("Declutter")]
    [Range(0f, 1f)]
    [SerializeField] private float tuftBlendChance = 0.72f;
    [Range(0f, 1f)]
    [SerializeField] private float whiteFlowerThreshold = 0.82f;
    [SerializeField] private int whiteFlowerMinSpacing = 3;
    [Range(0f, 1f)]
    [SerializeField] private float yellowFlowerThreshold = 0.9f;
    [SerializeField] private int yellowFlowerMinSpacing = 4;

    public TileBase[] BaseTiles => baseTiles;
    public TileBase[] TuftTiles => tuftTiles;
    public TileBase[] WhiteFlowerTiles => whiteFlowerTiles;
    public TileBase[] YellowFlowerTiles => yellowFlowerTiles;

    public float TuftRegionWeight => tuftRegionWeight;
    public float WhiteFlowerRegionWeight => whiteFlowerRegionWeight;
    public float YellowFlowerRegionWeight => yellowFlowerRegionWeight;
    public float MacroNoiseScale => Mathf.Max(0.0001f, macroNoiseScale);
    public float MicroNoiseScale => Mathf.Max(0.0001f, microNoiseScale);
    public float TuftBlendChance => Mathf.Clamp01(tuftBlendChance);
    public float WhiteFlowerThreshold => Mathf.Clamp01(whiteFlowerThreshold);
    public int WhiteFlowerMinSpacing => Mathf.Max(0, whiteFlowerMinSpacing);
    public float YellowFlowerThreshold => Mathf.Clamp01(yellowFlowerThreshold);
    public int YellowFlowerMinSpacing => Mathf.Max(0, yellowFlowerMinSpacing);

    public bool HasGroundTiles => fallbackGroundTile != null
        || HasTiles(baseTiles)
        || HasTiles(tuftTiles)
        || HasTiles(whiteFlowerTiles)
        || HasTiles(yellowFlowerTiles);

    public bool HasTuftTiles => HasTiles(tuftTiles);
    public bool HasWhiteFlowerTiles => HasTiles(whiteFlowerTiles);
    public bool HasYellowFlowerTiles => HasTiles(yellowFlowerTiles);

    public TileBase GetGroundFallbackOrDefault(TileBase defaultTile)
    {
        if (fallbackGroundTile != null)
        {
            return fallbackGroundTile;
        }

        if (defaultTile != null)
        {
            return defaultTile;
        }

        return GetFirstTile(baseTiles)
            ?? GetFirstTile(tuftTiles)
            ?? GetFirstTile(whiteFlowerTiles)
            ?? GetFirstTile(yellowFlowerTiles);
    }

    public TileBase GetWallFallbackOrDefault(TileBase defaultTile)
    {
        return fallbackWallTile != null ? fallbackWallTile : defaultTile;
    }

    private void OnValidate()
    {
        macroNoiseScale = Mathf.Max(0.0001f, macroNoiseScale);
        microNoiseScale = Mathf.Max(0.0001f, microNoiseScale);
        whiteFlowerMinSpacing = Mathf.Max(0, whiteFlowerMinSpacing);
        yellowFlowerMinSpacing = Mathf.Max(0, yellowFlowerMinSpacing);

        float totalRegionWeight = tuftRegionWeight + whiteFlowerRegionWeight + yellowFlowerRegionWeight;
        const float maxAccentCoverage = 0.8f;
        if (totalRegionWeight > maxAccentCoverage && totalRegionWeight > 0f)
        {
            float scale = maxAccentCoverage / totalRegionWeight;
            tuftRegionWeight *= scale;
            whiteFlowerRegionWeight *= scale;
            yellowFlowerRegionWeight *= scale;
        }
    }

    private static bool HasTiles(TileBase[] tiles)
    {
        return GetFirstTile(tiles) != null;
    }

    private static TileBase GetFirstTile(TileBase[] tiles)
    {
        if (tiles == null)
        {
            return null;
        }

        for (int i = 0; i < tiles.Length; i++)
        {
            if (tiles[i] != null)
            {
                return tiles[i];
            }
        }

        return null;
    }
}
