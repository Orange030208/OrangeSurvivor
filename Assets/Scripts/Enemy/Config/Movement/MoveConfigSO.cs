using System;
using UnityEngine;

public abstract class MoveConfigSO : ScriptableObject
{
    public abstract Type GetComponentType();
    public abstract void ApplyTo(MoveBase move, float moveSpeed, float attackDetectionRadius);
}
