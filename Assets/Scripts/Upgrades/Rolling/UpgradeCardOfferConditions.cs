using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class UpgradeCardOfferConditions
{
    private const int MIN_WAVE = 1;

    [Header("波次")]
    [SerializeField] private int minWave = MIN_WAVE;

    [Header("构筑要求")]
    [SerializeField] private List<UpgradeCardTagPickRequirement> requiredTagPickCounts = new();
    [SerializeField] private List<WeaponDataSO> requiredOwnedWeapons = new();

    [Header("互斥")]
    [SerializeField] private List<string> mutuallyExclusiveCardIds = new();

    public UpgradeCardOfferConditions()
    {
    }

    public UpgradeCardOfferConditions(
        int minWave,
        IReadOnlyList<UpgradeCardTagPickRequirement> requiredTagPickCounts,
        IReadOnlyList<WeaponDataSO> requiredOwnedWeapons,
        IReadOnlyList<string> mutuallyExclusiveCardIds)
    {
        this.minWave = Mathf.Max(MIN_WAVE, minWave);
        this.requiredTagPickCounts = requiredTagPickCounts != null
            ? new List<UpgradeCardTagPickRequirement>(requiredTagPickCounts)
            : new List<UpgradeCardTagPickRequirement>();
        this.requiredOwnedWeapons = requiredOwnedWeapons != null
            ? new List<WeaponDataSO>(requiredOwnedWeapons)
            : new List<WeaponDataSO>();
        this.mutuallyExclusiveCardIds = mutuallyExclusiveCardIds != null
            ? new List<string>(mutuallyExclusiveCardIds)
            : new List<string>();
        Validate();
    }

    public int MinWave => Mathf.Max(MIN_WAVE, minWave);
    public IReadOnlyList<UpgradeCardTagPickRequirement> RequiredTagPickCounts => requiredTagPickCounts;
    public IReadOnlyList<WeaponDataSO> RequiredOwnedWeapons => requiredOwnedWeapons;
    public IReadOnlyList<string> MutuallyExclusiveCardIds => mutuallyExclusiveCardIds;

    public static UpgradeCardOfferConditions Empty()
    {
        return new UpgradeCardOfferConditions();
    }

    public bool AreSatisfied(UpgradeCardOfferContext context)
    {
        if (context == null)
        {
            return false;
        }

        if (context.WaveNumber < MinWave)
        {
            return false;
        }

        if (!AreTagRequirementsSatisfied(context.RunState))
        {
            return false;
        }

        return AreOwnedWeaponRequirementsSatisfied(context);
    }

    public bool IsMutuallyExclusiveWith(string cardId)
    {
        if (string.IsNullOrWhiteSpace(cardId) || mutuallyExclusiveCardIds == null)
        {
            return false;
        }

        for (int i = 0; i < mutuallyExclusiveCardIds.Count; i++)
        {
            if (string.Equals(mutuallyExclusiveCardIds[i], cardId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public bool HasPickedMutualExclusion(UpgradeRunState runState)
    {
        if (runState == null || mutuallyExclusiveCardIds == null)
        {
            return false;
        }

        for (int i = 0; i < mutuallyExclusiveCardIds.Count; i++)
        {
            if (runState.WasPicked(mutuallyExclusiveCardIds[i]))
            {
                return true;
            }
        }

        return false;
    }

    public void Validate()
    {
        minWave = Mathf.Max(MIN_WAVE, minWave);
        requiredTagPickCounts ??= new List<UpgradeCardTagPickRequirement>();
        requiredOwnedWeapons ??= new List<WeaponDataSO>();
        mutuallyExclusiveCardIds ??= new List<string>();

        for (int i = requiredTagPickCounts.Count - 1; i >= 0; i--)
        {
            UpgradeCardTagPickRequirement requirement = requiredTagPickCounts[i];
            requirement.Validate();
            requiredTagPickCounts[i] = requirement;
        }

        for (int i = 0; i < mutuallyExclusiveCardIds.Count; i++)
        {
            if (mutuallyExclusiveCardIds[i] != null)
            {
                mutuallyExclusiveCardIds[i] = mutuallyExclusiveCardIds[i].Trim();
            }
        }
    }

    private bool AreTagRequirementsSatisfied(UpgradeRunState runState)
    {
        if (requiredTagPickCounts == null || requiredTagPickCounts.Count == 0)
        {
            return true;
        }

        for (int i = 0; i < requiredTagPickCounts.Count; i++)
        {
            if (!requiredTagPickCounts[i].IsSatisfied(runState))
            {
                return false;
            }
        }

        return true;
    }

    private bool AreOwnedWeaponRequirementsSatisfied(UpgradeCardOfferContext context)
    {
        if (requiredOwnedWeapons == null || requiredOwnedWeapons.Count == 0)
        {
            return true;
        }

        for (int i = 0; i < requiredOwnedWeapons.Count; i++)
        {
            if (requiredOwnedWeapons[i] == null)
            {
                continue;
            }

            if (!context.HasOwnedWeapon(requiredOwnedWeapons[i]))
            {
                return false;
            }
        }

        return true;
    }
}
