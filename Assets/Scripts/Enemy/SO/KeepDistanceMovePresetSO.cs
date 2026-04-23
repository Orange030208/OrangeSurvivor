using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Keep Distance Move Preset", menuName = "SO/Enemies/Movement/Keep Distance Move Preset", order = 11)]
public sealed class KeepDistanceMovePresetSO : MovePresetSO
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float desiredDistance = 5f;
    [SerializeField] private float tolerance = 0.5f;
    public override Type GetComponentType()
    {
        throw new NotImplementedException();
    }

    public override void ApplyTo(MoveBase move, EnemySO enemy)
    {
        throw new NotImplementedException();
    }
}