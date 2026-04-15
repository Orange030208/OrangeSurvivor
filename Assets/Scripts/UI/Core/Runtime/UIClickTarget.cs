using System;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class UIClickTarget : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private bool interactable = true;

    public event Action OnClicked;

    public bool Interactable
    {
        get => interactable;
        set => interactable = value;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!interactable || eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        OnClicked?.Invoke();
    }

    public void ClearListeners()
    {
        OnClicked = null;
    }
}
