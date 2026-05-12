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

        if (!equippedAccessoryDict.TryGetValue(accessoryData.AccessoryId, out var dictList))
        {
            dictList = new List<RuntimeAccessoryData>();
            equippedAccessoryDict[accessoryData.AccessoryId] = dictList;
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

    public bool UnequipAccessory(AccessoryDataSO accessoryData)
    {
        if (accessoryData == null || featureHost == null || propertiesManager == null)
        {
            return false;
        }

        if (!equippedAccessoryDict.TryGetValue(accessoryData.AccessoryId, out var dictList)) return false;
        if (dictList.Count == 0)
        {
            equippedAccessoryDict.Remove(accessoryData.AccessoryId);
            return false;
        }

        //移除同类饰品的最后添加的一个
        var equipped = dictList[dictList.Count - 1];
        dictList.RemoveAt(dictList.Count - 1);
        if (dictList.Count == 0)
        {
            equippedAccessoryDict.Remove(accessoryData.AccessoryId);
        }

        featureHost.RemoveFeature(equipped.RuntimeId);
        propertiesManager.RemoveModifiers(equipped.RuntimeId);

        int index = equippedAccessoryList.LastIndexOf(equipped);
        if (index >= 0) equippedAccessoryList.RemoveAt(index);

        OnAccessoryUnequipped?.Invoke(equipped.AccessoryData);
        return true;
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
