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
            GameEventBus.Subscribe<WeaponSelectionChangedEvent>(OnSelectionWeaponsChanged);

            startButton.onClick.AddListener(() =>
            {
                if (HasWeaponSelected) GameManager.Instance.StartGame();
            });

            GameEventBus.Publish<RequestWeaponSelectionSnapshotEvent>();
        }

        protected override void OnPageClosed()
        {
            GameEventBus.Unsubscribe<WeaponSelectionChangedEvent>(OnSelectionWeaponsChanged);

            // 清理所有Button事件
            foreach (var container in weaponContainers)
            {
                container.Cleanup();
            }

            startButton.onClick.RemoveAllListeners();
        }

        private void OnSelectionWeaponsChanged(WeaponSelectionChangedEvent e)
        {
            currentSelectionWeapons = e.SelectionWeapons;
            if (currentSelectionWeapons == null) return;
            ApplySelectionWeapons(currentSelectionWeapons);
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
                    weaponData,
                    selectionWeapons[i].level,
                    () => OnWeaponClicked(index)
                );
            }
        }

        private void OnWeaponClicked(int selectedIndex)
        {
            GameEventBus.Publish(new WeaponSelectionOptionClickedEvent(selectedIndex));

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
