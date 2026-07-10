#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
#endif
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Accessory Data List", menuName = ScriptableObjectMenuPaths.ACCESSORY_DATA_LIST, order = 0)]
public class AccessoryDataListSO : ScriptableObject
{
    [field: SerializeField] public AccessoryDataSO[] Accessories { get; private set; }

#if UNITY_EDITOR
    private static readonly string[] ACCESSORIES_DATA_PATH =
    {
        GameContentAssetPaths.AccessoriesData
    };
    
    public void RefreshAccessories()
    {
        string[] guids = AssetDatabase.FindAssets("t:AccessoryDataSO", GetExistingAccessoryDataPaths());

        if (guids.Length == 0)
        {
            Debug.LogWarning($"No AccessoryDataSO assets found in {ACCESSORIES_DATA_PATH}");
            Accessories = Array.Empty<AccessoryDataSO>();
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            return;
        }

        var accessories = new List<AccessoryDataSO>();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var accessory = AssetDatabase.LoadAssetAtPath<AccessoryDataSO>(path);
            if (accessory != null)
            {
                accessories.Add(accessory);
            }
        }

        Accessories = accessories.OrderBy(acc => acc.ItemName).ToArray();
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
        Debug.Log($"Successfully loaded {Accessories.Length} accessories from {ACCESSORIES_DATA_PATH}");
    }

    private static string[] GetExistingAccessoryDataPaths()
    {
        return ACCESSORIES_DATA_PATH.Where(AssetDatabase.IsValidFolder).ToArray();
    }
#endif
}
