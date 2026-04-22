using System;
using UnityEngine;

public abstract class AttackConfigSO : ScriptableObject
{
    public abstract Type GetComponentType();
    public abstract void ApplyTo(AttackBase attack);
}
