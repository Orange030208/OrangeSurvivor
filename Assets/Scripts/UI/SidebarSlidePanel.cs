using System;
using DG.Tweening;
using UnityEngine;

public enum SidebarType
{
    Left,
    Right
}

public class SidebarSlidePanel : MonoBehaviour
{
    [Header("基础")]
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private SidebarType sidebarType = SidebarType.Left;

    [Header("动画")]
    [SerializeField] private float slideDuration = 0.25f;
    [SerializeField] private Ease slideEase = Ease.OutCubic;
    [SerializeField] private float extraHideOffset = 0f;

    public event Action<SidebarSlidePanel> OnShowStarted;
    public event Action<SidebarSlidePanel> OnShowCompleted;
    public event Action<SidebarSlidePanel> OnHideStarted;
    public event Action<SidebarSlidePanel> OnHideCompleted;

    public bool IsShown { get; private set; }

    private Vector2 shownPos;
    private Vector2 hiddenPos;
    private Tween slideTween;

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
        if (panelRect == null)
        {
            return;
        }

        shownPos = panelRect.anchoredPosition;

        float panelWidth = panelRect.rect.width;
        if (panelWidth <= 0f)
        {
            panelWidth = Mathf.Abs(panelRect.sizeDelta.x);
        }

        float hideDistance = panelWidth + extraHideOffset;
        float direction = sidebarType == SidebarType.Right ? 1f : -1f;
        hiddenPos = shownPos + new Vector2(direction * hideDistance, 0f);
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

        OnHideStarted?.Invoke(this);

        slideTween = panelRect
            .DOAnchorPos(hiddenPos, slideDuration)
            .SetEase(slideEase)
            .OnComplete(() =>
            {
                IsShown = false;
                panelRect.gameObject.SetActive(false);
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
        panelRect.anchoredPosition = hiddenPos;
        panelRect.gameObject.SetActive(false);
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
