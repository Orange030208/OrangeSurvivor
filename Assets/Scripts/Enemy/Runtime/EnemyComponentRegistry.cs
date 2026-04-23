using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyComponentRegistry : MonoBehaviour
{
    private readonly Dictionary<Type, MoveBase> movesByType = new();
    private readonly Dictionary<Type, AttackBase> attacksByType = new();

    public void Rebuild()
    {
        movesByType.Clear();
        attacksByType.Clear();

        MoveBase[] moves = GetComponents<MoveBase>();
        for (int i = 0; i < moves.Length; i++)
        {
            MoveBase move = moves[i];
            if (move == null)
            {
                continue;
            }

            movesByType[move.GetType()] = move;
        }

        AttackBase[] attacks = GetComponents<AttackBase>();
        for (int i = 0; i < attacks.Length; i++)
        {
            AttackBase attack = attacks[i];
            if (attack == null)
            {
                continue;
            }

            attacksByType[attack.GetType()] = attack;
        }
    }

    public MoveBase GetOrAddMove(Type componentType)
    {
        if (componentType == null || !typeof(MoveBase).IsAssignableFrom(componentType))
        {
            throw new ArgumentException($"{nameof(componentType)} must be a {nameof(MoveBase)} type.", nameof(componentType));
        }

        if (movesByType.TryGetValue(componentType, out MoveBase cachedMove) && cachedMove != null)
        {
            return cachedMove;
        }

        MoveBase move = GetComponent(componentType) as MoveBase ?? gameObject.AddComponent(componentType) as MoveBase;
        if (move == null)
        {
            throw new InvalidOperationException($"Failed to add or resolve move component {componentType.Name}.");
        }

        movesByType[componentType] = move;
        return move;
    }

    public AttackBase GetOrAddAttack(Type componentType)
    {
        if (componentType == null || !typeof(AttackBase).IsAssignableFrom(componentType))
        {
            throw new ArgumentException($"{nameof(componentType)} must be a {nameof(AttackBase)} type.", nameof(componentType));
        }

        if (attacksByType.TryGetValue(componentType, out AttackBase cachedAttack) && cachedAttack != null)
        {
            return cachedAttack;
        }

        AttackBase attack = GetComponent(componentType) as AttackBase ?? gameObject.AddComponent(componentType) as AttackBase;
        if (attack == null)
        {
            throw new InvalidOperationException($"Failed to add or resolve attack component {componentType.Name}.");
        }

        attacksByType[componentType] = attack;
        return attack;
    }

    public MoveBase GetMove(Type componentType)
    {
        if (componentType == null)
        {
            return null;
        }

        movesByType.TryGetValue(componentType, out MoveBase move);
        return move;
    }

    public AttackBase GetAttack(Type componentType)
    {
        if (componentType == null)
        {
            return null;
        }

        attacksByType.TryGetValue(componentType, out AttackBase attack);
        return attack;
    }
}
