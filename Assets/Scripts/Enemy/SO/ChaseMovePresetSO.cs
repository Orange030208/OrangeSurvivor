using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Chase Move Preset", menuName = "SO/Enemies/Movement/Chase Move Preset", order = 10)]
public sealed class ChaseMovePresetSO : MovePresetSO
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float stopDistance = 0.8f;
    public override Type GetComponentType()
    {
        throw new NotImplementedException();
    }

    public override void ApplyTo(MoveBase move, EnemySO enemy)
    {
        throw new NotImplementedException();
    }
}