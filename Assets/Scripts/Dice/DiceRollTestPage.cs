using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 独立测试页：验证 Modal 异步返回的结果与传入表现层的点数一致。
/// </summary>
public sealed class DiceRollTestPage : PageBase
{
    [SerializeField] private Button rollButton;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private TextMeshProUGUI eventText;

    private readonly DiceRoller diceRoller = new();
    private bool buttonBound;
    private bool rolling;

    protected override void OnCreate()
    {
        base.OnCreate();
        ValidateConfiguration();
    }

    protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
    {
        BindButton();
        resultText.text = "请求点数：-";
        eventText.text = "Modal 返回：-";
        rollButton.interactable = true;
        return UniTask.CompletedTask;
    }

    protected override void OnClosed(CloseReason reason)
    {
        UnbindButton();
        rolling = false;
    }

    private void OnRollClicked()
    {
        RollDiceAsync().Forget();
    }

    private async UniTaskVoid RollDiceAsync()
    {
        if (rolling)
        {
            return;
        }

        rolling = true;
        rollButton.interactable = false;

        try
        {
            DiceRollResult result = diceRoller.Roll();
            resultText.text = $"请求点数：{result.FaceValue}";

            ModalResult<DiceRollResult> modalResult = await UIManager.Instance.ShowModalAsync<DiceRollModal, DiceRollResult>(
                new DiceRollModalContext(result),
                this.GetCancellationTokenOnDestroy());

            eventText.text = modalResult.Confirmed
                ? $"Modal 返回：{modalResult.Value.FaceValue}"
                : "Modal 已取消";
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
            rolling = false;
            if (rollButton != null)
            {
                rollButton.interactable = true;
            }
        }
    }

    private void BindButton()
    {
        if (buttonBound)
        {
            return;
        }

        rollButton.onClick.AddListener(OnRollClicked);
        buttonBound = true;
    }

    private void UnbindButton()
    {
        if (!buttonBound)
        {
            return;
        }

        rollButton.onClick.RemoveListener(OnRollClicked);
        buttonBound = false;
    }

    private void ValidateConfiguration()
    {
        if (rollButton == null || resultText == null || eventText == null)
        {
            throw new MissingReferenceException($"{nameof(DiceRollTestPage)} '{name}' has an incomplete UI binding.");
        }
    }
}
