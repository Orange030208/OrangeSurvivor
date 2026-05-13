using System;

public readonly struct ContentHistoryScope : IEquatable<ContentHistoryScope>
{
    public ContentHistoryScope(string scopeId, string poolId = null, string ownerId = null)
    {
        ScopeId = ContentPoolScopeIds.Normalize(scopeId);
        PoolId = poolId ?? string.Empty;
        OwnerId = ownerId ?? string.Empty;
    }

    public string ScopeId { get; }
    public string PoolId { get; }
    public string OwnerId { get; }

    public bool Equals(ContentHistoryScope other)
    {
        return string.Equals(ScopeId, other.ScopeId, StringComparison.Ordinal) &&
               string.Equals(PoolId, other.PoolId, StringComparison.Ordinal) &&
               string.Equals(OwnerId, other.OwnerId, StringComparison.Ordinal);
    }

    public override bool Equals(object obj)
    {
        return obj is ContentHistoryScope other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = StringComparer.Ordinal.GetHashCode(ScopeId);
            hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(PoolId);
            hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(OwnerId);
            return hashCode;
        }
    }
}
