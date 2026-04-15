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

    public void Configure(Sprite characterIcon, bool selected, Action onClick)
    {
        characterIconImage.sprite = characterIcon;
        isSelected = selected;
        clickTarget.ClearListeners();
        clickTarget.OnClicked += () => onClick?.Invoke();
        SetSelectedImmediate(isSelected);
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        RuntimeMotion?.Kill();
        RuntimeMotion?.Play(isSelected ? UIMotionAction.Highlight : UIMotionAction.Show);
    }

    protected override void OnPresentationRefreshed()
    {
        SetSelectedImmediate(isSelected);
    }

    private void SetSelectedImmediate(bool selected)
    {
        RuntimeMotion?.Kill();
        RuntimeMotion?.SetImmediate(selected ? UIMotionAction.Highlight : UIMotionAction.Show);
    }
}
