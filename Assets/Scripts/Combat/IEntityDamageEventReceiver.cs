/// <summary>
/// Receives applied damage callbacks relative to the component owner's entity.
/// This keeps high-frequency combat reactions local to the source and target instead of broadcasting globally.
/// </summary>
public interface IEntityDamageEventReceiver
{
    void OnOwnerDamageDealt(HitResult result);
    void OnOwnerDamageReceived(HitResult result);
}
