using System;
using UnityEngine;

public abstract class AttackPresetSO : ScriptableObject
{
    [Header("Meta")]
    [SerializeField] private string attackId;
    [SerializeField] private string displayName;
    [SerializeField] private bool availableAtRuntime = true;

    [Header("Combat Policy")]
    [SerializeField] private float preferredRange = 1f;
    [SerializeField] private float minRange = 0f;
    [SerializeField] private float maxRange = 1.5f;
    [SerializeField] private bool blocksMovementWhenExecuting = true;

    public string AttackId => attackId;
    public string DisplayName => displayName;
    public bool AvailableAtRuntime => availableAtRuntime;
    public float PreferredRange => Mathf.Max(0f, preferredRange);
    public float MinRange => Mathf.Max(0f, minRange);
    public float MaxRange => Mathf.Max(MinRange, maxRange);
    public bool BlocksMovementWhenExecuting => blocksMovementWhenExecuting;

    public abstract Type GetComponentType();
    public abstract void ApplyTo(AttackBase attack, EnemySO enemy);
}
