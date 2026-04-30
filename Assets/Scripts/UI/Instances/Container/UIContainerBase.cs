using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

[RequireComponent(typeof(Graphic))]
public abstract class UIContainerBase<T, K> : MonoBehaviour, IContainerColorRender, IDisposable, IPointerClickHandler, IConfigurable<T>
    where K : MonoBehaviour
{
    [Header("--")]
    [FormerlySerializedAs("IconImage")]
    [SerializeField] protected Image iconImage;

    [FormerlySerializedAs("accessoryNameText")]
    [SerializeField] protected TextMeshProUGUI nameText;

    [FormerlySerializedAs("priceText")]
    [FormerlySerializedAs("recyclePriceText")]
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

    public void RenderColor(ItemDataSO itemData, int colorDependency)
    {
        ItemQualityVisualResolver.Apply(this, itemData, colorDependency, iconImage);
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

public interface IConfigurable<T>
{
    public void Configure(T resource);
}
