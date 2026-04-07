using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeContainer : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI upgradeNameText;
    [SerializeField] private TextMeshProUGUI upgradeValueText;
    [SerializeField] private Button button;

    private int _containerIndex;
    private PropType _propType;
    private float _value;

    public void Configure(int containerIndex, Sprite icon, string upgradeName, string upgradeValue, PropType propType, float value)
    {
        _containerIndex = containerIndex;
        _propType = propType;
        _value = value;

        iconImage.sprite = icon;
        upgradeNameText.text = upgradeName;
        upgradeValueText.text = upgradeValue;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnButtonClick);
    }

    private void OnButtonClick()
    {
        GameEventBus.Publish(new UpgradeContainerClickedEvent(_containerIndex, _propType, _value));
    }

    public void Cleanup()
    {
        button.onClick.RemoveAllListeners();
    }
}