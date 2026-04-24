using UnityEngine;

public abstract class EntityComponentBase:MonoBehaviour
{
    public abstract Entity Owner { get; }
    
    public abstract void Initialize(Entity owner);
}