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
        private readonly Dictionary<string, List<EquippedAccessory>> equippedAccessories = new();
        private readonly List<IAccessoryEffect> _activeEffects = new();
        [SerializeField] private readonly List<AccessoryDataSO> _accessories = new();

        public event Action<AccessoryDataSO> OnAccessoryEquipped;
        public event Action<AccessoryDataSO> OnAccessoryUnequipped;

        public IReadOnlyList<AccessoryDataSO> EquippedAccessories => _accessories.AsReadOnly();

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
            foreach (var effect in _activeEffects)
            {
                effect.OnUpdate(gameObject, propertiesManager, deltaTime);
            }
        }

        public bool EquipAccessory(AccessoryDataSO accessoryData)
        {
            if (accessoryData == null) return false;

            var equipped = new EquippedAccessory(accessoryData);
            if (!equippedAccessories.TryGetValue(accessoryData.AccessoryId, out var list))
            {
                list = new List<EquippedAccessory>();
                equippedAccessories[accessoryData.AccessoryId] = list;
            }
            list.Add(equipped);
            _accessories.Add(accessoryData);

            ApplyAccessoryEffects(equipped);

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

            RemoveAccessoryEffects(equipped);
            int index = _accessories.LastIndexOf(equipped.Data);
            if (index >= 0) _accessories.RemoveAt(index);

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
                _activeEffects.Add(effect);
            }
        }

        private void RemoveAccessoryEffects(EquippedAccessory equipped)
        {
            foreach (var effect in equipped.Effects)
            {
                effect.OnUnequip(gameObject, propertiesManager);
                _activeEffects.Remove(effect);
            }
        }

        public IReadOnlyList<AccessoryDataSO> GetEquippedAccessories()
        {
            return _accessories.AsReadOnly();
        }

        public bool IsEquipped(string accessoryId)
        {
            return equippedAccessories.TryGetValue(accessoryId, out var list) && list.Count > 0;
        }

        private class EquippedAccessory
        {
            public AccessoryDataSO Data { get; }
            public List<IAccessoryEffect> Effects { get; }

            public EquippedAccessory(AccessoryDataSO data)
            {
                Data = data;
                Effects = data.CreateEffects(Guid.NewGuid().ToString("N"));
            }
        }
    }
}
