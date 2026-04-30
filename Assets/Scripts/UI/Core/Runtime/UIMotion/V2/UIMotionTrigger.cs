using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Graphic))]
public sealed class UIMotionTrigger : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerClickHandler,
    ISelectHandler,
    IDeselectHandler
{
    [Serializable]
    private sealed class TriggerBinding
    {
        public UIMotionTriggerEvent triggerEvent;
        public string clipId;
        public bool requireLeftButton = true;
        [Min(0f)] public float delay;
    }

    [SerializeField] private UIMotionPlayer player;
    [SerializeField] private List<TriggerBinding> bindings = new()
    {
        new TriggerBinding { triggerEvent = UIMotionTriggerEvent.PointerEnter, clipId = UIMotionClipIds.HOVER_IN, requireLeftButton = false },
        new TriggerBinding { triggerEvent = UIMotionTriggerEvent.PointerExit, clipId = UIMotionClipIds.HOVER_OUT, requireLeftButton = false },
        new TriggerBinding { triggerEvent = UIMotionTriggerEvent.PointerDown, clipId = UIMotionClipIds.PRESS, requireLeftButton = true },
        new TriggerBinding { triggerEvent = UIMotionTriggerEvent.PointerUp, clipId = UIMotionClipIds.RELEASE, requireLeftButton = true }
    };

    private bool isPointerPressed;

    private void Awake()
    {
        ResolvePlayer();
    }

    private void OnEnable()
    {
        Play(UIMotionTriggerEvent.OnEnable, null);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Play(UIMotionTriggerEvent.PointerEnter, eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isPointerPressed)
        {
            Play(UIMotionTriggerEvent.PointerUp, eventData);
            isPointerPressed = false;
        }

        Play(UIMotionTriggerEvent.PointerExit, eventData);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (IsLeftButton(eventData))
        {
            isPointerPressed = true;
        }

        Play(UIMotionTriggerEvent.PointerDown, eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (IsLeftButton(eventData))
        {
            isPointerPressed = false;
        }

        Play(UIMotionTriggerEvent.PointerUp, eventData);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Play(UIMotionTriggerEvent.PointerClick, eventData);
    }

    public void OnSelect(BaseEventData eventData)
    {
        Play(UIMotionTriggerEvent.Select, null);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (isPointerPressed)
        {
            Play(UIMotionTriggerEvent.PointerUp, null);
            isPointerPressed = false;
        }

        Play(UIMotionTriggerEvent.Deselect, null);
    }

    private void Play(UIMotionTriggerEvent triggerEvent, PointerEventData pointerEventData)
    {
        ResolvePlayer();
        if (player == null)
        {
            return;
        }

        for (int i = 0; i < bindings.Count; i++)
        {
            TriggerBinding binding = bindings[i];
            if (binding == null || binding.triggerEvent != triggerEvent)
            {
                continue;
            }

            if (pointerEventData != null
                && binding.requireLeftButton
                && pointerEventData.button != PointerEventData.InputButton.Left)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(binding.clipId))
            {
                player.Play(binding.clipId, binding.delay);
            }
        }
    }

    private void ResolvePlayer()
    {
        if (player != null)
        {
            return;
        }

        player = GetComponent<UIMotionPlayer>();
        if (player == null)
        {
            player = GetComponentInParent<UIMotionPlayer>();
        }
    }

    private static bool IsLeftButton(PointerEventData eventData)
    {
        return eventData == null || eventData.button == PointerEventData.InputButton.Left;
    }
}
