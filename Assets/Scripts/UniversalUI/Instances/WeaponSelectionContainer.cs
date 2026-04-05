using System;
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

        [Header("Prop管理")] [SerializeField] private Transform propContainersParent;

        private Action _clickAction;
        private bool _isSelected;
        public bool isSelected => _isSelected;

        public void Configure(Sprite icon, string weaponName, int level, Action clickAction,WeaponDataSO weaponData)
        {
            this._clickAction = clickAction;

            iconImage.sprite = icon;
            weaponNameText.text = weaponName;
            Color levelColor = ItemLevelColorHelper.GetColorByLevel(level);
            weaponNameText.color = levelColor;
            
            foreach (var image in levelDependencyImages)
            {
                image.color = levelColor;
            }

            ConfigurePropContainer(weaponData);

            // 先清理旧事件，再绑定新事件
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnButtonClick);
        }

        private void ConfigurePropContainer(WeaponDataSO weaponData)
        {
            PropContainerManager.GeneratePropContainers(weaponData.GetBaseProps(), propContainersParent);
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
