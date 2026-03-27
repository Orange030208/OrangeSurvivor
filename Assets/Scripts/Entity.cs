using UnityEngine;

public abstract class Entity : MonoBehaviour,IEntity
{
    public abstract Vector2 Center { get; }
}