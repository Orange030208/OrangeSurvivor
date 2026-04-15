using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
[RequireComponent(typeof(UIRevealMotion))]
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

    [SerializeField] private UIRevealMotion motion;

    [Header("Lifecycle")]
    [SerializeField] private bool playEnterOnEnable;

    [Header("Pointer Event Bindings")]
    [SerializeField] private List<MotionBinding> bindings = new()
    {
        new MotionBinding { motionEvent = UIMotionEvent.PointerEnter, action = UIMotionAction.Highlight },
        new MotionBinding { motionEvent = UIMotionEvent.PointerExit, action = UIMotionAction.Show },
        new MotionBinding { motionEvent = UIMotionEvent.PointerDown, action = UIMotionAction.Press },
        new MotionBinding { motionEvent = UIMotionEvent.PointerUp, action = UIMotionAction.Show },
        new MotionBinding { motionEvent = UIMotionEvent.PointerClick, action = UIMotionAction.Emphasis }
    };

    private IUIRuntimeMotion runtimeMotion;

    private void Awake()
    {
        motion ??= GetComponent<UIRevealMotion>();
        runtimeMotion = motion;
    }

    private void OnEnable()
    {
        if (playEnterOnEnable)
        {
            runtimeMotion.Play(UIMotionAction.Show);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        print("111111");
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
        runtimeMotion.RefreshDefaults();
    }

    private void PlayBoundAction(UIMotionEvent motionEvent, bool requireLeftButton, PointerEventData eventData)
    {
        if (requireLeftButton && eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        MotionBinding binding = GetBinding(motionEvent);
        if (binding != null)
        {
            runtimeMotion.Play(binding.action);
        }
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
