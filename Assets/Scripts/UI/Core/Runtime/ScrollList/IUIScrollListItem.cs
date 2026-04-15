using UnityEngine;

public interface IUIScrollListItem
{
    RectTransform ItemRectTransform { get; }
    GameObject ItemGameObject { get; }
    void SetVisible(bool visible);
    void RefreshPresentation();
    Vector2 GetLayoutSize();
}
