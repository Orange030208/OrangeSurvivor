using UnityEngine.Scripting.APIUpdating;

namespace AXR.Framework.UI
{
    using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class UIClickTarget : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private bool interactable = true;

    public event UnityAction OnClicked;

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
}
