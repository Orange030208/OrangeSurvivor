using UnityEngine;

namespace UniversalUI.Core.Runtime
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIPageBase : MonoBehaviour, IUIPage
    {
        private CanvasGroup canvasGroup;

        private string instanceId = string.Empty;
        private bool isVisible;

        public System.Type PageType => GetType();
        public string InstanceId => instanceId;
        public bool IsVisible => isVisible;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        /// <summary>
        /// 为页面注入唯一实例 ID。
        /// </summary>
        public void SetupInstance(string newInstanceId)
        {
            if (string.IsNullOrWhiteSpace(newInstanceId))
            {
                throw new System.ArgumentException("SetupInstance failed: newInstanceId is null or empty.", nameof(newInstanceId));
            }

            instanceId = newInstanceId;
        }

        /// <summary>
        /// 处理页面打开流程并触发扩展生命周期。
        /// </summary>
        public void HandleOpen(UIPageOpenContext context)
        {
            ValidateCanvasGroup();
            gameObject.SetActive(true);
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            isVisible = true;
            OnPageOpened(context);
        }

        /// <summary>
        /// 处理页面关闭流程并触发扩展生命周期。
        /// </summary>
        public void HandleClose()
        {
            ValidateCanvasGroup();
            OnPageClosed();
            isVisible = false;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 更新页面焦点状态并触发扩展回调。
        /// </summary>
        public void HandleFocusChanged(bool hasFocus)
        {
            ValidateCanvasGroup();
            canvasGroup.interactable = hasFocus;
            canvasGroup.blocksRaycasts = hasFocus;
            OnFocusChanged(hasFocus);
        }

        /// <summary>
        /// 每帧驱动页面逻辑，仅在可见状态下执行。
        /// </summary>
        public void HandleTick(float deltaTime)
        {
            if (!isVisible)
            {
                return;
            }

            OnPageTick(deltaTime);
        }

        // 打开时的业务初始化。
        protected virtual void OnPageOpened(UIPageOpenContext context)
        {
        }

        // 关闭时的资源回收。
        protected virtual void OnPageClosed()
        {
        }

        // 焦点切换反馈。
        protected virtual void OnFocusChanged(bool hasFocus)
        {
        }

        // 帧级刷新逻辑。
        protected virtual void OnPageTick(float deltaTime)
        {
        }

        /// <summary>
        /// 校验 CanvasGroup 引用，确保页面交互控制可用。
        /// </summary>
        private void ValidateCanvasGroup()
        {
            if (canvasGroup == null)
            {
                throw new MissingReferenceException($"UIPage '{name}' is missing CanvasGroup reference.");
            }
        }
    }
}
