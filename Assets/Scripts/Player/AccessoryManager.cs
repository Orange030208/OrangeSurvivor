using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PropertiesManager))]
[RequireComponent(typeof(FeatureHost))]
public class AccessoryManager : MonoBehaviour
{
    private FeatureHost featureHost;
    private readonly Dictionary<string, List<EquippedAccessory>> equippedAccessories = new();
    private readonly List<AccessoryDataSO> accessories = new();

    public event Action<AccessoryDataSO> OnAccessoryEquipped;
    public event Action<AccessoryDataSO> OnAccessoryUnequipped;

    public IReadOnlyList<AccessoryDataSO> EquippedAccessories => accessories.AsReadOnly();

    private void Awake()
    {
        featureHost = GetComponent<FeatureHost>();
    }

    private void OnDisable()
    {
        ClearEquippedAccessories();
    }

    public bool EquipAccessory(AccessoryDataSO accessoryData)
    {
        if (accessoryData == null || featureHost == null)
        {
            return false;
        }

        var equipped = new EquippedAccessory(accessoryData);
        if (!equippedAccessories.TryGetValue(accessoryData.AccessoryId, out var list))
        {
            list = new List<EquippedAccessory>();
            equippedAccessories[accessoryData.AccessoryId] = list;
        }

        list.Add(equipped);
        accessories.Add(accessoryData);

        FeatureInstaller.InstallSource(featureHost, equipped.RuntimeSourceId, accessoryData);

        OnAccessoryEquipped?.Invoke(accessoryData);
        return true;
    }

    public bool UnequipAccessory(string accessoryId)
    {
        if (!equippedAccessories.TryGetValue(accessoryId, out var list)) return false;
        if (list.Count == 0)
        {
            equippedAccessories.Remove(accessoryId);
            return false;
        }

        var equipped = list[list.Count - 1];
        list.RemoveAt(list.Count - 1);
        if (list.Count == 0)
        {
            equippedAccessories.Remove(accessoryId);
        }

        FeatureInstaller.RemoveSource(featureHost, equipped.RuntimeSourceId);
        int index = accessories.LastIndexOf(equipped.Data);
        if (index >= 0) accessories.RemoveAt(index);

        OnAccessoryUnequipped?.Invoke(equipped.Data);
        return true;
    }

    public bool UnequipAccessory(AccessoryDataSO accessoryData)
    {
        if (accessoryData == null) return false;
        return UnequipAccessory(accessoryData.AccessoryId);
    }

    public IReadOnlyList<AccessoryDataSO> GetEquippedAccessories()
    {
        return accessories.AsReadOnly();
    }

    public bool IsEquipped(string accessoryId)
    {
        return equippedAccessories.TryGetValue(accessoryId, out var list) && list.Count > 0;
    }

    private void ClearEquippedAccessories()
    {
        foreach (var pair in equippedAccessories)
        {
            List<EquippedAccessory> list = pair.Value;
            for (int i = 0; i < list.Count; i++)
            {
                FeatureInstaller.RemoveSource(featureHost, list[i].RuntimeSourceId);
            }
        }

        equippedAccessories.Clear();
        accessories.Clear();
    }

    private class EquippedAccessory
    {
        public AccessoryDataSO Data { get; }
        public string RuntimeSourceId { get; }

        public EquippedAccessory(AccessoryDataSO data)
        {
            Data = data;
            RuntimeSourceId = $"ACC_{data.AccessoryId}_{Guid.NewGuid():N}";
        }
    }
}