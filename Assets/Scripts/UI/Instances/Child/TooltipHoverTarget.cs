using System;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using UnityEngine;
using UnityEngine.EventSystems;

public class TooltipHoverTarget : ViewPartBase, IPointerDownHandler, IPointerUpHandler, IPointerMoveHandler, IPointerExitHandler
{
    private static readonly Vector2 TOOLTIP_OFFSET = new Vector2(18f, -18f);
    private const float TOOLTIP_MARGIN = 12f;

    [SerializeField] private MonoBehaviour dataSourceComponent;

    private IInfoDocumentSource dataSource;
    private readonly InfoDocumentService infoDocumentService = new();
    private UIManager uiManager;
    private bool isPointerDown;
    private bool tooltipRequestInFlight;
    private bool tooltipOpenedForCurrentPress;
    private Vector2 pendingScreenPosition;

    private void Awake()
    {
        ValidateConfiguration();
        dataSource = (IInfoDocumentSource)dataSourceComponent;
    }

    public void SetDataSource(IInfoDocumentSource source)
    {
        dataSource = source;
        if (source is MonoBehaviour behaviour)
        {
            dataSourceComponent = behaviour;
        }
    }

    public void ConfigureOwner(UIManager manager)
    {
        uiManager = manager;
    }

    private void OnDisable()
    {
        if (!isPointerDown)
        {
            return;
        }

        Hide();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPointerDown = true;
        tooltipOpenedForCurrentPress = false;
        Show(eventData.position);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (!isPointerDown)
        {
            return;
        }

        Show(eventData.position);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Hide();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Hide();
    }

    private void Show(Vector2 screenPosition)
    {
        pendingScreenPosition = screenPosition;
        if (tooltipRequestInFlight || tooltipOpenedForCurrentPress)
        {
            ResolveUIManager(false)?.UpdateTooltipPosition(screenPosition);
            return;
        }

        ShowAsync(screenPosition).Forget();
    }

    private void ValidateConfiguration()
    {
        if (dataSourceComponent == null)
        {
            throw new MissingReferenceException($"{nameof(TooltipHoverTarget)} '{name}' is missing tooltip data source component.");
        }

        if (dataSourceComponent is not IInfoDocumentSource)
        {
            throw new MissingComponentException($"{nameof(TooltipHoverTarget)} '{name}' requires a component implementing {nameof(IInfoDocumentSource)}.");
        }
    }

    private void Hide()
    {
        if (!isPointerDown)
        {
            return;
        }

        isPointerDown = false;
        tooltipOpenedForCurrentPress = false;
        ResolveUIManager(false)?.HideTooltip();
    }

    private async UniTaskVoid ShowAsync(Vector2 screenPosition)
    {
        try
        {
            tooltipRequestInFlight = true;
            UIManager uiManager = ResolveUIManager(true);
            if (uiManager == null)
            {
                return;
            }

            TooltipOptions options = new TooltipOptions(
                screenPosition: screenPosition,
                offset: TOOLTIP_OFFSET,
                followPointer: true,
                margin: TOOLTIP_MARGIN,
                preferredAnchor: FloatingViewAnchor.BottomRight,
                useScreenPosition: true);

            object payload = dataSource;
            if (dataSource != null && infoDocumentService.TryBuild(dataSource, out InfoDocument builtDocument))
            {
                payload = builtDocument;
            }

            ViewHandle<DescribableTooltip> handle = await uiManager.ShowTooltipAsync<DescribableTooltip>(payload, options);
            if (!isPointerDown)
            {
                await handle.CloseAsync(CloseReason.Cancel);
                return;
            }

            tooltipOpenedForCurrentPress = true;
            if (pendingScreenPosition != screenPosition)
            {
                uiManager.UpdateTooltipPosition(pendingScreenPosition);
            }
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            isPointerDown = false;
            tooltipOpenedForCurrentPress = false;
        }
        finally
        {
            tooltipRequestInFlight = false;
        }
    }

    private UIManager ResolveUIManager(bool throwIfMissing)
    {
        if (uiManager != null)
        {
            return uiManager;
        }

        if (throwIfMissing)
        {
            throw new MissingReferenceException($"{nameof(TooltipHoverTarget)} '{name}' requires an explicit {nameof(UIManager)} before tooltip can be opened.");
        }

        return null;
    }
}
