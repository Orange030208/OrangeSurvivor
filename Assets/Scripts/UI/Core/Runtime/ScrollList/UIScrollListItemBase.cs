using System;
using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public abstract class UIScrollListItemBase : MonoBehaviour, IUIScrollListItem
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private MonoBehaviour runtimeMotionBehaviour;

    private RectTransform itemRectTransform;
    private IUIRuntimeMotion runtimeMotion;

    public RectTransform ItemRectTransform => itemRectTransform;
    public GameObject ItemGameObject => gameObject;
    protected IUIRuntimeMotion RuntimeMotion => runtimeMotion;

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

    public Tween PlayReveal(string clipId, float delay)
    {
        ResolveReferences();
        return runtimeMotion?.Play(clipId, delay);
    }

    public void SetRevealImmediate(string clipId)
    {
        ResolveReferences();
        runtimeMotion?.SetImmediate(clipId);
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
            runtimeMotion = ResolveRuntimeMotion(runtimeMotionBehaviour);
            runtimeMotion ??= ResolveRuntimeMotion(this);
        }
    }

    private static IUIRuntimeMotion ResolveRuntimeMotion(MonoBehaviour source)
    {
        if (source == null)
        {
            return null;
        }

        if (source is IUIRuntimeMotion directMotion)
        {
            return directMotion;
        }

        MonoBehaviour[] behaviours = source.GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IUIRuntimeMotion motion)
            {
                return motion;
            }
        }

        return null;
    }
}
