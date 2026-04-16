using System;
using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public abstract class UIScrollListItemBase : MonoBehaviour, IUIScrollListItem
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private UIRuntimeMotionBase runtimeMotionBehaviour;

    private RectTransform itemRectTransform;
    private UIRuntimeMotionBase runtimeMotion;

    public RectTransform ItemRectTransform => itemRectTransform;
    public GameObject ItemGameObject => gameObject;
    protected UIRuntimeMotionBase RuntimeMotion => runtimeMotion;

    protected virtual void Awake()
    {
        ResolveReferences();
    }

    public void SetVisible(bool visible)
    {
        ResolveReferences();
        gameObject.SetActive(visible);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }
    }

    public void RefreshPresentation()
    {
        ResolveReferences();
        runtimeMotion?.RefreshDefaults();
        OnPresentationRefreshed();
    }

    public Vector2 GetLayoutSize()
    {
        ResolveReferences();
        return itemRectTransform.rect.size;
    }

    public void SetLayoutSize(Vector2 layoutSize)
    {
        ResolveReferences();
        itemRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, layoutSize.x);
        itemRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, layoutSize.y);
    }

    public Tween PlayReveal(UIMotionAction action, float delay)
    {
        ResolveReferences();
        return runtimeMotion?.Play(action, delay);
    }

    public void SetRevealImmediate(UIMotionAction action)
    {
        ResolveReferences();
        runtimeMotion?.SetImmediate(action);
    }

    public void KillRevealMotion()
    {
        ResolveReferences();
        runtimeMotion?.Kill();
    }

    protected virtual void OnPresentationRefreshed()
    {
    }

    private void ResolveReferences()
    {
        itemRectTransform ??= GetComponent<RectTransform>();
        canvasGroup ??= GetComponent<CanvasGroup>();

        if (runtimeMotion == null)
        {
            runtimeMotion = runtimeMotionBehaviour;
            if (runtimeMotion == null && runtimeMotionBehaviour != null)
            {
                runtimeMotion = runtimeMotionBehaviour.GetComponent<UIRuntimeMotionBase>();
            }

            runtimeMotion ??= GetComponent<UIRuntimeMotionBase>();
        }
    }
}
