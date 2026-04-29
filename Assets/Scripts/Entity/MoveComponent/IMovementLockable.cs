public interface IMovementLockable
{
    public bool IsMovementLocked { get; }

    public void AddMovementLock(object source);

    public void RemoveMovementLock(object source);
}
