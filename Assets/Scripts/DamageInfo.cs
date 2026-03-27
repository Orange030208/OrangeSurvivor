using UnityEngine;

public struct DamageInfo
{
    public int damage;
    public Vector2 position; 
    public bool isCritical;

    public DamageInfo(int damage, Vector2 position, bool isCritical)
    {
        this.damage = damage;
        this.position = position;
        this.isCritical = isCritical;
    }
}