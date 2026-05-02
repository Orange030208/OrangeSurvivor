using System;
using UnityEngine;
using UnityEngine.UI;

public class CharacterButton : UIScrollListItemBase
{
    [SerializeField] private Image characterIconImage;
    [SerializeField] private UIClickTarget clickTarget;

    protected override void Awake()
    {
        base.Awake();
        clickTarget = GetComponent<UIClickTarget>();
    }

    public void Configure(Sprite characterIcon, Action onClick)
    {
        characterIconImage.sprite = characterIcon;
        clickTarget.ClearListeners();
        clickTarget.OnClicked += () =>
        {
            AudioSfxBridge.RequestPlay(AudioSfxKey.WoodenButtonClicked);
            onClick?.Invoke();
        };
    }

    public void SetSelected(bool selected)
    {
        // 角色选择的选中动画暂时停用；保留入口，避免破坏列表选择流程。
    }
}
