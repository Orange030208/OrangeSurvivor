using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UniversalUI.Instances
{
    public class WeaponSelectionContainer : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI weaponNameText;
        [SerializeField] private Button button;
        [SerializeField] private Image[] levelDependencyImages;
        [SerializeField] private Image outline;

        [Header("Prop管理")][SerializeField] private Transform propContainersParent;

        private Action _clickAction;
        private bool _isSelected;
        public bool isSelected => _isSelected;

        public void Configure(Sprite icon, string weaponName, int level, Action clickAction, WeaponDataSO weaponData)
        {
            this._clickAction = clickAction;

            iconImage.sprite = icon;
            string levelPrefix = level switch
            {
                1 => "灰",
                2 => "绿",
                3 => "蓝",
                4 => "紫",
                5 => "橙",
                6 => "红",
                _ => ""
            };
            weaponNameText.text = $"Lv.{level} [{levelPrefix}] {weaponName}";
            Color levelColor = ItemLevelColorHelper.GetColorByLevel(level);
            weaponNameText.color = levelColor;
            outline.color = levelColor;

            foreach (var image in levelDependencyImages)
            {
                image.color = levelColor;
            }

            Dictionary<PropType, float> calculatedProps = WeaponPropsCalculator.GetProps(weaponData, level);
            ConfigurePropContainer(calculatedProps);

            // 先清理旧事件，再绑定新事件
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnButtonClick);
        }

        private void ConfigurePropContainer(Dictionary<PropType, float> calculatedProps)
        {
            PropContainerManager.GeneratePropContainers(calculatedProps, propContainersParent);
        }

        private void OnButtonClick()
        {
            _clickAction?.Invoke();
        }

        public void Select()
        {
            transform.DOKill();
            _isSelected = true;
            transform.DOScale(Vector3.one * 1.1f, .3f).SetEase(Ease.InOutSine);
        }

        public void Deselect()
        {
            transform.DOKill();
            _isSelected = false;
            transform.DOScale(Vector3.one, .3f).SetEase(Ease.InOutSine);
        }

        public void Cleanup()
        {
            button.onClick.RemoveAllListeners();
            _clickAction = null;
            _isSelected = false;
            transform.DOKill();
        }
    }
}
