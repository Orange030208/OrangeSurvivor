using UnityEngine;

public readonly struct DamageTextViewData
{
    public float Damage { get; }
    public bool IsCritical { get; }
    public Vector3 WorldPosition { get; }

    public DamageTextViewData(float damage, bool isCritical, Vector3 worldPosition)
    {
        Damage = Mathf.Max(0f, damage);
        IsCritical = isCritical;
        WorldPosition = worldPosition;
    }
}
