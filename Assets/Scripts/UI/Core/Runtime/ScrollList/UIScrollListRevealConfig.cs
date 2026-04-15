using System;
using DG.Tweening;
using UnityEngine;

[Serializable]
public sealed class UIScrollListRevealConfig
{
    [SerializeField] private bool playOnRefresh = true;
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private bool playTogether = true;
    [SerializeField] [Min(0f)] private float startDelay;
    [SerializeField] [Min(0f)] private float itemStagger = 0.04f;
    [SerializeField] private Ease sequenceEase = Ease.OutCubic;
    [SerializeField] private UIMotionAction revealAction = UIMotionAction.Show;

    public bool PlayOnRefresh => playOnRefresh;
    public bool UseUnscaledTime => useUnscaledTime;
    public bool PlayTogether => playTogether;
    public float StartDelay => startDelay;
    public float ItemStagger => itemStagger;
    public Ease SequenceEase => sequenceEase;
    public UIMotionAction RevealAction => revealAction;
}
