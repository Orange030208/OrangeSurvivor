using System;
using UnityEngine;

[Serializable]
public struct UpgradeCardTagPickRequirement
{
    private const int MIN_PICK_COUNT = 1;

    [SerializeField] private UpgradeCardTag tag;
    [SerializeField] private int minPickCount;

    public UpgradeCardTagPickRequirement(UpgradeCardTag tag, int minPickCount)
    {
        this.tag = tag;
        this.minPickCount = Mathf.Max(MIN_PICK_COUNT, minPickCount);
    }

    public UpgradeCardTag Tag => tag;
    public int MinPickCount => Mathf.Max(MIN_PICK_COUNT, minPickCount);

    public bool IsSatisfied(UpgradeRunState runState)
    {
        return runState != null && runState.GetTagPickCount(tag) >= MinPickCount;
    }

    public void Validate()
    {
        minPickCount = Mathf.Max(MIN_PICK_COUNT, minPickCount);
    }
}
