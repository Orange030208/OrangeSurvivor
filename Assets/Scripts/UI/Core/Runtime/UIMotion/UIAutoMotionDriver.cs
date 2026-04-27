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
        [Tooltip("UI event that triggers this motion.")]
        public UIMotionEvent motionEvent;

        [Tooltip("Interaction feedback motion played when this event is raised.")]
        public UIInteractionMotion interactionMotion = UIInteractionMotion.Normal;

        [Tooltip("Legacy action field. Use interactionMotion for new bindings.")]
        public UIMotionAction action = UIMotionAction.Show;

        public UIInteractionMotion ResolveInteractionMotion()
        {
            if (action != UIMotionAction.Show)
            {
                return UIMotionActionMapper.ToInteractionMotion(action, motionEvent);
            }

            return interactionMotion;
        }
    }

    [SerializeField] private UIRuntimeMotionBase motionSource;

    [Header("Lifecycle")]
    [SerializeField] private bool playEnterOnEnable;

    [Header("Pointer Event Bindings")]
    [SerializeField] private List<MotionBinding> bindings = new()
    {
        new MotionBinding { motionEvent = UIMotionEvent.PointerEnter, interactionMotion = UIInteractionMotion.Hover, action = UIMotionAction.Enter },
        new MotionBinding { motionEvent = UIMotionEvent.PointerExit, interactionMotion = UIInteractionMotion.Unhover, action = UIMotionAction.Exit },
        new MotionBinding { motionEvent = UIMotionEvent.PointerDown, interactionMotion = UIInteractionMotion.Pressed, action = UIMotionAction.Press },
        new MotionBinding { motionEvent = UIMotionEvent.PointerUp, interactionMotion = UIInteractionMotion.Released, action = UIMotionAction.Release },
        new MotionBinding { motionEvent = UIMotionEvent.PointerClick, interactionMotion = UIInteractionMotion.ClickPulse, action = UIMotionAction.Emphasis }
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
            PlayVisibility(UIVisibilityMotion.Enter);
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

        PlayInteraction(binding.ResolveInteractionMotion());
    }

    private void PlayVisibility(UIVisibilityMotion motion)
    {
        EnsureRuntimeMotions();
        foreach (UIRuntimeMotionBase runtimeMotion in runtimeMotions)
        {
            runtimeMotion.PlayVisibility(motion);
        }
    }

    private void PlayInteraction(UIInteractionMotion motion)
    {
        EnsureRuntimeMotions();
        foreach (UIRuntimeMotionBase runtimeMotion in runtimeMotions)
        {
            runtimeMotion.PlayInteraction(motion);
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
