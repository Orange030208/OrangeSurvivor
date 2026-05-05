using Orange.UIFramework;
using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(UIClickTarget))]
public class CharacterButton : MonoBehaviour
{
    [SerializeField] private Image characterIconImage;
    [SerializeField] private UIClickTarget clickTarget;

    private void Awake()
    {
        ResolveReferences();
    }

    public void Configure(Sprite characterIcon, Action onClick)
    {
        ResolveReferences();
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

    private void ResolveReferences()
    {
        if (characterIconImage == null)
        {
            characterIconImage = GetComponentInChildren<Image>(true);
        }

        if (clickTarget == null)
        {
            clickTarget = GetComponent<UIClickTarget>();
        }

        if (clickTarget == null)
        {
            clickTarget = GetComponentInChildren<UIClickTarget>(true);
        }

        if (characterIconImage == null)
        {
            throw new MissingReferenceException($"{nameof(CharacterButton)} '{name}' is missing character icon image.");
        }

        if (clickTarget == null)
        {
            throw new MissingReferenceException($"{nameof(CharacterButton)} '{name}' is missing click target.");
        }
    }
}
