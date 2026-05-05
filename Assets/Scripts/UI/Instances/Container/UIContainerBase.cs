using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

[RequireComponent(typeof(Graphic))]
public abstract class UIContainerBase<T, K> : MonoBehaviour, IDisposable, IPointerClickHandler
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

    [Header("卡片品质表现")]
    [SerializeField] protected CardQualityVisualController cardQualityVisualController;

    public event Action<PointerEventData> OnClicked;

    public virtual void Dispose()
    {
        CleanClickEvent();
    }

    public void CleanClickEvent()
    {
        OnClicked = null;
    }

    public void RenderQuality(CardQuality quality)
    {
        if (cardQualityVisualController == null)
        {
            cardQualityVisualController = GetComponent<CardQualityVisualController>();
        }

        if (cardQualityVisualController == null)
        {
            return;
        }

        if (!cardQualityVisualController.Apply(quality))
        {
            Debug.LogWarning($"{nameof(UIContainerBase<T, K>)} '{name}' could not resolve card quality '{quality}'.", this);
        }
    }

    protected void RenderItemQuality(ItemDataSO itemData, int qualityValue)
    {
        RenderQuality(CardQualityResolver.FromItem(itemData, qualityValue));
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
