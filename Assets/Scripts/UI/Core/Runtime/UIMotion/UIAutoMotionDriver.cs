using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Graphic))]
public class UIAutoMotionDriver : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerClickHandler
{
    [Serializable]
    private class MotionBinding
    {
        [Tooltip("触发该动效的 UI 事件。")] 
        public UIMotionEvent motionEvent;

        [Tooltip("该事件发生时要播放的动效动作。")]
        public UIMotionAction action = UIMotionAction.Show;
    }

    [SerializeField] private UIRuntimeMotionBase motionSource;

    [Header("Lifecycle")]
    [SerializeField] private bool playEnterOnEnable;

    [Header("Pointer Event Bindings")]
    [SerializeField] private List<MotionBinding> bindings = new()
    {
        new MotionBinding { motionEvent = UIMotionEvent.PointerEnter, action = UIMotionAction.Enter },
        new MotionBinding { motionEvent = UIMotionEvent.PointerExit, action = UIMotionAction.Exit },
        new MotionBinding { motionEvent = UIMotionEvent.PointerDown, action = UIMotionAction.Press },
        new MotionBinding { motionEvent = UIMotionEvent.PointerUp, action = UIMotionAction.Release },
        new MotionBinding { motionEvent = UIMotionEvent.PointerClick, action = UIMotionAction.Emphasis }
    };

    private readonly List<UIRuntimeMotionBase> runtimeMotions = new();

    private void Awake()
    {
        RebuildRuntimeMotions();
    }

    private void OnEnable()
    {
        if (playEnterOnEnable)
        {
            PlayAll(UIMotionAction.Show);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PlayBoundAction(UIMotionEvent.PointerEnter, requireLeftButton: false, eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        PlayBoundAction(UIMotionEvent.PointerExit, requireLeftButton: false, eventData);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        PlayBoundAction(UIMotionEvent.PointerDown, requireLeftButton: true, eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        PlayBoundAction(UIMotionEvent.PointerUp, requireLeftButton: true, eventData);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        PlayBoundAction(UIMotionEvent.PointerClick, requireLeftButton: true, eventData);
    }

    public void RefreshDefaults()
    {
        RebuildRuntimeMotions();
        foreach (UIRuntimeMotionBase runtimeMotion in runtimeMotions)
        {
            runtimeMotion.RefreshDefaults();
        }
    }

    private void PlayBoundAction(UIMotionEvent motionEvent, bool requireLeftButton, PointerEventData eventData)
    {
        if (requireLeftButton && eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        MotionBinding binding = GetBinding(motionEvent);
        if (binding == null)
        {
            return;
        }

        PlayAll(binding.action);
    }

    private void PlayAll(UIMotionAction action)
    {
        EnsureRuntimeMotions();
        foreach (UIRuntimeMotionBase runtimeMotion in runtimeMotions)
        {
            runtimeMotion.Play(action);
        }
    }

    private void EnsureRuntimeMotions()
    {
        if (runtimeMotions.Count > 0)
        {
            return;
        }

        RebuildRuntimeMotions();
    }

    private void RebuildRuntimeMotions()
    {
        runtimeMotions.Clear();

        UIRuntimeMotionBase source = motionSource != null ? motionSource : GetComponent<UIRuntimeMotionBase>();
        if (source == null)
        {
            return;
        }

        source.GetComponents(runtimeMotions);
    }

    private MotionBinding GetBinding(UIMotionEvent motionEvent)
    {
        foreach (MotionBinding binding in bindings)
        {
            if (binding.motionEvent == motionEvent)
            {
                return binding;
            }
        }

        return null;
    }
}
