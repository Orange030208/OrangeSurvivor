using System;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class TooltipHoverArea : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public event Action<bool> HoverChanged;

    public void OnPointerEnter(PointerEventData eventData)
    {
        HoverChanged?.Invoke(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HoverChanged?.Invoke(false);
    }
}
