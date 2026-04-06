using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeContainer : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI upgradeNameText;
    [SerializeField] private TextMeshProUGUI upgradeValueText;
    [SerializeField] private Button button;

    private Action _clickAction;
    private bool _isSelected;
    public bool isSelected => _isSelected;

    public void Configure(Sprite icon, string upgradeName, string upgradeValue, Action clickAction)
    {
        _clickAction = clickAction;

        iconImage.sprite = icon;
        upgradeNameText.text = upgradeName;
        upgradeValueText.text = upgradeValue;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnButtonClick);
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