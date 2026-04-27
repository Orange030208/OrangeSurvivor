using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BasePropGroup", menuName = ScriptableObjectMenuPaths.BASE_PROP_GROUP)]
public class BasePropGroupSO : ScriptableObject
{
    [SerializeField] private List<BasePropData> values = new();

    public IReadOnlyList<BasePropData> Values => values;
}
