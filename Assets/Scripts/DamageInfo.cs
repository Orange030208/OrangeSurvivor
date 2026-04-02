using UnityEngine;

public struct DamageInfo
{
    public float damage;
    public Vector2 position; 
    public bool isCritical;

    public DamageInfo(float damage, Vector2 position, bool isCritical)
    {
        this.damage = damage;
        this.position = position;
        this.isCritical = isCritical;
    }
}