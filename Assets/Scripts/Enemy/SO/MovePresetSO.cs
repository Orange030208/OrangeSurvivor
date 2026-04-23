using System;
using UnityEngine;

public abstract class MovePresetSO : ScriptableObject
{
    [Header("Meta")]
    [SerializeField] private string moveId;
    [SerializeField] private string displayName;
    [SerializeField] private bool availableAtRuntime = true;

    [Header("Runtime Policy")]
    [SerializeField] private bool allowConcurrentAttack = true;
    [SerializeField] private bool stopOnSwitch = true;

    public string MoveId => moveId;
    public string DisplayName => displayName;
    public bool AvailableAtRuntime => availableAtRuntime;
    public bool AllowConcurrentAttack => allowConcurrentAttack;
    public bool StopOnSwitch => stopOnSwitch;

    public abstract Type GetComponentType();
    public abstract void ApplyTo(MoveBase move, EnemySO enemy);
}