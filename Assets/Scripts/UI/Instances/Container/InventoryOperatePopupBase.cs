using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

[RequireComponent(typeof(Graphic))]
public abstract class InventoryOperatePopupBase : PopupBase, IDisposable, IPointerClickHandler
{
    [Header("--")]
    [FormerlySerializedAs("IconImage")]
    [SerializeField] protected Image iconImage;

    [FormerlySerializedAs("accessoryNameText")]
    [SerializeField] protected TextMeshProUGUI nameText;

    [FormerlySerializedAs("priceText")]
    [FormerlySerializedAs("recyclePriceText")]
    [SerializeField] protected ExtraInfoDescriber bottom;

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
            Debug.LogWarning($"{nameof(InventoryOperatePopupBase)} '{name}' could not resolve card quality '{quality}'.", this);
        }
    }

    public virtual void OnPointerClick(PointerEventData eventData)
    {
        RaiseClicked(eventData);
    }

    public abstract void Configure(InventoryItemOperateResource resource);

    protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
    {
        if (!(context.Payload is InventoryItemOperateResource resource) || resource.itemData == null)
        {
            throw new ArgumentException($"{GetType().Name} '{name}' requires a valid {nameof(InventoryItemOperateResource)} payload.");
        }

        Configure(resource);
        return UniTask.CompletedTask;
    }

    protected override void OnClosed(CloseReason reason)
    {
        Dispose();
    }

    protected void RenderItemQuality(ItemDataSO itemData, int qualityValue)
    {
        RenderQuality(CardQualityResolver.FromItem(itemData, qualityValue));
    }

    protected void RaiseClicked(PointerEventData eventData)
    {
        OnClicked?.Invoke(eventData);
    }

    private void OnDestroy()
    {
        Dispose();
    }
}
