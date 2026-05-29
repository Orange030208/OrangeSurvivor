using System;
using Orange.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

[RequireComponent(typeof(Graphic))]
public abstract class UIContainerBase<T, K> : ViewPartBase, IDisposable, IPointerClickHandler
    where K : MonoBehaviour
{
    [SerializeField] protected Image iconImage;

    [SerializeField] protected TextMeshProUGUI nameText;

    [SerializeField] protected K bottom;

    public event Action<PointerEventData> OnClicked;

    public virtual void Dispose()
    {
        CleanClickEvent();
    }

    public void CleanClickEvent()
    {
        OnClicked = null;
    }

    protected void RenderItemQuality(ItemDataSO itemData, int qualityValue)
    {
    }

    protected void RenderTier(IHasContentTier source)
    {
    }

    public virtual void OnPointerClick(PointerEventData eventData)
    {
        RaiseClicked(eventData);
    }

    public abstract void Configure(T resource);

    protected void RaiseClicked(PointerEventData eventData)
    {
        OnClicked?.Invoke(eventData);
    }

    private void OnDestroy()
    {
        Dispose();
    }
}
