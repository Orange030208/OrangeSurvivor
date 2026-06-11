
namespace Orange.UIFramework
{
    using UnityEngine;
using UnityEngine.UI;

public sealed class UIMotionTargetSnapshot
{
    public UIMotionTargetSnapshot(Transform transform)
    {
        // 快照只记录 Track 需要回到的初始 UI 状态，不持有业务数据。
        // Initial/InitialPlusOffset 模式都依赖这里捕获的值来避免硬编码 Prefab 坐标。
        Transform = transform;
        RectTransform = transform as RectTransform;
        CanvasGroup = transform != null ? transform.GetComponent<CanvasGroup>() : null;
        Graphic = transform != null ? transform.GetComponent<Graphic>() : null;
        Image = transform != null ? transform.GetComponent<Image>() : null;
        if (RectTransform != null)
        {
            AnchoredPosition = RectTransform.anchoredPosition;
            LocalScale = RectTransform.localScale;
            LocalEulerAngles = RectTransform.localEulerAngles;
        }

        if (CanvasGroup != null)
        {
            CanvasAlpha = CanvasGroup.alpha;
        }

        if (Graphic != null)
        {
            GraphicColor = Graphic.color;
        }

        if (Image != null)
        {
            ImageFillAmount = Image.fillAmount;
            Sprite = Image.sprite;
        }

    }

    public Transform Transform { get; }
    public RectTransform RectTransform { get; }
    public CanvasGroup CanvasGroup { get; }
    public Graphic Graphic { get; }
    public Image Image { get; }
    public Vector2 AnchoredPosition { get; }
    public Vector3 LocalScale { get; }
    public Vector3 LocalEulerAngles { get; }
    public float CanvasAlpha { get; }
    public Color GraphicColor { get; }
    public float ImageFillAmount { get; }
    public Sprite Sprite { get; }
}
}
