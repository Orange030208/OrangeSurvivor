/// <summary>
/// Installed feature effects can implement this to react when their owner receives applied damage.
/// FeatureHost owns the global damage event subscription and dispatches only matching owner events.
/// </summary>
public interface IDamageReceivedFeatureEffect
{
    void OnDamageReceived(HitResult result);
}
