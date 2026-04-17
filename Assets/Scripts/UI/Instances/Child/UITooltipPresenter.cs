using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UITooltipPresenter : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI footerText;
    [SerializeField] private DescriptionListDisplayer descriptionListDisplayer;
    [SerializeField] private Vector2 screenOffset = new(18f, -18f);
    [SerializeField] private Vector2 screenPadding = new(12f, 12f);

    private Canvas parentCanvas;
    private Camera uiCamera;

    private void Awake()
    {
        if (root == null)
        {
            root = transform as RectTransform;
        }

        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            uiCamera = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera;
        }
    }

    private void OnEnable()
    {
        GameEventBus.Subscribe<ShowTooltipRequestedEvent>(OnShowTooltipRequested);
        GameEventBus.Subscribe<HideTooltipRequestedEvent>(OnHideTooltipRequested);
        HideImmediate();
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<ShowTooltipRequestedEvent>(OnShowTooltipRequested);
        GameEventBus.Unsubscribe<HideTooltipRequestedEvent>(OnHideTooltipRequested);
    }

    private void OnShowTooltipRequested(ShowTooltipRequestedEvent eventData)
    {
        ApplyData(eventData.Data);
        SetVisible(true);
        SetScreenPosition(eventData.ScreenPosition);
    }

    private void OnHideTooltipRequested(HideTooltipRequestedEvent _)
    {
        HideImmediate();
    }

    private void ApplyData(TooltipDisplayData data)
    {
        if (titleText != null)
        {
            titleText.text = data.Title;
        }

        if (footerText != null)
        {
            footerText.text = data.Footer;
            footerText.gameObject.SetActive(!string.IsNullOrWhiteSpace(data.Footer));
        }

        if (iconImage != null)
        {
            iconImage.sprite = data.Icon;
            iconImage.enabled = data.Icon != null;
        }

        descriptionListDisplayer?.DisplayDescriptions(data.Descriptions);
        LayoutRebuilder.ForceRebuildLayoutImmediate(root);
    }

    private void SetScreenPosition(Vector2 screenPosition)
    {
        if (root == null)
        {
            return;
        }

        RectTransform parentRect = root.parent as RectTransform;
        if (parentRect == null)
        {
            root.position = screenPosition + screenOffset;
            return;
        }

        Vector2 desiredScreenPosition = screenPosition + screenOffset;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, desiredScreenPosition, uiCamera, out Vector2 localPoint))
        {
            return;
        }

        Vector2 clampedLocalPosition = ClampToParent(parentRect, localPoint);
        Vector3 currentLocalPosition = root.localPosition;
        root.localPosition = new Vector3(clampedLocalPosition.x, clampedLocalPosition.y, currentLocalPosition.z);
    }

    private Vector2 ClampToParent(RectTransform parentRect, Vector2 localPosition)
    {
        Vector2 size = root.rect.size;
        Vector2 pivot = root.pivot;
        Rect parentRectValue = parentRect.rect;

        float minX = parentRectValue.xMin + screenPadding.x + size.x * pivot.x;
        float maxX = parentRectValue.xMax - screenPadding.x - size.x * (1f - pivot.x);
        float minY = parentRectValue.yMin + screenPadding.y + size.y * pivot.y;
        float maxY = parentRectValue.yMax - screenPadding.y - size.y * (1f - pivot.y);

        return new Vector2(
            Mathf.Clamp(localPosition.x, minX, maxX),
            Mathf.Clamp(localPosition.y, minY, maxY));
    }

    private void SetVisible(bool visible)
    {
        canvasGroup.alpha = visible ? 1f : 0f;
    }

    private void HideImmediate()
    {
        SetVisible(false);
    }
}
