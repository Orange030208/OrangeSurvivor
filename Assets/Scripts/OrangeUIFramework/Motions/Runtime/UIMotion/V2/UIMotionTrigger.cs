namespace Orange.UIFramework
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.EventSystems;

    [DisallowMultipleComponent]
    // 将 Unity UI/EventSystem 事件翻译成 UIMotionPlayer 的 Clip 播放请求。
    // 它不直接创建 Tween，因此交互反馈仍统一经过 Player 的通道与冲突策略。
    // 组件允许挂在可射线命中的子节点父级，避免子节点抢先处理 PointerDown/Click 导致 Button 收不到点击。
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

            // 这里保存 ClipId 而不是直接引用 Clip，方便多个 Prefab 复用同一套事件绑定语义。
            public string clipId;
            public bool requireLeftButton = true;
            [Min(0f)] public float delay;
        }

        [SerializeField] private UIMotionPlayer player;

        [SerializeField] private List<TriggerBinding> bindings = new()
        {
            new TriggerBinding
            {
                triggerEvent = UIMotionTriggerEvent.PointerEnter, clipId = UIMotionClipIds.HOVER_IN,
                requireLeftButton = false
            },
            new TriggerBinding
            {
                triggerEvent = UIMotionTriggerEvent.PointerExit, clipId = UIMotionClipIds.HOVER_OUT,
                requireLeftButton = false
            },
            new TriggerBinding
            {
                triggerEvent = UIMotionTriggerEvent.PointerDown, clipId = UIMotionClipIds.PRESS,
                requireLeftButton = true
            },
            new TriggerBinding
            {
                triggerEvent = UIMotionTriggerEvent.PointerUp, clipId = UIMotionClipIds.RELEASE,
                requireLeftButton = true
            }
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

            // 同一个事件允许触发多条绑定，例如 PointerDown 同时播放按压和音效回调 Track。
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

            // 优先绑定本对象上的 Player；找不到时向父级查找，支持按钮子节点只挂 Trigger 的 Prefab 结构。
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
}
