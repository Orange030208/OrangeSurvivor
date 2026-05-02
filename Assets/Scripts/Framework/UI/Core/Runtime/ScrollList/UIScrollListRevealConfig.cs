using UnityEngine.Scripting.APIUpdating;

namespace AXR.Framework.UI
{
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
    [SerializeField] private string initialClipId = UIMotionClipIds.HIDE;
    [SerializeField] private string revealClipId = UIMotionClipIds.SHOW;

    public bool PlayOnRefresh => playOnRefresh;
    public bool UseUnscaledTime => useUnscaledTime;
    public bool PlayTogether => playTogether;
    public float StartDelay => startDelay;
    public float ItemStagger => itemStagger;
    public Ease SequenceEase => sequenceEase;
    public string InitialClipId => string.IsNullOrWhiteSpace(initialClipId) ? UIMotionClipIds.HIDE : initialClipId;
    public string RevealClipId => string.IsNullOrWhiteSpace(revealClipId) ? UIMotionClipIds.SHOW : revealClipId;
}
}
