using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Map Adjacency Rule Set", menuName = ScriptableObjectMenuPaths.MAP_ADJACENCY_RULE_SET, order = 0)]
public class MapAdjacencyRuleSetSO : ScriptableObject
{
    public bool useExplicitRules = true;
    public bool useSocketCompatibility = false;
    public bool allowMissingRules = false;
    public List<MapAdjacencyRule> rules = new();

    public MapAdjacencyRuleSetSnapshot CreateSnapshot()
    {
        return new MapAdjacencyRuleSetSnapshot(useExplicitRules, useSocketCompatibility, allowMissingRules, rules);
    }

    private void OnValidate()
    {
        if (rules == null)
        {
            rules = new List<MapAdjacencyRule>();
        }
    }
}
