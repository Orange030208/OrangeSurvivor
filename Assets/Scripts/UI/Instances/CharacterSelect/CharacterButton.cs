using System;
using UnityEngine;
using UnityEngine.UI;

public class CharacterButton : MonoBehaviour
{
    [SerializeField] private Image characterIconImage;
    [SerializeField] private Button iconButton;
    [SerializeField] private float selectedScale = 1.08f;

    public event Action OnClick;

    public void Configure(Sprite characterIcon, bool isSelected)
    {
        characterIconImage.sprite = characterIcon;
        iconButton.onClick.RemoveAllListeners();
        iconButton.onClick.AddListener(() => OnClick?.Invoke());
        SetSelected(isSelected);
    }

    public void SetSelected(bool isSelected)
    {
        transform.localScale = isSelected ? Vector3.one * selectedScale : Vector3.one;
    }

    public void CleanUp()
    {
        OnClick = null;
        iconButton.onClick.RemoveAllListeners();
        transform.localScale = Vector3.one;
    }
}
