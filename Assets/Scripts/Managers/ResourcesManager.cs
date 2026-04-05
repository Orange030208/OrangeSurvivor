using System.Linq;
using UnityEngine;
using UniversalUI.Integration.Game.ScriptableObjects;

public static class ResourcesManager
{
    private const string propIconsDataPath = "Data/Prop Icons";

    private static PropIcon[] propIcons;

    public static Sprite GetPropIcon(PropType propType)
    {
        if (propIcons == null)
        {
            PropIconDataSO data = Resources.Load<PropIconDataSO>(propIconsDataPath);
            propIcons = data.PropIcons;
        }

        return propIcons.First(propIcon => propIcon.propType == propType).icon;
    }
}