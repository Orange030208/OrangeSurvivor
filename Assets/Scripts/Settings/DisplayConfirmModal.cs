using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class DisplayConfirmModal : ModalBase<bool>
{
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private float remainingSeconds;
    private bool inputBound;

    public override bool RequiresTick => true;

    protected override void Awake()
    {
        base.Awake();
        ResolveReferences();
        ValidateConfiguration();
        BindButtons();
    }

    private void OnDestroy()
    {
        UnbindButtons();
        UnbindCancelInput();
    }

    protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
    {
        DisplayConfirmModalContext modalContext = context.GetPayload<DisplayConfirmModalContext>();
        if (modalContext == null)
        {
            throw new System.ArgumentException($"{nameof(DisplayConfirmModal)} requires {nameof(DisplayConfirmModalContext)} payload.");
        }

        Configure(modalContext);
        BindCancelInput();
        return UniTask.CompletedTask;
    }

    protected override UniTask OnOpenedAsync(CancellationToken cancellationToken)
    {
        if (confirmButton != null)
        {
            EventSystem.current?.SetSelectedGameObject(confirmButton.gameObject);
        }

        return UniTask.CompletedTask;
    }

    protected override void OnClosed(CloseReason reason)
    {
        UnbindCancelInput();
        remainingSeconds = 0f;
    }

    protected override void OnTick(float deltaTime)
    {
        remainingSeconds -= deltaTime;
        if (remainingSeconds <= 0f)
        {
            Cancel(CloseReason.Cancel);
            return;
        }

        RefreshCountdown();
    }

    public void ValidateConfiguration()
    {
        if (messageText == null)
        {
            throw new MissingReferenceException($"{nameof(DisplayConfirmModal)} '{name}' is missing message text.");
        }

        if (countdownText == null)
        {
            throw new MissingReferenceException($"{nameof(DisplayConfirmModal)} '{name}' is missing countdown text.");
        }

        if (confirmButton == null)
        {
            throw new MissingReferenceException($"{nameof(DisplayConfirmModal)} '{name}' is missing confirm button.");
        }

        if (cancelButton == null)
        {
            throw new MissingReferenceException($"{nameof(DisplayConfirmModal)} '{name}' is missing cancel button.");
        }
    }

    private void Configure(DisplayConfirmModalContext context)
    {
        ResolveReferences();
        ValidateConfiguration();

        remainingSeconds = context.TimeoutSeconds;
        messageText.text = $"保留显示设置?\n{DisplaySettingsService.Format(context.PreviousDisplay)} -> {DisplaySettingsService.Format(context.TargetDisplay)}";
        RefreshCountdown();
    }

    private void RefreshCountdown()
    {
        if (countdownText != null)
        {
            countdownText.text = $"{Mathf.CeilToInt(Mathf.Max(0f, remainingSeconds))} 秒后自动回退";
        }
    }

    private void BindButtons()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(OnConfirmClicked);
            confirmButton.onClick.AddListener(OnConfirmClicked);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveListener(OnCancelClicked);
            cancelButton.onClick.AddListener(OnCancelClicked);
        }
    }

    private void UnbindButtons()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(OnConfirmClicked);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveListener(OnCancelClicked);
        }
    }

    private void BindCancelInput()
    {
        if (inputBound)
        {
            return;
        }

        GameInputService inputService = GameInputService.Instance;
        if (inputService == null)
        {
            return;
        }

        inputService.UiCancelPerformed += OnUiCancelPerformed;
        inputBound = true;
    }

    private void UnbindCancelInput()
    {
        if (!inputBound)
        {
            return;
        }

        GameInputService inputService = GameInputService.Instance;
        if (inputService != null)
        {
            inputService.UiCancelPerformed -= OnUiCancelPerformed;
        }

        inputBound = false;
    }

    private void OnConfirmClicked()
    {
        SetResult(true);
    }

    private void OnCancelClicked()
    {
        Cancel(CloseReason.Cancel);
    }

    private void OnUiCancelPerformed()
    {
        Cancel(CloseReason.Cancel);
    }

    private void ResolveReferences()
    {
        if (messageText == null || countdownText == null)
        {
            TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                TextMeshProUGUI text = texts[i];
                if (messageText == null && text.name == "Message")
                {
                    messageText = text;
                }
                else if (countdownText == null && text.name == "Countdown")
                {
                    countdownText = text;
                }
            }
        }

        if (confirmButton == null)
        {
            Transform confirm = transform.Find("Panel/Actions/ConfirmButton");
            confirmButton = confirm != null ? confirm.GetComponent<Button>() : null;
        }

        if (cancelButton == null)
        {
            Transform cancel = transform.Find("Panel/Actions/CancelButton");
            cancelButton = cancel != null ? cancel.GetComponent<Button>() : null;
        }
    }
}
