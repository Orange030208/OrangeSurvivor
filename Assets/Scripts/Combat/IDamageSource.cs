using System;

public interface IDamageSource
{
    Entity SourceEntity { get; }
}

public interface IDamageDealtNotifier : IDamageSource
{
    event Action<HitResult> DamageDealt;
    void NotifyDamageDealt(HitResult result);
}
