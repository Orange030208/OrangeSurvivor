using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(UIClickTarget), typeof(UIRevealMotion))]
public class CharacterButton : UIScrollListItemBase
{
    [SerializeField] private Image characterIconImage;
    [SerializeField] private UIClickTarget clickTarget;

    private bool isSelected;

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
        isSelected = selected;
        RuntimeMotion?.Kill();
        RuntimeMotion?.Play(isSelected ? UIMotionAction.Highlight : UIMotionAction.Normal);
    }

    protected override void OnPresentationRefreshed()
    {
        SetSelectedImmediate(isSelected);
    }

    private void SetSelectedImmediate(bool selected)
    {
        RuntimeMotion?.Kill();
        RuntimeMotion?.SetImmediate(selected ? UIMotionAction.Highlight : UIMotionAction.Normal);
    }
}
