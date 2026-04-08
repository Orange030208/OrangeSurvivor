using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Graphic))]
public class ClickOnlyHandler : MonoBehaviour,IPointerClickHandler,IDisposable
{
    public event Action<PointerEventData> OnClick;
    
    public void OnPointerClick(PointerEventData eventData)
    {
        OnClick?.Invoke(eventData);
    }

    public void Dispose()
    {
        OnClick = null;
    }
}
