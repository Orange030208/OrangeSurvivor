using System;
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

    public bool TryRollOne(
        IReadOnlyList<WeaponDataSO> weapons,
        IReadOnlyList<AccessoryDataSO> accessories,
        ContentTierWeightProfileSO tierWeightProfile,
        ShopExtractionContext context,
        out ShopExtractionCandidate candidate)
    {
        ShopExtractionPool pool = CreatePool(weapons, accessories, tierWeightProfile);
        if (pool.TryDrawOne(context, out ExtractionResult<ShopExtractionCandidate> result))
        {
            candidate = result.Item;
            return true;
        }

        candidate = null;
        return false;
    }

    public IReadOnlyList<ShopExtractionCandidate> DrawManyUnique(
        IReadOnlyList<WeaponDataSO> weapons,
        IReadOnlyList<AccessoryDataSO> accessories,
        ContentTierWeightProfileSO tierWeightProfile,
        ShopExtractionContext context,
        int count)
    {
        ShopExtractionPool pool = CreatePool(weapons, accessories, tierWeightProfile);
        IReadOnlyList<ExtractionResult<ShopExtractionCandidate>> results = pool.DrawManyUnique(context, count);
        if (results.Count == 0)
        {
            return Array.Empty<ShopExtractionCandidate>();
        }

        List<ShopExtractionCandidate> candidates = new(results.Count);
        for (int i = 0; i < results.Count; i++)
        {
            candidates.Add(results[i].Item);
        }

        return candidates;
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
