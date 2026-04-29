public interface IPoolable
{
    void OnRentFromPool();
    void OnReturnToPool();
    void OnDiscardFromPool();
}
