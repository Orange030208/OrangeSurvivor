using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Map Constraint Profile", menuName = ScriptableObjectMenuPaths.MAP_CONSTRAINT_PROFILE, order = 0)]
public class MapConstraintProfileSO : ScriptableObject
{
    public int borderPadding;
    public bool requireConnectedFloor;
    public int minimumConnectedFloorArea;
    public List<RectInt> blockedRegions = new();
    public List<MapForcedCell> forcedCells = new();

    public MapConstraintProfileSnapshot CreateSnapshot()
    {
        return new MapConstraintProfileSnapshot(borderPadding, requireConnectedFloor, minimumConnectedFloorArea, blockedRegions, forcedCells);
    }

    private void OnValidate()
    {
        borderPadding = Mathf.Max(0, borderPadding);
        minimumConnectedFloorArea = Mathf.Max(0, minimumConnectedFloorArea);

        if (blockedRegions == null)
        {
            blockedRegions = new List<RectInt>();
        }

        if (forcedCells == null)
        {
            forcedCells = new List<MapForcedCell>();
        }
    }
}
