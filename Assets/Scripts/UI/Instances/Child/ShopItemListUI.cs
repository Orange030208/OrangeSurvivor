using System;
using System.Collections.Generic;
using DG.Tweening;
using Orange.UIFramework;
using UnityEngine;
using UnityEngine.UI;

public class ShopItemListUI : ViewPartBase
{
    private const float LAYOUT_MOVE_DURATION = 0.18f;

    [SerializeField] private ShopItemContainer itemPrefab;
    [SerializeField] private Transform itemParent;

    private readonly List<ShopItemContainer> renderedItems = new();
    private readonly List<ShopItemIdentity> renderedItemIdentities = new();
    private readonly Dictionary<ShopItemContainer, Tween> layoutMoveTweens = new();

    public event Action<int> BuyRequested;
    public event Action<int> LockToggleRequested;

    private void Awake()
    {
        ValidateConfiguration();
        itemParent.Clear();
    }

    private void OnDisable()
    {
        KillAllLayoutMoveTweens();
    }

    private void OnDestroy()
    {
        Clear();
        BuyRequested = null;
        LockToggleRequested = null;
    }

    public void Render(ShopItemData[] items, ShopRefreshReason reason)
    {
        if (items == null || items.Length == 0)
        {
            Clear();
            return;
        }

        List<ShopItemContainer> previousItems = new(renderedItems);
        List<ShopItemIdentity> previousIdentities = new(renderedItemIdentities);
        Dictionary<ShopItemContainer, Vector2> previousPositions = CaptureAnchoredPositions(previousItems);
        bool[] previousItemConsumed = new bool[previousItems.Count];
        List<LayoutMoveRequest> layoutMoveRequests = new();

        renderedItems.Clear();
        renderedItemIdentities.Clear();

        for (int i = 0; i < items.Length; i++)
        {
            RenderItem(
                items[i],
                i,
                reason,
                previousItems,
                previousIdentities,
                previousPositions,
                previousItemConsumed,
                layoutMoveRequests);
        }

        DestroyUnusedPreviousItems(previousItems, previousItemConsumed);
        PlayLayoutMoveAnimations(layoutMoveRequests);
    }

    public void Clear()
    {
        KillAllLayoutMoveTweens();
        for (int i = 0; i < renderedItems.Count; i++)
        {
            DestroyItem(renderedItems[i]);
        }

        renderedItems.Clear();
        renderedItemIdentities.Clear();
    }

    private void RenderItem(
        ShopItemData itemData,
        int itemIndex,
        ShopRefreshReason reason,
        List<ShopItemContainer> previousItems,
        List<ShopItemIdentity> previousIdentities,
        Dictionary<ShopItemContainer, Vector2> previousPositions,
        bool[] previousItemConsumed,
        List<LayoutMoveRequest> layoutMoveRequests)
    {
        if (itemData.ItemData == null)
        {
            Debug.LogWarning($"{nameof(ShopItemListUI)} on '{name}' skipped rendering a shop item without {nameof(ItemDataSO)}.", this);
            return;
        }

        ShopItemIdentity nextIdentity = ShopItemIdentity.From(itemData);
        int reusableItemIndex = FindReusableItemIndex(nextIdentity, previousItems, previousIdentities, previousItemConsumed);
        bool reusedExistingItem = reusableItemIndex >= 0;
        bool playReveal = ShouldPlayReveal(itemData, reason, reusedExistingItem);
        ShopItemContainer container = reusedExistingItem
            ? previousItems[reusableItemIndex]
            : CreateItem();

        if (reusedExistingItem)
        {
            previousItemConsumed[reusableItemIndex] = true;
        }

        container.transform.SetSiblingIndex(itemIndex);
        bool refreshMotion = !reusedExistingItem || playReveal;
        container.Configure(new InfoAddIndex<ShopItemData>(itemData, itemIndex), playReveal, refreshMotion);
        if (!playReveal
            && reusedExistingItem
            && previousPositions.TryGetValue(container, out Vector2 previousAnchoredPosition))
        {
            layoutMoveRequests.Add(new LayoutMoveRequest(container, previousAnchoredPosition));
        }

        renderedItems.Add(container);
        renderedItemIdentities.Add(nextIdentity);
    }

    private ShopItemContainer CreateItem()
    {
        ShopItemContainer container = Instantiate(itemPrefab, itemParent);
        BindItemCallbacks(container);
        return container;
    }

    private void DestroyUnusedPreviousItems(List<ShopItemContainer> previousItems, bool[] previousItemConsumed)
    {
        for (int i = 0; i < previousItems.Count; i++)
        {
            if (i < previousItemConsumed.Length && previousItemConsumed[i])
            {
                continue;
            }

            DestroyItem(previousItems[i]);
        }
    }

    private void DestroyItem(ShopItemContainer item)
    {
        if (item == null)
        {
            return;
        }

        KillLayoutMoveTween(item, complete: false);
        UnbindItemCallbacks(item);
        item.CleanUp();
        Destroy(item.gameObject);
    }

