using System;
using System.Collections.Generic;
using Survivors.Accessory;
using UnityEngine;

namespace Survivors.Player
{
    [RequireComponent(typeof(PropertiesManager))]
    public class AccessoryManager : MonoBehaviour
    {
        [SerializeField] private List<AccessoryDataSO> initialAccessories = new();

        private PropertiesManager propertiesManager;
        private readonly Dictionary<string, EquippedAccessory> equippedAccessories = new();
        private readonly List<IAccessoryEffect> activeEffects = new();
        private readonly List<AccessoryDataSO> accessories = new();

        public event Action<AccessoryDataSO> OnAccessoryEquipped;
        public event Action<AccessoryDataSO> OnAccessoryUnequipped;

        public IReadOnlyList<AccessoryDataSO> EquippedAccessories => accessories.AsReadOnly();

        private void Awake()
        {
            propertiesManager = GetComponent<PropertiesManager>();
        }

        private void Start()
        {
            foreach (var accessory in initialAccessories)
            {
                if (accessory != null)
                {
                    EquipAccessory(accessory);
                }
            }
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            foreach (var effect in activeEffects)
            {
                effect.OnUpdate(gameObject, propertiesManager, deltaTime);
            }
        }

        public bool EquipAccessory(AccessoryDataSO accessoryData)
        {
            if (accessoryData == null) return false;
            if (equippedAccessories.ContainsKey(accessoryData.AccessoryId)) return false;

            var equipped = new EquippedAccessory(accessoryData);
            equippedAccessories[accessoryData.AccessoryId] = equipped;
            accessories.Add(accessoryData);

            ApplyAccessoryEffects(equipped);

            OnAccessoryEquipped?.Invoke(accessoryData);
            return true;
        }

        public bool UnequipAccessory(string accessoryId)
        {
            if (!equippedAccessories.TryGetValue(accessoryId, out var equipped)) return false;

            RemoveAccessoryEffects(equipped);

            equippedAccessories.Remove(accessoryId);
            accessories.Remove(equipped.Data);

            OnAccessoryUnequipped?.Invoke(equipped.Data);
            return true;
        }

        public bool UnequipAccessory(AccessoryDataSO accessoryData)
        {
            if (accessoryData == null) return false;
            return UnequipAccessory(accessoryData.AccessoryId);
        }

        private void ApplyAccessoryEffects(EquippedAccessory equipped)
        {
            foreach (var effect in equipped.Effects)
            {
                effect.OnEquip(gameObject, propertiesManager);
                activeEffects.Add(effect);
            }
        }

        private void RemoveAccessoryEffects(EquippedAccessory equipped)
        {
            foreach (var effect in equipped.Effects)
            {
                effect.OnUnequip(gameObject, propertiesManager);
                activeEffects.Remove(effect);
            }
        }

        public IReadOnlyList<AccessoryDataSO> GetEquippedAccessories()
        {
            return accessories.AsReadOnly();
        }

        public bool IsEquipped(string accessoryId)
        {
            return equippedAccessories.ContainsKey(accessoryId);
        }

        private class EquippedAccessory
        {
            public AccessoryDataSO Data { get; }
            public List<IAccessoryEffect> Effects { get; }

            public EquippedAccessory(AccessoryDataSO data)
            {
                Data = data;
                Effects = data.CreateEffects();
            }
        }
    }
}
