using UnityEngine.Scripting.APIUpdating;

namespace AXR.Framework.UI
{
    using System;
using UnityEngine;

[Serializable]
public sealed class UIScrollListLayoutConfig
{
    [SerializeField] private UIScrollListDirection direction = UIScrollListDirection.Vertical;
    [SerializeField] private bool reverseOrder;
    [SerializeField] private UIScrollListAlignment mainAxisAlignment = UIScrollListAlignment.Start;
    [SerializeField] private UIScrollListAlignment crossAxisAlignment = UIScrollListAlignment.Center;
    [SerializeField] [Min(0f)] private float spacing = 16f;
    [SerializeField] private bool overrideChildWidth;
    [SerializeField] [Min(0f)] private float childWidth = 120f;
    [SerializeField] private bool overrideChildHeight;
    [SerializeField] [Min(0f)] private float childHeight = 120f;
    [SerializeField] [Min(0)] private int paddingLeft;
    [SerializeField] [Min(0)] private int paddingRight;
    [SerializeField] [Min(0)] private int paddingTop;
    [SerializeField] [Min(0)] private int paddingBottom;

    public UIScrollListDirection Direction => direction;
    public bool ReverseOrder => reverseOrder;
    public UIScrollListAlignment MainAxisAlignment => mainAxisAlignment;
    public UIScrollListAlignment CrossAxisAlignment => crossAxisAlignment;
    public float Spacing => spacing;
    public bool OverrideChildWidth => overrideChildWidth;
    public float ChildWidth => childWidth;
    public bool OverrideChildHeight => overrideChildHeight;
    public float ChildHeight => childHeight;
    public int PaddingLeft => paddingLeft;
    public int PaddingRight => paddingRight;
    public int PaddingTop => paddingTop;
    public int PaddingBottom => paddingBottom;
}
}
