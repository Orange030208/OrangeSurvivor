using UnityEngine;
using UnityEngine.EventSystems;

public class TooltipHoverTarget : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerMoveHandler, IPointerExitHandler
{
    [SerializeField] private MonoBehaviour tooltipDataSourceComponent;

    private ITooltipDataSource tooltipDataSource;
    private bool isPointerDown;

    private void Awake()
    {
        tooltipDataSource = tooltipDataSourceComponent as ITooltipDataSource;
    }

    public void SetTooltipDataSource(ITooltipDataSource source)
    {
        tooltipDataSource = source;
        tooltipDataSourceComponent = source as MonoBehaviour;
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
        if (tooltipDataSource == null)
        {
            return;
        }

        GameEventBus.Publish(new ShowTooltipRequestedEvent(tooltipDataSource.BuildTooltipData(), screenPosition));
    }

    private void Hide()
    {
        if (!isPointerDown)
        {
            return;
        }

        isPointerDown = false;
        GameEventBus.Publish<HideTooltipRequestedEvent>();
    }
}
