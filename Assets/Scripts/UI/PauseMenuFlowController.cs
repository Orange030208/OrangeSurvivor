using UnityEngine;

/// <summary>
/// 暂停菜单流程协调器：负责“先关闭暂停菜单，再执行业务动作”的异步编排。
/// 页面只发点击意图事件，真正的恢复/返回菜单动作由这里统一在页面关闭后触发。
/// </summary>
public class PauseMenuFlowController : MonoBehaviour
{
    [SerializeField] private UIManager uiManager;

    private bool waitingForPauseMenuClose;
    private PauseMenuAction pendingAction = PauseMenuAction.None;

    private enum PauseMenuAction
    {
        None,
        Resume,
        ReturnToMenu
    }

    private void Awake()
    {
        if (uiManager == null)
        {
            uiManager = FindFirstObjectByType<UIManager>();
        }
    }

    private void OnEnable()
    {
        GameEventBus.Subscribe<PauseMenuContinueClickedEvent>(OnPauseMenuContinueClicked);
        GameEventBus.Subscribe<PauseMenuReturnToMenuClickedEvent>(OnPauseMenuReturnToMenuClicked);

        if (uiManager != null)
        {
            uiManager.PageClosed += OnPageClosed;
        }
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<PauseMenuContinueClickedEvent>(OnPauseMenuContinueClicked);
        GameEventBus.Unsubscribe<PauseMenuReturnToMenuClickedEvent>(OnPauseMenuReturnToMenuClicked);

        if (uiManager != null)
        {
            uiManager.PageClosed -= OnPageClosed;
        }
    }

    private void OnPauseMenuContinueClicked()
    {
        RequestPauseMenuClose(PauseMenuAction.Resume);
    }

    private void OnPauseMenuReturnToMenuClicked()
    {
        RequestPauseMenuClose(PauseMenuAction.ReturnToMenu);
    }

    private void RequestPauseMenuClose(PauseMenuAction action)
    {
        if (uiManager == null || waitingForPauseMenuClose)
        {
            return;
        }

        if (!uiManager.IsPageOpen<GamePauseMenu>())
        {
            PublishAction(action);
            return;
        }

        pendingAction = action;
        waitingForPauseMenuClose = uiManager.ClosePage<GamePauseMenu>();

        if (!waitingForPauseMenuClose)
        {
            PublishAction(action);
        }
    }

    private void OnPageClosed(object sender, UIPageEventArgs eventArgs)
    {
        if (!waitingForPauseMenuClose || eventArgs.PageType != typeof(GamePauseMenu))
        {
            return;
        }

        PauseMenuAction action = pendingAction;
        waitingForPauseMenuClose = false;
        pendingAction = PauseMenuAction.None;
        PublishAction(action);
    }

    private void PublishAction(PauseMenuAction action)
    {
        switch (action)
        {
            case PauseMenuAction.Resume:
                GameEventBus.Publish<ResumeGameRequestedEvent>();
                break;
            case PauseMenuAction.ReturnToMenu:
                GameEventBus.Publish<ReturnToMenuRequestedEvent>();
                break;
        }
    }
}
