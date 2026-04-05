using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UniversalUI.Core.Runtime;

namespace UniversalUI.Instances
{
    public class WeaponSelectionUIPage : UIPageBase
    {
        [SerializeField] private WeaponSelectionContainer[] weaponContainers;
        private SelectionWeapon[] currentSelectionWeapons;
        [SerializeField] private Button startButton;

        protected override void OnPageOpened(UIPageOpenContext context)
        {
            WeaponSelectionManager.Instance.OnSelectionWeaponsChanged += OnSelectionWeaponsChanged;
            startButton.onClick.AddListener(() =>
            {
                if (HasWeaponSelected) GameManager.Instance.StartGame();
            });
            //TODO:架构修改
            if (!FetchSelectionWeapons()) return;
            ApplySelectionWeapons(currentSelectionWeapons);
        }

        protected override void OnPageClosed()
        {
            WeaponSelectionManager.Instance.OnSelectionWeaponsChanged -= OnSelectionWeaponsChanged;
            // 清理所有Button事件
            foreach (var container in weaponContainers)
            {
                container.Cleanup();
            }
            startButton.onClick.RemoveAllListeners();
        }

        private bool FetchSelectionWeapons()
        {
            currentSelectionWeapons = WeaponSelectionManager.Instance.SelectionWeapons;
            if (currentSelectionWeapons == null) return false;
            return true;
        }

        private void OnSelectionWeaponsChanged(SelectionWeapon[] selectionWeapons)
        {
            currentSelectionWeapons = selectionWeapons;
            ApplySelectionWeapons(selectionWeapons);
        }

        private void ApplySelectionWeapons(SelectionWeapon[] selectionWeapons)
        {
            int count = Mathf.Min(weaponContainers.Length, selectionWeapons.Length);
            for (int i = 0; i < count; i++)
            {
                int index = i; // 闭包捕获当前索引
                var weaponData = selectionWeapons[i].weaponData;

                // 配置UI，并绑定点击事件（内部处理选中状态）
                weaponContainers[i].Configure(
                    weaponData?.Sprite,
                    weaponData?.Name ?? string.Empty,
                    selectionWeapons[i].level,
                    () => OnWeaponClicked(index), // 点击时触发UI选中逻辑
                    weaponData
                );
            }
        }

        private void OnWeaponClicked(int selectedIndex)
        {
            var selectedWeapon = currentSelectionWeapons[selectedIndex];
            selectedWeapon.onWeaponSelected?.Invoke(selectedWeapon.weaponData, selectedWeapon.level);

            for (int i = 0; i < weaponContainers.Length; i++)
            {
                if (i == selectedIndex)
                    weaponContainers[i].Select();
                else
                    weaponContainers[i].Deselect();
            }
        }

        private bool HasWeaponSelected
        {
            get
            {
                foreach (var container in weaponContainers)
                {
                    if (container.isSelected)
                        return true;
                }
                return false;
            }
        }

    }
}