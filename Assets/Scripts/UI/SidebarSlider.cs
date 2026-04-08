using System;
using DG.Tweening;
using UnityEngine;

public enum SidebarType
{
    Left,
    Right,
    Top,
    Bottom
}

public class SidebarSlider : MonoBehaviour
{
    [Header("基础")]
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private SidebarType sidebarType = SidebarType.Left;

    [Header("动画")]
    [SerializeField] private float slideDuration = 0.25f;
    [SerializeField] private Ease slideEase = Ease.OutCubic;
    [SerializeField] private float extraHideOffset = 0f;
    [SerializeField] private bool setInactiveOnHide = true;

    public event Action<SidebarSlider> OnShowStarted;
    public event Action<SidebarSlider> OnShowCompleted;
    public event Action<SidebarSlider> OnHideStarted;
    public event Action<SidebarSlider> OnHideCompleted;

    public bool IsShown { get; private set; }

    private Vector2 shownPos;
    private Vector2 hiddenPos;
    private Tween slideTween;
    
    private bool posCached = false;

    private void Awake()
    {
        if (panelRect == null)
        {
            panelRect = GetComponent<RectTransform>();
        }

        CachePositionsByCurrentState();
    }

    private void OnDestroy()
    {
        KillTween();
        ClearEvents();
    }

    public void CachePositionsByCurrentState()
    {
        if (panelRect == null || posCached)
        {
            return;
        }

        shownPos = panelRect.anchoredPosition;

        float panelWidth = panelRect.rect.width;
        if (panelWidth <= 0f)
        {
            panelWidth = Mathf.Abs(panelRect.sizeDelta.x);
        }

        float panelHeight = panelRect.rect.height;
        if (panelHeight <= 0f)
        {
            panelHeight = Mathf.Abs(panelRect.sizeDelta.y);
        }

        float hideDistance;
        Vector2 moveDir;

        switch (sidebarType)
        {
            case SidebarType.Right:
                hideDistance = panelWidth + extraHideOffset;
                moveDir = Vector2.right;
                break;
            case SidebarType.Top:
                hideDistance = panelHeight + extraHideOffset;
                moveDir = Vector2.up;
                break;
            case SidebarType.Bottom:
                hideDistance = panelHeight + extraHideOffset;
                moveDir = Vector2.down;
                break;
            default:
                hideDistance = panelWidth + extraHideOffset;
                moveDir = Vector2.left;
                break;
        }

        hiddenPos = shownPos + moveDir * hideDistance;
        posCached = true;
    }

    public void Show()
    {
        if (panelRect == null)
        {
            return;
        }

        KillTween();
        panelRect.gameObject.SetActive(true);

        OnShowStarted?.Invoke(this);

        slideTween = panelRect
            .DOAnchorPos(shownPos, slideDuration)
            .SetEase(slideEase)
            .OnComplete(() =>
            {
                IsShown = true;
                OnShowCompleted?.Invoke(this);
            });
    }

    public void Hide()
    {
        if (panelRect == null)
        {
            return;
        }
        
        KillTween();
        
        CachePositionsByCurrentState();

        OnHideStarted?.Invoke(this);

        slideTween = panelRect
            .DOAnchorPos(hiddenPos, slideDuration)
            .SetEase(slideEase)
            .OnComplete(() =>
            {
                IsShown = false;
                if (setInactiveOnHide)
                {
                    panelRect.gameObject.SetActive(false);
                }

                OnHideCompleted?.Invoke(this);
            });
    }

    public void HideImmediate()
    {
        if (panelRect == null)
        {
            return;
        }
        
        KillTween();
        
        CachePositionsByCurrentState();
        
        panelRect.anchoredPosition = hiddenPos;
        if (setInactiveOnHide)
        {
            panelRect.gameObject.SetActive(false);
        }

        IsShown = false;
    }

    public void KillTween()
    {
        slideTween?.Kill();
        slideTween = null;
    }

    public void ClearEvents()
    {
        OnShowStarted = null;
        OnShowCompleted = null;
        OnHideStarted = null;
        OnHideCompleted = null;
    }
}
