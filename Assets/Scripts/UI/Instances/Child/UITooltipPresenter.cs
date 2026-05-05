using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UITooltipPresenter : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private ExtraInfoDescriber extraInfoDescriber;
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

        if (root == null)
        {
            throw new MissingComponentException($"{nameof(UITooltipPresenter)} '{name}' requires a RectTransform root.");
        }

        if (canvasGroup == null)
        {
            throw new MissingReferenceException($"{nameof(UITooltipPresenter)} '{name}' is missing CanvasGroup.");
        }

        if (iconImage == null)
        {
            throw new MissingReferenceException($"{nameof(UITooltipPresenter)} '{name}' is missing icon image.");
        }

        if (titleText == null)
        {
            throw new MissingReferenceException($"{nameof(UITooltipPresenter)} '{name}' is missing title text.");
        }

        if (extraInfoDescriber == null)
        {
            throw new MissingReferenceException($"{nameof(UITooltipPresenter)} '{name}' is missing description list displayer.");
        }

        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            uiCamera = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera;
        }
    }

    private void OnEnable()
    {
        HideImmediate();
    }

    public void Present(IDescribable describable)
    {
        ApplyDocument(describable);
        SetVisible(true);
    }

    public void Present(IDescribable describable, Vector2 screenPosition)
    {
        Present(describable);
        SetScreenPosition(screenPosition);
    }

    private void ApplyDocument(IDescribable document)
    {
        titleText.text = document != null ? document.Title : string.Empty;
        iconImage.sprite = document != null ? document.Icon : null;
        iconImage.enabled = document != null && document.Icon != null;
        extraInfoDescriber.Display(document);
        LayoutRebuilder.ForceRebuildLayoutImmediate(root);
    }

    private void SetScreenPosition(Vector2 screenPosition)
    {
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
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    public void HideImmediate()
    {
        SetVisible(false);
    }
}
