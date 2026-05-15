using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PropertiesManager))]
[RequireComponent(typeof(FeatureHost))]
public class AccessoryManager : EntityComponentBase
{
    private FeatureHost featureHost;
    private PropertiesManager propertiesManager;
    private Entity owner;
    private readonly Dictionary<string, List<RuntimeAccessoryData>> equippedAccessoryDict = new();
    private readonly List<RuntimeAccessoryData> equippedAccessoryList = new();
    public event Action<AccessoryDataSO> OnAccessoryEquipped;
    public event Action<AccessoryDataSO> OnAccessoryUnequipped;

    public IReadOnlyList<RuntimeAccessoryData> EquippedAccessoryList => equippedAccessoryList.AsReadOnly();


    public override Entity Owner => owner;
    public override void Initialize(Entity owner)
    {
        this.owner = owner;
        featureHost = this.owner.GetComponent<FeatureHost>();
        propertiesManager = this.owner.GetComponent<PropertiesManager>();
        if (!this.owner.TryGetComponent<IInitialAccessoryProvider>(out IInitialAccessoryProvider initialAccessoryProvider))
        {
            return;
        }

        IReadOnlyList<AccessoryDataSO> initialAccessories = initialAccessoryProvider.InitialAccessories;
        if (initialAccessories == null || initialAccessories.Count == 0)
        {
            return;
        }

        for (int i = 0; i < initialAccessories.Count; i++)
        {
            EquipAccessory(initialAccessories[i], false);
        }
    }

    public override void OnDisableComponent()
    {
        ClearEquippedAccessories();
    }

    public bool EquipAccessory(AccessoryDataSO accessoryData, bool playSfx = true)
    {
        if (accessoryData == null || featureHost == null || propertiesManager == null)
        {
            return false;
        }

        if (!CanEquipAccessory(accessoryData))
        {
            return false;
        }

        string accessoryKey = GetAccessoryKey(accessoryData);
        if (!equippedAccessoryDict.TryGetValue(accessoryKey, out var dictList))
        {
            dictList = new List<RuntimeAccessoryData>();
            equippedAccessoryDict[accessoryKey] = dictList;
        }

        var newAccessoryData = new RuntimeAccessoryData(accessoryData);

        dictList.Add(newAccessoryData);
        equippedAccessoryList.Add(newAccessoryData);

        featureHost.InstallFeature(newAccessoryData.RuntimeId, newAccessoryData.AccessoryData.SpecialFeatures);
        propertiesManager.AddModifiers(newAccessoryData.RuntimeId, accessoryData.PropertyModifiers);

        OnAccessoryEquipped?.Invoke(accessoryData);
        if (playSfx)
        {
            AudioSfxBridge.RequestPlay(AudioSfxKey.ItemEquipped);
        }
        return true;
    }

    public int GetEquippedCount(AccessoryDataSO accessoryData)
    {
        if (accessoryData == null)
        {
            return 0;
        }

        if (!string.IsNullOrWhiteSpace(accessoryData.AccessoryId))
        {
            return GetEquippedCount(accessoryData.AccessoryId);
        }

        int count = 0;
        for (int i = 0; i < equippedAccessoryList.Count; i++)
        {
            if (equippedAccessoryList[i].AccessoryData == accessoryData)
            {
                count++;
            }
        }

        return count;
    }

    public int GetEquippedCount(string accessoryId)
    {
        if (string.IsNullOrWhiteSpace(accessoryId))
        {
            return 0;
        }

        return equippedAccessoryDict.TryGetValue(accessoryId, out List<RuntimeAccessoryData> dictList)
            ? dictList.Count
            : 0;
    }

    public bool CanEquipAccessory(AccessoryDataSO accessoryData)
    {
        return accessoryData != null && accessoryData.CanOwnMore(GetEquippedCount(accessoryData));
    }

    public bool UnequipAccessory(AccessoryDataSO accessoryData)
    {
        if (accessoryData == null || featureHost == null || propertiesManager == null)
        {
            return false;
        }

        string accessoryKey = GetAccessoryKey(accessoryData);
        if (!equippedAccessoryDict.TryGetValue(accessoryKey, out var dictList)) return false;
        if (dictList.Count == 0)
        {
            equippedAccessoryDict.Remove(accessoryKey);
            return false;
        }

        //移除同类饰品的最后添加的一个
        var equipped = dictList[dictList.Count - 1];
        dictList.RemoveAt(dictList.Count - 1);
        if (dictList.Count == 0)
        {
            equippedAccessoryDict.Remove(accessoryKey);
        }

        featureHost.RemoveFeature(equipped.RuntimeId);
        propertiesManager.RemoveModifiers(equipped.RuntimeId);

        int index = equippedAccessoryList.LastIndexOf(equipped);
        if (index >= 0) equippedAccessoryList.RemoveAt(index);

        OnAccessoryUnequipped?.Invoke(equipped.AccessoryData);
        return true;
    }

    private static string GetAccessoryKey(AccessoryDataSO accessoryData)
    {
        return !string.IsNullOrWhiteSpace(accessoryData.AccessoryId)
            ? accessoryData.AccessoryId
            : accessoryData.GetInstanceID().ToString();
    }

    private void ClearEquippedAccessories()
    {
        if (featureHost == null || propertiesManager == null)
        {
            equippedAccessoryDict.Clear();
            equippedAccessoryList.Clear();
            return;
        }

        foreach (var pair in equippedAccessoryDict)
        {
            List<RuntimeAccessoryData> list = pair.Value;
            for (int i = 0; i < list.Count; i++)
            {
                featureHost.RemoveFeature(list[i].RuntimeId);
                propertiesManager.RemoveModifiers(list[i].RuntimeId);
            }
        }

        equippedAccessoryDict.Clear();
        equippedAccessoryList.Clear();
    }
}

public struct RuntimeAccessoryData
{
    public string RuntimeId;
    public AccessoryDataSO AccessoryData;

    public RuntimeAccessoryData(AccessoryDataSO accessoryData)
    {
        this.AccessoryData = accessoryData;
        RuntimeId = $"ACC_{accessoryData.AccessoryId}_{Guid.NewGuid():N}";
    }
}
