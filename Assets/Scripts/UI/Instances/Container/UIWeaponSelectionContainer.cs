using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class UIWeaponSelectionContainer:UIContainerBase<InfoAddIndex<WeaponInfo>,UIPropertiesViewList>
{
    private bool _isSelected;
    public bool IsSelected => _isSelected;
    
    public override void Configure(InfoAddIndex<WeaponInfo> resource)
    {
        iconImage.sprite = resource.info.weaponData.ItemIcon;
        nameText.text = ItemDisplayHelper.GetWeaponDisplayName(resource.info.weaponData.ItemName, resource.info.level);
        bottom.Render(resource.info.weaponData.GetPropsList());
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