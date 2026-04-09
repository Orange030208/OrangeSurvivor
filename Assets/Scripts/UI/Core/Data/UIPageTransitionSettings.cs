using System;
using DG.Tweening;
using UnityEngine;

public enum UITransitionType
{
    None,
    Fade,
    SlideFromLeft,
    SlideFromRight,
    SlideFromTop,
    SlideFromBottom,
    Scale
}

[Serializable]
public sealed class UIPageTransitionSettings
{
    public UITransitionType transitionType = UITransitionType.Fade;
    public float duration = 0.2f;
    public Ease ease = Ease.OutCubic;
    public float offset = 80f;
    public float startScale = 0.94f;
    public bool fade = true;
}
