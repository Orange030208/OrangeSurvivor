using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YokiFrame;

/// <summary>
/// 独立测试页：验证 Orange 弹窗链路、回调与类型事件拿到的是同一个结果。
/// </summary>
public sealed class DiceRollTestPage : PageBase
{
    private const string DICE_POPUP_GROUP_ID = "dice-roll";

    [SerializeField] private Button rollButton;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private TextMeshProUGUI eventText;

    private readonly DiceRoller diceRoller = new();
    private bool eventsBound;
    private bool openingPopup;

    protected override void OnCreate()
    {
        base.OnCreate();
        ValidateConfiguration();
    }

    protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
    {
        BindEvents();
        resultText.text = "回调结果：-";
        eventText.text = "事件结果：-";
        rollButton.interactable = true;
        return UniTask.CompletedTask;
    }

    protected override void OnClosed(CloseReason reason)
    {
        UnbindEvents();
        openingPopup = false;
    }

    private void OnRollClicked()
    {
        OpenDicePopupAsync().Forget();
    }

    private async UniTaskVoid OpenDicePopupAsync()
    {
        if (openingPopup)
        {
            return;
        }

        openingPopup = true;
        rollButton.interactable = false;

        try
        {
            DiceRollResult result = diceRoller.Roll();
            PopupOptions options = new(
                closeOnOutsideClick: false,
                groupId: DICE_POPUP_GROUP_ID,
                replaceSameGroup: true,
                trackInStack: false,
                preferredAnchor: FloatingViewAnchor.Center,
                showBackdrop: true);

            ViewHandle<DiceRollPopup> handle = await UIManager.Instance.ShowPopupAsync<DiceRollPopup>(
                new DiceRollPopupContext(result, OnDiceRollCompleted),
                options,
                this.GetCancellationTokenOnDestroy());
            await handle.ClosedTask;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }
        finally
        {
            openingPopup = false;
            if (rollButton != null)
            {
                rollButton.interactable = true;
            }
        }
    }

    private void OnDiceRollCompleted(DiceRollResult result)
    {
        resultText.text = $"回调结果：{result.FaceValue}";
    }

    private void OnDiceRolled(DiceRolledEvent eventData)
    {
        eventText.text = $"事件结果：{eventData.Result.FaceValue}";
    }

    private void BindEvents()
    {
        if (eventsBound)
        {
            return;
        }

        rollButton.onClick.AddListener(OnRollClicked);
        EventKit.Type.Register<DiceRolledEvent>(OnDiceRolled);
        eventsBound = true;
    }

    private void UnbindEvents()
    {
        if (!eventsBound)
        {
            return;
        }

        rollButton.onClick.RemoveListener(OnRollClicked);
        EventKit.Type.UnRegister<DiceRolledEvent>(OnDiceRolled);
        eventsBound = false;
    }

    private void ValidateConfiguration()
    {
        if (rollButton == null || resultText == null || eventText == null)
        {
            throw new MissingReferenceException($"{nameof(DiceRollTestPage)} '{name}' has an incomplete UI binding.");
        }
    }
}
