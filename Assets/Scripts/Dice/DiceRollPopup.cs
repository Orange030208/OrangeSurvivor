using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Orange.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YokiFrame;

/// <summary>
/// 由 Orange UIFramework 管理的骰子弹窗。它只播放调用方已经决定的结果。
/// </summary>
public sealed class DiceRollPopup : PopupBase
{
    private const string ROLL_CLIP_ID = "Dice.Roll";
    private const string SETTLE_CLIP_ID = "Dice.Settle";
    private const string PRESENTATION_CHANNEL = UIMotionChannelIds.PRESENTATION;

    [Header("内容")]
    [SerializeField] private Image diceImage;
    [SerializeField] private Sprite[] faceSprites = new Sprite[DiceRollResult.MAX_FACE_VALUE];
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private Button closeButton;

    [Header("Orange Motion")]
    [SerializeField] private UIMotionPlayer diceMotionPlayer;

    [Header("表现节奏")]
    [SerializeField] [Min(0.1f)] private float rollDuration = 0.72f;
    [SerializeField] [Min(0.01f)] private float firstFaceInterval = 0.045f;
    [SerializeField] [Min(0.01f)] private float lastFaceInterval = 0.16f;

    private DiceRollPopupContext currentContext;
    private CancellationTokenSource rollCancellation;
    private bool completed;
    private bool closeBound;

    protected override void OnCreate()
    {
        base.OnCreate();
        ValidateConfiguration();
    }

    protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
    {
        currentContext = context.GetPayload<DiceRollPopupContext>()
            ?? throw new InvalidOperationException($"{nameof(DiceRollPopup)} requires {nameof(DiceRollPopupContext)} payload.");

        ValidateConfiguration();
        BindCloseButton();
        completed = false;
        closeButton.interactable = false;
        resultText.gameObject.SetActive(false);
        diceImage.sprite = GetFaceSprite(DiceRollResult.MIN_FACE_VALUE);

        rollCancellation?.Cancel();
        rollCancellation?.Dispose();
        rollCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, this.GetCancellationTokenOnDestroy());
        PlayRollAsync(currentContext.Result, rollCancellation.Token).Forget();
        return UniTask.CompletedTask;
    }

    protected override void OnClosed(CloseReason reason)
    {
        rollCancellation?.Cancel();
        rollCancellation?.Dispose();
        rollCancellation = null;
        diceMotionPlayer.StopChannel(PRESENTATION_CHANNEL);
        UnbindCloseButton();
        currentContext = null;
        completed = false;
    }

    private async UniTaskVoid PlayRollAsync(DiceRollResult result, CancellationToken cancellationToken)
    {
        try
        {
            diceMotionPlayer.Play(ROLL_CLIP_ID);

            float elapsed = 0f;
            int previousFaceValue = DiceRollResult.MIN_FACE_VALUE;
            while (elapsed < rollDuration)
            {
                cancellationToken.ThrowIfCancellationRequested();

                float progress = Mathf.Clamp01(elapsed / rollDuration);
                int nextFaceValue = GetDifferentFaceValue(previousFaceValue);
                diceImage.sprite = GetFaceSprite(nextFaceValue);
                previousFaceValue = nextFaceValue;

                float interval = Mathf.Lerp(firstFaceInterval, lastFaceInterval, progress * progress);
                await UniTask.Delay(
                    TimeSpan.FromSeconds(interval),
                    DelayType.UnscaledDeltaTime,
                    PlayerLoopTiming.Update,
                    cancellationToken);
                elapsed += interval;
            }

            cancellationToken.ThrowIfCancellationRequested();
            diceImage.sprite = GetFaceSprite(result.FaceValue);
            resultText.text = $"结果：{result.FaceValue}";
            resultText.gameObject.SetActive(true);
            diceMotionPlayer.Play(SETTLE_CLIP_ID);
            await UniTask.Delay(
                TimeSpan.FromSeconds(0.2f),
                DelayType.UnscaledDeltaTime,
                PlayerLoopTiming.Update,
                cancellationToken);

            Complete(result);
        }
        catch (OperationCanceledException)
        {
            // 视图被框架关闭或替换时，结算不应继续向外发送。
        }
    }

    private void Complete(DiceRollResult result)
    {
        if (completed)
        {
            return;
        }

        completed = true;
        closeButton.interactable = true;
        currentContext.Completed?.Invoke(result);
        EventKit.Type.Send(new DiceRolledEvent(result));
    }

    private void OnCloseClicked()
    {
        if (!completed)
        {
            return;
        }

        Handle.CloseAsync(CloseReason.Completed, this.GetCancellationTokenOnDestroy()).Forget();
    }

    private int GetDifferentFaceValue(int previousFaceValue)
    {
        int value = UnityEngine.Random.Range(DiceRollResult.MIN_FACE_VALUE, DiceRollResult.MAX_FACE_VALUE);
        if (value >= previousFaceValue)
        {
            value++;
        }

        return value;
    }

    private Sprite GetFaceSprite(int faceValue)
    {
        Sprite sprite = faceSprites[faceValue - DiceRollResult.MIN_FACE_VALUE];
        if (sprite == null)
        {
            throw new MissingReferenceException($"{nameof(DiceRollPopup)} '{name}' is missing sprite for face {faceValue}.");
        }

        return sprite;
    }

    private void BindCloseButton()
    {
        if (closeBound)
        {
            return;
        }

        closeButton.onClick.AddListener(OnCloseClicked);
        closeBound = true;
    }

    private void UnbindCloseButton()
    {
        if (!closeBound)
        {
            return;
        }

        closeButton.onClick.RemoveListener(OnCloseClicked);
        closeBound = false;
    }

    private void ValidateConfiguration()
    {
        if (diceImage == null || resultText == null || closeButton == null || diceMotionPlayer == null)
        {
            throw new MissingReferenceException($"{nameof(DiceRollPopup)} '{name}' has an incomplete UI binding.");
        }

        if (faceSprites == null || faceSprites.Length != DiceRollResult.MAX_FACE_VALUE)
        {
            throw new InvalidOperationException($"{nameof(DiceRollPopup)} '{name}' requires exactly {DiceRollResult.MAX_FACE_VALUE} face sprites.");
        }
    }
}
