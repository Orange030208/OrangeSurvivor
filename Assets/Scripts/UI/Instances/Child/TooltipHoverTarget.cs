using UnityEngine;
using UnityEngine.EventSystems;

public class TooltipHoverTarget : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerMoveHandler, IPointerExitHandler
{
    [SerializeField] private MonoBehaviour dataSourceComponent;
    [SerializeField] private UITooltipPresenter tooltipPresenter;

    private IDescribable dataSource;
    private bool isPointerDown;

    private void Awake()
    {
        ValidateConfiguration();
        dataSource = (IDescribable)dataSourceComponent;
    }

    public void SetDataSource(IDescribable source)
    {
        dataSource = source;
        dataSourceComponent = source as MonoBehaviour;
    }

    public void SetTooltipPresenter(UITooltipPresenter presenter)
    {
        tooltipPresenter = presenter;
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
        ResolveTooltipPresenter(true)?.Present(dataSource, screenPosition);
    }

    private void ValidateConfiguration()
    {
        if (dataSourceComponent == null)
        {
            throw new MissingReferenceException($"{nameof(TooltipHoverTarget)} '{name}' is missing tooltip data source component.");
        }

        if (dataSourceComponent is not IDescribable)
        {
            throw new MissingComponentException($"{nameof(TooltipHoverTarget)} '{name}' requires a component implementing {nameof(IDescribable)}.");
        }
    }

    private void Hide()
    {
        if (!isPointerDown)
        {
            return;
        }

        isPointerDown = false;
        ResolveTooltipPresenter(false)?.HideImmediate();
    }

    private UITooltipPresenter ResolveTooltipPresenter(bool throwIfMissing)
    {
        if (tooltipPresenter != null)
        {
            return tooltipPresenter;
        }

        tooltipPresenter = GetComponentInParent<UITooltipPresenter>(true);
        if (tooltipPresenter != null)
        {
            return tooltipPresenter;
        }

        if (throwIfMissing)
        {
            throw new MissingReferenceException($"{nameof(TooltipHoverTarget)} '{name}' requires an explicit {nameof(UITooltipPresenter)} reference.");
        }

        return null;
    }
}
