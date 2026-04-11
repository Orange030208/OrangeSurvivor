using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class UIWeaponSelectionContainer : UIContainerBase<InfoAddIndex<WeaponLevelEntry>, DescriptionListDisplayer>
{
    private bool _isSelected;
    public bool IsSelected => _isSelected;
    
    public override void Configure(InfoAddIndex<WeaponLevelEntry> resource)
    {
        iconImage.sprite = resource.info.weaponData.ItemIcon;
        nameText.text = ItemDisplayHelper.GetWeaponDisplayName(resource.info.weaponData.ItemName, resource.info.level);
        bottom.DisplayDescriptions(resource.info.weaponData.GetDescriptions(resource.info.level));
        RenderColor(resource.info.weaponData, resource.info.level);
    }

    public void Select()
    {
        transform.DOKill();
        _isSelected = true;
        transform.DOScale(Vector3.one * 1.1f, .3f).SetEase(Ease.InOutSine);
    }

    public void Unselect()
    {
        transform.DOKill();
        _isSelected = false;
        transform.DOScale(Vector3.one, .3f).SetEase(Ease.InOutSine);
    }
}
