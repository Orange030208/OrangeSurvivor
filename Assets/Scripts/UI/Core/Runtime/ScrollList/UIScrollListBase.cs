using System;
using System.Collections.Generic;
using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;

public abstract class UIScrollListBase<TItem, TData> : MonoBehaviour
    where TItem : UIScrollListItemBase
{
    private static readonly Vector2 CONTENT_ANCHOR = new(0f, 1f);
    private static readonly Vector2 HORIZONTAL_ITEM_ANCHOR = new(0f, 0.5f);
    private static readonly Vector2 VERTICAL_ITEM_ANCHOR = new(0.5f, 1f);
    private const float MIN_LAYOUT_SIZE = 0.01f;

    [SerializeField] private TItem itemPrefab;
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private UIScrollListLayoutConfig layoutConfig = new();
    [SerializeField] private UIScrollListRevealConfig revealConfig = new();

    private readonly List<TItem> activeItems = new();
    private readonly HashSet<int> invalidSizeWarnings = new();
    private Sequence revealSequence;

    public IReadOnlyList<TItem> ActiveItems => activeItems;
    protected RectTransform ContentRoot => contentRoot;
    protected ScrollRect ScrollRect => scrollRect;

    protected virtual void Awake()
    {
        CacheExistingItems();
    }

    protected virtual void OnDestroy()
    {
        KillRevealSequence();
    }

    public void Render(IReadOnlyList<TData> dataList)
    {
        CacheExistingItems();

        int targetCount = dataList?.Count ?? 0;
        EnsureItemCount(targetCount);

        for (int i = 0; i < activeItems.Count; i++)
        {
            TItem item = activeItems[i];
            bool shouldShow = i < targetCount;
            item.SetVisible(shouldShow);
            if (!shouldShow)
            {
                continue;
            }

            BindItem(item, dataList[i], i);
        }

        Relayout();

        if (revealConfig.PlayOnRefresh)
        {
            PlayReveal();
        }
    }

    [Button("清空")]
    public void Clear()
    {
        KillRevealSequence();
        for (int i = 0; i < activeItems.Count; i++)
        {
            if (activeItems[i] != null)
            {
                Destroy(activeItems[i].gameObject);
            }
        }

        activeItems.Clear();
        invalidSizeWarnings.Clear();
        EnsureContentRootLayout();
        UpdateContentSize();
        ResetScrollPosition();
    }

    [Button("重新布局")]
    public void Relayout()
    {
        CacheExistingItems();
        EnsureContentRootLayout();
        ApplyChildLayoutSizeOverrides();
        UpdateContentSize();
        ApplyItemPositions();
        RefreshPresentation();
    }

    [Button("播放动画")]
    public void PlayReveal()
    {
        CacheExistingItems();
        KillRevealSequence();
        revealSequence = DOTween.Sequence().SetUpdate(revealConfig.UseUnscaledTime).SetEase(revealConfig.SequenceEase);
        if (revealConfig.StartDelay > 0f)
        {
            revealSequence.AppendInterval(revealConfig.StartDelay);
        }

        int visibleOrder = 0;
        for (int i = 0; i < activeItems.Count; i++)
        {
            TItem item = activeItems[i];
            if (item == null || !item.gameObject.activeSelf)
            {
                continue;
            }

            item.KillRevealMotion();
            item.SetRevealImmediate(revealConfig.InitialClipId);
            float itemDelay = revealConfig.PlayTogether ? visibleOrder * revealConfig.ItemStagger : 0f;
            Tween tween = item.PlayReveal(revealConfig.RevealClipId, itemDelay);
            if (tween == null)
            {
                item.SetRevealImmediate(UIMotionClipIds.VISIBLE);
                visibleOrder++;
                continue;
            }

            if (revealConfig.PlayTogether)
            {
                revealSequence.Join(tween);
            }
            else
            {
                revealSequence.Append(tween);
                if (revealConfig.ItemStagger > 0f)
                {
                    revealSequence.AppendInterval(revealConfig.ItemStagger);
                }
            }

            visibleOrder++;
        }
    }

    protected abstract void BindItem(TItem item, TData data, int index);

    protected virtual void ResetScrollPosition()
    {
        if (scrollRect == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        if (scrollRect.vertical)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }

        if (scrollRect.horizontal)
        {
            scrollRect.horizontalNormalizedPosition = 0f;
        }
    }

    protected virtual void OnCreateItem(TItem item, int index)
    {
    }

    protected void ClearItemsImmediate()
    {
        KillRevealSequence();
        for (int i = activeItems.Count - 1; i >= 0; i--)
        {
            TItem item = activeItems[i];
            if (item != null)
            {
                DestroyImmediate(item.gameObject);
            }
        }

        activeItems.Clear();
        invalidSizeWarnings.Clear();
        EnsureContentRootLayout();
        UpdateContentSize();
    }

    private void EnsureItemCount(int targetCount)
    {
        while (activeItems.Count < targetCount)
        {
            TItem item = Instantiate(itemPrefab, contentRoot);
            activeItems.Add(item);
            OnCreateItem(item, activeItems.Count - 1);
        }

        for (int i = 0; i < activeItems.Count; i++)
        {
            TItem item = activeItems[i];
            if (item != null)
            {
                item.SetVisible(i < targetCount);
            }
        }
    }

    private void CacheExistingItems()
    {
        if (contentRoot == null)
        {
            return;
        }

        for (int i = activeItems.Count - 1; i >= 0; i--)
        {
            if (activeItems[i] == null)
            {
                activeItems.RemoveAt(i);
            }
        }

        for (int i = 0; i < contentRoot.childCount; i++)
        {
            TItem item = contentRoot.GetChild(i).GetComponent<TItem>();
            if (item != null && !activeItems.Contains(item))
            {
                activeItems.Add(item);
            }
        }
    }

    private void RefreshPresentation()
    {
        Canvas.ForceUpdateCanvases();

        for (int i = 0; i < activeItems.Count; i++)
        {
            activeItems[i]?.RefreshPresentation();
        }

        ResetScrollPosition();
    }

    private void EnsureContentRootLayout()
    {
        if (contentRoot == null)
        {
            return;
        }

        contentRoot.anchorMin = CONTENT_ANCHOR;
        contentRoot.anchorMax = CONTENT_ANCHOR;
        contentRoot.pivot = CONTENT_ANCHOR;
    }

    private void ApplyChildLayoutSizeOverrides()
    {
        for (int i = 0; i < activeItems.Count; i++)
        {
            TItem item = activeItems[i];
            if (item == null || !item.gameObject.activeSelf)
            {
                continue;
            }

            RectTransform itemRect = item.ItemRectTransform;
            EnsureItemLayout(itemRect);

            Vector2 currentSize = item.GetLayoutSize();
            float width = layoutConfig.OverrideChildWidth ? layoutConfig.ChildWidth : currentSize.x;
            float height = layoutConfig.OverrideChildHeight ? layoutConfig.ChildHeight : currentSize.y;
            item.SetLayoutSize(new Vector2(width, height));
        }

        Canvas.ForceUpdateCanvases();
    }

    private void ApplyItemPositions()
    {
        List<TItem> orderedItems = GetOrderedVisibleItems();
        if (orderedItems.Count <= 0 || contentRoot == null)
        {
            return;
        }

        if (layoutConfig.Direction == UIScrollListDirection.Horizontal)
        {
            ApplyHorizontalPositions(orderedItems);
            return;
        }

        ApplyVerticalPositions(orderedItems);
    }

    private void ApplyHorizontalPositions(List<TItem> orderedItems)
    {
        float availableMainAxisSize = GetAvailableHorizontalMainAxisSize();
        float occupiedMainAxisSize = GetOccupiedHorizontalMainAxisSize(orderedItems);
        float cursor = GetHorizontalMainAxisStart(availableMainAxisSize, occupiedMainAxisSize);

        for (int i = 0; i < orderedItems.Count; i++)
        {
            TItem item = orderedItems[i];
            RectTransform itemRect = item.ItemRectTransform;
            EnsureItemLayout(itemRect);

            Vector2 layoutSize = GetValidatedLayoutSize(item, i);
            float y = GetHorizontalCrossAxisPosition(layoutSize.y);
            itemRect.anchoredPosition = new Vector2(cursor, y);
            cursor += layoutSize.x + layoutConfig.Spacing;
        }
    }

    private void ApplyVerticalPositions(List<TItem> orderedItems)
    {
        float availableMainAxisSize = GetAvailableVerticalMainAxisSize();
        float occupiedMainAxisSize = GetOccupiedVerticalMainAxisSize(orderedItems);
        float cursor = GetVerticalMainAxisStart(availableMainAxisSize, occupiedMainAxisSize);

        for (int i = 0; i < orderedItems.Count; i++)
        {
            TItem item = orderedItems[i];
            RectTransform itemRect = item.ItemRectTransform;
            EnsureItemLayout(itemRect);

            Vector2 layoutSize = GetValidatedLayoutSize(item, i);
            float x = GetVerticalCrossAxisPosition(layoutSize.x);
            itemRect.anchoredPosition = new Vector2(x, cursor);
            cursor -= layoutSize.y + layoutConfig.Spacing;
        }
    }

    private void EnsureItemLayout(RectTransform itemRect)
    {
        if (itemRect == null)
        {
            return;
        }

        Vector2 itemAnchor = layoutConfig.Direction == UIScrollListDirection.Horizontal
            ? HORIZONTAL_ITEM_ANCHOR
            : VERTICAL_ITEM_ANCHOR;
        itemRect.anchorMin = itemAnchor;
        itemRect.anchorMax = itemAnchor;
        itemRect.pivot = itemAnchor;
    }

    private void UpdateContentSize()
    {
        if (contentRoot == null)
        {
            return;
        }

        float mainAxisSize = 0f;
        float crossAxisMax = 0f;
        int visibleCount = 0;

        for (int i = 0; i < activeItems.Count; i++)
        {
            TItem item = activeItems[i];
            if (item == null || !item.gameObject.activeSelf)
            {
                continue;
            }

            Vector2 layoutSize = GetValidatedLayoutSize(item, i);
            float mainAxisItemSize = layoutConfig.Direction == UIScrollListDirection.Horizontal ? layoutSize.x : layoutSize.y;
            float crossAxisItemSize = layoutConfig.Direction == UIScrollListDirection.Horizontal ? layoutSize.y : layoutSize.x;
            mainAxisSize += mainAxisItemSize;
            crossAxisMax = Mathf.Max(crossAxisMax, crossAxisItemSize);
            visibleCount++;
        }

        if (visibleCount > 1)
        {
            mainAxisSize += layoutConfig.Spacing * (visibleCount - 1);
        }

        Vector2 viewportSize = GetViewportSize();
        if (layoutConfig.Direction == UIScrollListDirection.Horizontal)
        {
            float requiredWidth = layoutConfig.PaddingLeft + mainAxisSize + layoutConfig.PaddingRight;
            float requiredHeight = layoutConfig.PaddingTop + crossAxisMax + layoutConfig.PaddingBottom;
            float width = Mathf.Max(requiredWidth, viewportSize.x);
            float height = Mathf.Max(requiredHeight, viewportSize.y);
            contentRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            contentRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            return;
        }

        float requiredVerticalWidth = layoutConfig.PaddingLeft + crossAxisMax + layoutConfig.PaddingRight;
        float requiredVerticalHeight = layoutConfig.PaddingTop + mainAxisSize + layoutConfig.PaddingBottom;
        float verticalWidth = Mathf.Max(requiredVerticalWidth, viewportSize.x);
        float verticalHeight = Mathf.Max(requiredVerticalHeight, viewportSize.y);
        contentRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, verticalWidth);
        contentRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, verticalHeight);
    }

    private List<TItem> GetOrderedVisibleItems()
    {
        List<TItem> orderedItems = new();
        if (layoutConfig.ReverseOrder)
        {
            for (int i = activeItems.Count - 1; i >= 0; i--)
            {
                TItem item = activeItems[i];
                if (item != null && item.gameObject.activeSelf)
                {
                    orderedItems.Add(item);
                }
            }

            return orderedItems;
        }

        for (int i = 0; i < activeItems.Count; i++)
        {
            TItem item = activeItems[i];
            if (item != null && item.gameObject.activeSelf)
            {
                orderedItems.Add(item);
            }
        }

        return orderedItems;
    }

    private Vector2 GetViewportSize()
    {
        if (scrollRect == null || scrollRect.viewport == null)
        {
            return contentRoot.rect.size;
        }

        return scrollRect.viewport.rect.size;
    }

    private float GetAvailableHorizontalMainAxisSize()
    {
        return GetViewportSize().x;
    }

    private float GetAvailableVerticalMainAxisSize()
    {
        return GetViewportSize().y;
    }

    private float GetOccupiedHorizontalMainAxisSize(List<TItem> orderedItems)
    {
        float totalSize = 0f;
        for (int i = 0; i < orderedItems.Count; i++)
        {
            Vector2 layoutSize = GetValidatedLayoutSize(orderedItems[i], i);
            totalSize += layoutSize.x;
        }

        if (orderedItems.Count > 1)
        {
            totalSize += layoutConfig.Spacing * (orderedItems.Count - 1);
        }

        return totalSize;
    }

    private float GetOccupiedVerticalMainAxisSize(List<TItem> orderedItems)
    {
        float totalSize = 0f;
        for (int i = 0; i < orderedItems.Count; i++)
        {
            Vector2 layoutSize = GetValidatedLayoutSize(orderedItems[i], i);
            totalSize += layoutSize.y;
        }

        if (orderedItems.Count > 1)
        {
            totalSize += layoutConfig.Spacing * (orderedItems.Count - 1);
        }

        return totalSize;
    }

    private float GetHorizontalMainAxisStart(float availableMainAxisSize, float occupiedMainAxisSize)
    {
        float start = layoutConfig.PaddingLeft;
        float availableInnerSize = Mathf.Max(0f, availableMainAxisSize - layoutConfig.PaddingLeft - layoutConfig.PaddingRight);
        float remainingSize = Mathf.Max(0f, availableInnerSize - occupiedMainAxisSize);
        return start + GetStartOffset(remainingSize, layoutConfig.MainAxisAlignment);
    }

    private float GetVerticalMainAxisStart(float availableMainAxisSize, float occupiedMainAxisSize)
    {
        float start = -layoutConfig.PaddingTop;
        float availableInnerSize = Mathf.Max(0f, availableMainAxisSize - layoutConfig.PaddingTop - layoutConfig.PaddingBottom);
        float remainingSize = Mathf.Max(0f, availableInnerSize - occupiedMainAxisSize);
        return start - GetStartOffset(remainingSize, layoutConfig.MainAxisAlignment);
    }

    private float GetHorizontalCrossAxisPosition(float itemHeight)
    {
        float availableCrossAxisSize = contentRoot.rect.height;
        float availableInnerSize = Mathf.Max(0f, availableCrossAxisSize - layoutConfig.PaddingTop - layoutConfig.PaddingBottom);
        float remainingSize = Mathf.Max(0f, availableInnerSize - itemHeight);
        float offset = GetStartOffset(remainingSize, layoutConfig.CrossAxisAlignment);
        float pivotY = -layoutConfig.PaddingTop - offset - (itemHeight * HORIZONTAL_ITEM_ANCHOR.y);
        float anchorReferenceY = -availableCrossAxisSize * (1f - HORIZONTAL_ITEM_ANCHOR.y);
        return pivotY - anchorReferenceY;
    }

    private float GetVerticalCrossAxisPosition(float itemWidth)
    {
        float availableCrossAxisSize = contentRoot.rect.width;
        float availableInnerSize = Mathf.Max(0f, availableCrossAxisSize - layoutConfig.PaddingLeft - layoutConfig.PaddingRight);
        float remainingSize = Mathf.Max(0f, availableInnerSize - itemWidth);
        float offset = GetStartOffset(remainingSize, layoutConfig.CrossAxisAlignment);
        float pivotX = layoutConfig.PaddingLeft + offset + (itemWidth * VERTICAL_ITEM_ANCHOR.x);
        float anchorReferenceX = availableCrossAxisSize * VERTICAL_ITEM_ANCHOR.x;
        return pivotX - anchorReferenceX;
    }

    private float GetStartOffset(float remainingSize, UIScrollListAlignment alignment)
    {
        return alignment switch
        {
            UIScrollListAlignment.Center => remainingSize * 0.5f,
            UIScrollListAlignment.End => remainingSize,
            _ => 0f
        };
    }

    private Vector2 GetValidatedLayoutSize(TItem item, int index)
    {
        Vector2 layoutSize = item.GetLayoutSize();
        if (layoutSize.x >= MIN_LAYOUT_SIZE && layoutSize.y >= MIN_LAYOUT_SIZE)
        {
            return layoutSize;
        }

        if (invalidSizeWarnings.Add(index))
        {
            Debug.LogWarning(
                $"{GetType().Name} item '{item.name}' has invalid layout size {layoutSize}. " +
                "Manual scroll list requires each item root RectTransform to have a concrete non-zero size.",
                item);
        }

        return new Vector2(Mathf.Max(layoutSize.x, MIN_LAYOUT_SIZE), Mathf.Max(layoutSize.y, MIN_LAYOUT_SIZE));
    }

    private void KillRevealSequence()
    {
        revealSequence?.Kill();
        revealSequence = null;
    }
}
