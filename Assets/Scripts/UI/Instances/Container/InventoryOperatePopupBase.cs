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
    [Header("基础信息")]
    [SerializeField] protected Image iconImage;
    
    [SerializeField] protected TextMeshProUGUI nameText;
    
    [SerializeField] protected ExtraInfoDescriber bottom;

    protected readonly InfoDocumentService InfoDocumentService = new();

    public event Action<PointerEventData> OnClicked;

    public virtual void Dispose()
    {
        CleanClickEvent();
    }

    public void CleanClickEvent()
    {
        OnClicked = null;
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
    }

    protected void RenderTier(IHasContentTier source)
    {
    }

    protected void DisplayDocument(object source)
    {
        if (bottom == null)
        {
            return;
        }

        if (source != null && InfoDocumentService.TryBuild(source, out InfoDocument document))
        {
            bottom.Display(document);
            return;
        }

        bottom.Display((InfoDocument)null);
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
