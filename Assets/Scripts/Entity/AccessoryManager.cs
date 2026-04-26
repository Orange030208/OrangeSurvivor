using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PropertiesManager))]
[RequireComponent(typeof(FeatureHost))]
public class AccessoryManager : EntityComponentBase
{
    private FeatureHost featureHost;
    private Entity owner;
    private readonly Dictionary<string, List<EquippedAccessory>> equippedAccessories = new();
    private readonly List<AccessoryDataSO> accessories = new();
    private readonly List<EquippedAccessoryInfo> equippedAccessoryInfos = new();

    public event Action<AccessoryDataSO> OnAccessoryEquipped;
    public event Action<AccessoryDataSO> OnAccessoryUnequipped;

    public IReadOnlyList<AccessoryDataSO> EquippedAccessories => accessories.AsReadOnly();
    public IReadOnlyList<EquippedAccessoryInfo> EquippedAccessoryInfos => equippedAccessoryInfos.AsReadOnly();


    public override Entity Owner => owner;
    public override void Initialize(Entity owner)
    {
        this.owner = owner;
        featureHost = this.owner.GetComponent<FeatureHost>();
    }

    public override void OnDisableComponent()
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
        equippedAccessoryInfos.Add(new EquippedAccessoryInfo(accessoryData, equipped.RuntimeSourceId));

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
        RemoveEquippedAccessoryInfo(equipped.RuntimeSourceId);

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

    public IReadOnlyList<EquippedAccessoryInfo> GetEquippedAccessoryInfos()
    {
        return equippedAccessoryInfos.AsReadOnly();
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
        equippedAccessoryInfos.Clear();
    }

    private void RemoveEquippedAccessoryInfo(string runtimeSourceId)
    {
        for (int i = equippedAccessoryInfos.Count - 1; i >= 0; i--)
        {
            if (equippedAccessoryInfos[i].RuntimeSourceId != runtimeSourceId)
            {
                continue;
            }

            equippedAccessoryInfos.RemoveAt(i);
            return;
        }
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
