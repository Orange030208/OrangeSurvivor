using UnityEngine;

[DisallowMultipleComponent]
public sealed class PooledObjectHandle : MonoBehaviour
{
    [SerializeField, HideInInspector] private string poolId;
    [SerializeField, HideInInspector] private GameObject sourcePrefab;

    private UnityLruPool owner;
    private bool isRented;
    private int rentVersion;

    public string PoolId => poolId;
    public GameObject SourcePrefab => sourcePrefab;
    public bool IsRented => isRented;
    public int RentVersion => rentVersion;

    internal UnityLruPool Owner => owner;

    public bool ReturnToPool()
    {
        if (owner == null)
        {
            Debug.LogWarning($"{nameof(PooledObjectHandle)} cannot return {name}: no pool owner is bound.", this);
            return false;
        }

        return owner.Return(gameObject, PoolReleaseReason.Manual);
    }

    internal void Bind(UnityLruPool owner, string poolId, GameObject sourcePrefab)
    {
        this.owner = owner;
        this.poolId = poolId;
        this.sourcePrefab = sourcePrefab;
    }

    internal void MarkRented()
    {
        isRented = true;
        rentVersion++;
    }

    internal void MarkReturned()
    {
        isRented = false;
    }
}