    private Dictionary<ShopItemContainer, Vector2> CaptureAnchoredPositions(List<ShopItemContainer> items)
    {
        Dictionary<ShopItemContainer, Vector2> positions = new();
        for (int i = 0; i < items.Count; i++)
        {
            ShopItemContainer item = items[i];
            RectTransform rectTransform = GetRectTransform(item);
            if (item == null || rectTransform == null)
            {
                continue;
            }

            positions[item] = rectTransform.anchoredPosition;
        }

        return positions;
    }

    private void PlayLayoutMoveAnimations(List<LayoutMoveRequest> layoutMoveRequests)
    {
        if (layoutMoveRequests.Count == 0)
        {
            return;
        }

        RectTransform parentRectTransform = itemParent as RectTransform;
        if (parentRectTransform == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(parentRectTransform);
        Canvas.ForceUpdateCanvases();

        for (int i = 0; i < layoutMoveRequests.Count; i++)
        {
            PlayLayoutMoveAnimation(layoutMoveRequests[i]);
        }
    }

    private void PlayLayoutMoveAnimation(LayoutMoveRequest request)
    {
        RectTransform rectTransform = GetRectTransform(request.Container);
        if (rectTransform == null)
        {
            return;
        }

        Vector2 targetAnchoredPosition = rectTransform.anchoredPosition;
        if ((targetAnchoredPosition - request.PreviousAnchoredPosition).sqrMagnitude < 0.01f)
        {
            return;
        }

        KillLayoutMoveTween(request.Container, complete: false);
        rectTransform.anchoredPosition = request.PreviousAnchoredPosition;
        Tween tween = rectTransform
            .DOAnchorPos(targetAnchoredPosition, LAYOUT_MOVE_DURATION)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true)
            .OnKill(() => layoutMoveTweens.Remove(request.Container));

        layoutMoveTweens[request.Container] = tween;
    }

    private void KillAllLayoutMoveTweens()
    {
        List<ShopItemContainer> items = new(layoutMoveTweens.Keys);
        for (int i = 0; i < items.Count; i++)
        {
            KillLayoutMoveTween(items[i], complete: false);
        }

        layoutMoveTweens.Clear();
    }

    private void KillLayoutMoveTween(ShopItemContainer item, bool complete)
    {
        if (item == null || !layoutMoveTweens.TryGetValue(item, out Tween tween))
        {
            return;
        }

        tween?.Kill(complete);
        layoutMoveTweens.Remove(item);
    }

    private int FindReusableItemIndex(
        ShopItemIdentity identity,
        List<ShopItemContainer> previousItems,
        List<ShopItemIdentity> previousIdentities,
        bool[] previousItemConsumed)
    {
        int count = Mathf.Min(previousItems.Count, previousIdentities.Count);
        for (int i = 0; i < count; i++)
        {
            if (previousItemConsumed[i] || previousItems[i] == null)
            {
                continue;
            }

            if (previousIdentities[i].Equals(identity))
            {
                return i;
            }
        }

        return -1;
    }

    private void BindItemCallbacks(ShopItemContainer container)
    {
        container.BuyRequested += OnItemBuyRequested;
        container.LockToggleRequested += OnItemLockToggleRequested;
    }

    private void UnbindItemCallbacks(ShopItemContainer container)
    {
        container.BuyRequested -= OnItemBuyRequested;
        container.LockToggleRequested -= OnItemLockToggleRequested;
    }

    private void OnItemBuyRequested(int itemIndex)
    {
        BuyRequested?.Invoke(itemIndex);
    }

    private void OnItemLockToggleRequested(int itemIndex)
    {
        LockToggleRequested?.Invoke(itemIndex);
    }

    private void ValidateConfiguration()
    {
        if (itemPrefab == null)
        {
            throw new MissingReferenceException($"{nameof(ShopItemListUI)} '{name}' is missing shop item prefab.");
        }

        if (itemParent == null)
        {
            throw new MissingReferenceException($"{nameof(ShopItemListUI)} '{name}' is missing item parent.");
        }
    }

    private static RectTransform GetRectTransform(ShopItemContainer item)
    {
        return item != null ? item.transform as RectTransform : null;
    }

    private static bool ShouldPlayReveal(
        ShopItemData itemData,
        ShopRefreshReason reason,
        bool reusedExistingItem)
    {
        if (reason == ShopRefreshReason.Reroll || reason == ShopRefreshReason.WaveRefresh)
        {
            return !itemData.Lock;
        }

        return !reusedExistingItem;
    }

    private readonly struct ShopItemIdentity : IEquatable<ShopItemIdentity>
    {
        private readonly ItemDataSO itemData;
        private readonly int level;

        private ShopItemIdentity(ItemDataSO itemData, int level)
        {
            this.itemData = itemData;
            this.level = level;
        }

        public static ShopItemIdentity From(ShopItemData itemData)
        {
            return new ShopItemIdentity(itemData.ItemData, itemData.Level);
        }

        public bool Equals(ShopItemIdentity other)
        {
            return itemData == other.itemData && level == other.level;
        }
    }

    private readonly struct LayoutMoveRequest
    {
        public readonly ShopItemContainer Container;
        public readonly Vector2 PreviousAnchoredPosition;

        public LayoutMoveRequest(ShopItemContainer container, Vector2 previousAnchoredPosition)
        {
            Container = container;
            PreviousAnchoredPosition = previousAnchoredPosition;
        }
    }
}
