using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UniversalUI.Instances
{
    public class UpgradeContainer : MonoBehaviour
    {
        [SerializeField] private Image image;
        [SerializeField] private TextMeshProUGUI upgradeNameText;
        [SerializeField] private TextMeshProUGUI upgradeValueText;

        [field:SerializeField]public Button Button {private set; get; }

        public void Configure(Sprite icon, string upgradeName, string upgradeValue,Action buttonAction)
        {
            image.sprite = icon;
            upgradeNameText.text = upgradeName;
            upgradeValueText.text = upgradeValue;
            Button.onClick.RemoveAllListeners();
            Button.onClick.AddListener(() => buttonAction?.Invoke());
        }
    }
}