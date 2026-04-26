using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BasePropGroup", menuName = "Survivors/Player/Base Prop Group")]
public class BasePropGroupSO : ScriptableObject
{
    [SerializeField] private List<BasePropData> values = new();

    public IReadOnlyList<BasePropData> Values => values;
}
