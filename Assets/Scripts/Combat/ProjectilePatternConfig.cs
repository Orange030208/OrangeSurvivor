using System;
using UnityEngine;

[Serializable]
public struct ProjectilePatternConfig : IEquatable<ProjectilePatternConfig>
{
    public static ProjectilePatternConfig Default => new(3, 15f, 3, 8, 0.08f);

    [SerializeField] private int spreadCount;
    [SerializeField] private float spreadAngle;
    [SerializeField] private int burstCount;
    [SerializeField] private int novaCount;
    [SerializeField] private float burstInterval;

    public int SpreadCount => Mathf.Max(1, spreadCount);
    public float SpreadAngle => Mathf.Max(0f, spreadAngle);
    public int BurstCount => Mathf.Max(1, burstCount);
    public int NovaCount => Mathf.Max(1, novaCount);
    public float BurstInterval => Mathf.Max(0f, burstInterval);

    public ProjectilePatternConfig(int spreadCount, float spreadAngle, int burstCount, int novaCount, float burstInterval)
    {
        this.spreadCount = spreadCount;
        this.spreadAngle = spreadAngle;
        this.burstCount = burstCount;
        this.novaCount = novaCount;
        this.burstInterval = burstInterval;
    }

    public bool Equals(ProjectilePatternConfig other)
    {
        return spreadCount == other.spreadCount
            && spreadAngle.Equals(other.spreadAngle)
            && burstCount == other.burstCount
            && novaCount == other.novaCount
            && burstInterval.Equals(other.burstInterval);
    }

    public override bool Equals(object obj)
    {
        return obj is ProjectilePatternConfig other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(spreadCount, spreadAngle, burstCount, novaCount, burstInterval);
    }
}
