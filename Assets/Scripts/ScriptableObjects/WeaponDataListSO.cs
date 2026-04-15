#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
#endif
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Weapon Data List", menuName = "SO/Weapon Data List", order = 0)]
public class WeaponDataListSO : ScriptableObject
{
    [field: SerializeField] public WeaponDataSO[] Weapons { get; private set; }

#if UNITY_EDITOR
    private static readonly string[] WEAPONS_DATA_PATH = new string[]
    {
        "Assets/Resources/Data/Weapons/Melee",
        "Assets/Resources/Data/Weapons/Range"
    };

    [NaughtyAttributes.Button]
    public void RefreshWeapons()
    {
        string[] guids = AssetDatabase.FindAssets("t:WeaponDataSO", WEAPONS_DATA_PATH);

        if (guids.Length == 0)
        {
            Debug.LogWarning($"No WeaponDataSO assets found in {WEAPONS_DATA_PATH}");
            Weapons = Array.Empty<WeaponDataSO>();
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            return;
        }

        var weapons = new List<WeaponDataSO>();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var weapon = AssetDatabase.LoadAssetAtPath<WeaponDataSO>(path);
            if (weapon != null)
            {
                weapons.Add(weapon);
            }
        }

        Weapons = weapons.OrderBy(w => w.ItemName).ToArray();
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
        Debug.Log($"Successfully loaded {Weapons.Length} weapons from {WEAPONS_DATA_PATH}");
    }
#endif
}
