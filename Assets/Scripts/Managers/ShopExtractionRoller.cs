using System.Collections.Generic;
using Orange.Extraction;

public sealed class ShopExtractionRoller
{
    private readonly IExtractionRandom random;
    private readonly List<ShopExtractionCandidate> candidateBuffer = new();

    public ShopExtractionRoller(IExtractionRandom random = null)
    {
        this.random = random ?? new SystemExtractionRandom();
    }

    public ShopExtractionPool CreatePool(
        IReadOnlyList<WeaponDataSO> weapons,
        IReadOnlyList<AccessoryDataSO> accessories,
        ContentTierWeightProfileSO tierWeightProfile)
    {
        BuildCandidates(weapons, accessories);
        return new ShopExtractionPool(candidateBuffer, tierWeightProfile, random);
    }

    private void BuildCandidates(
        IReadOnlyList<WeaponDataSO> weapons,
        IReadOnlyList<AccessoryDataSO> accessories)
    {
        candidateBuffer.Clear();

        if (accessories != null)
        {
            for (int i = 0; i < accessories.Count; i++)
            {
                ShopExtractionCandidate candidate = ShopExtractionCandidate.CreateAccessory(accessories[i]);
                if (candidate != null)
                {
                    candidateBuffer.Add(candidate);
                }
            }
        }

        if (weapons == null)
        {
            return;
        }

        for (int i = 0; i < weapons.Count; i++)
        {
            WeaponDataSO weapon = weapons[i];
            for (int level = WeaponLevelHelper.MinLevel; level <= WeaponLevelHelper.MaxLevel; level++)
            {
                ShopExtractionCandidate candidate = ShopExtractionCandidate.CreateWeapon(weapon, level);
                if (candidate != null)
                {
                    candidateBuffer.Add(candidate);
                }
            }
        }
    }
}
