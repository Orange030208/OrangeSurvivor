using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponSelectionContainer : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI weaponNameText;
    [SerializeField] private Button button;
    [Header("根据等级改变颜色的组件")]
    [SerializeField] private Graphic[] colorDependencyGraphics;
    [SerializeField] private Image outline;

    [Header("Prop管理")] [SerializeField] private Transform propContainersParent;

    private WeaponDataSO _currentWeaponData;
    private int _currentLevel;
    private int _containerIndex;
    private bool _isSelected;
    public bool isSelected => _isSelected;

    public void Configure(int containerIndex, WeaponDataSO weaponData, int level)
    {
        _containerIndex = containerIndex;
        _currentWeaponData = weaponData;
        _currentLevel = level;

        iconImage.sprite = weaponData.ItemIcon;
        weaponNameText.text = ItemDisplayHelper.GetWeaponDisplayName(weaponData.ItemName, level);
        Color levelColor = ColorHelper.GetColorByLevel(level);

        foreach (var image in colorDependencyGraphics)
        {
            image.color = levelColor;
        }

        Dictionary<PropType, float> calculatedProps = weaponData.GetPropsByLevel(level);
        ConfigurePropContainer(calculatedProps);

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnButtonClick);
    }

    private void ConfigurePropContainer(Dictionary<PropType, float> calculatedProps)
    {
        PropContainerManager.GeneratePropContainers(calculatedProps, propContainersParent);
    }

    private void OnButtonClick()
    {
        GameEventBus.Publish(new WeaponSelectionContainerClickedEvent(_containerIndex, _currentWeaponData, _currentLevel));
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
        _isSelected = false;
        transform.DOKill();
    }
}
