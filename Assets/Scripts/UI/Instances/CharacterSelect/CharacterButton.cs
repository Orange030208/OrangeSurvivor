using Orange.UIFramework;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CharacterButton : ViewPartBase
{
    [SerializeField] private Image characterIconImage;
    [SerializeField] private Button button;

    private UnityAction currentClickAction;

    private void Awake()
    {
        ResolveReferences();
    }

    public void Configure(Sprite characterIcon, Action onClick)
    {
        ResolveReferences();
        characterIconImage.sprite = characterIcon;
        if (currentClickAction != null)
        {
            button.onClick.RemoveListener(currentClickAction);
        }

        currentClickAction = () =>
        {
            AudioSfxBridge.RequestPlay(AudioSfxKey.UiConfirm);
            onClick?.Invoke();
        };
        button.onClick.AddListener(currentClickAction);
    }

    public void SetSelected(bool selected)
    {
        // 角色选择的选中动画暂时停用；保留入口，避免破坏列表选择流程。
    }

    private void ResolveReferences()
    {
        if (characterIconImage == null)
        {
            characterIconImage = GetComponentInChildren<Image>(true);
        }

        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (button == null)
        {
            button = GetComponentInChildren<Button>(true);
        }

        if (characterIconImage == null)
        {
            throw new MissingReferenceException($"{nameof(CharacterButton)} '{name}' is missing character icon image.");
        }

        if (button == null)
        {
            throw new MissingReferenceException($"{nameof(CharacterButton)} '{name}' is missing button.");
        }
    }
}
