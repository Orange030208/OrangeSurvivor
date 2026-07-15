using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 由 Orange UIFramework 管理的骰子结算 Modal。
/// 动画结束后通过 <see cref="ModalBase{TResult}.SetResult"/> 将结果返回给 await 调用方。
/// </summary>
public sealed class DiceRollModal : ModalBase<DiceRollResult>
{
    private const string ROLL_CLIP_ID = "Dice.Roll";
    private const string SETTLE_CLIP_ID = "Dice.Settle";
    private const string READY_CLIP_ID = "Dice.Ready";
    private const string RESULT_EXIT_CLIP_ID = "Dice.ResultExit";
    private const string PRESENTATION_CHANNEL = UIMotionChannelIds.PRESENTATION;
    private const int INTERMEDIATE_ROLL_FRAME_COUNT = 8;
    private const int FINAL_ROLL_FRAME_COUNT = 2;

    [Header("内容")]
    [SerializeField] private Image diceImage;
    [SerializeField] private Sprite[] faceSprites = new Sprite[DiceRollResult.MAX_FACE_VALUE];
    [SerializeField] private TextMeshProUGUI resultText;

    [Header("Orange Motion")]
    [SerializeField] private UIMotionPlayer diceMotionPlayer;

    private DiceRollModalContext currentContext;
    private CancellationTokenSource rollCancellation;

    protected override void OnCreate()
    {
        base.OnCreate();
        ValidateConfiguration();
    }

    protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
    {
        currentContext = context.GetPayload<DiceRollModalContext>()
            ?? throw new InvalidOperationException($"{nameof(DiceRollModal)} requires {nameof(DiceRollModalContext)} payload.");

        ValidateConfiguration();
        resultText.gameObject.SetActive(false);
        diceImage.sprite = GetFaceSprite(DiceRollResult.MIN_FACE_VALUE);

        rollCancellation?.Cancel();
        rollCancellation?.Dispose();
        rollCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, this.GetCancellationTokenOnDestroy());
        ResetDiceVisual();
        return UniTask.CompletedTask;
    }

    protected override UniTask OnOpenedAsync(CancellationToken cancellationToken)
    {
        PlayRollAsync(currentContext.Result, rollCancellation.Token).Forget();
        return UniTask.CompletedTask;
    }

    protected override void OnClosed(CloseReason reason)
    {
        rollCancellation?.Cancel();
        rollCancellation?.Dispose();
        rollCancellation = null;
        diceMotionPlayer.StopChannel(PRESENTATION_CHANNEL);
        currentContext = null;
    }

    private async UniTaskVoid PlayRollAsync(DiceRollResult result, CancellationToken cancellationToken)
    {
        try
        {
            ConfigureRollSpriteSequence(result);
            await PlayMotionIfConfiguredAsync(ROLL_CLIP_ID, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            resultText.text = $"结果：{result.FaceValue}";
            resultText.gameObject.SetActive(true);
            await PlayMotionIfConfiguredAsync(SETTLE_CLIP_ID, cancellationToken);
            await PlayMotionIfConfiguredAsync(RESULT_EXIT_CLIP_ID, cancellationToken);

            SetResult(result);
        }
        catch (OperationCanceledException)
        {
            // 视图被框架关闭或替换时，结算不应继续向外发送。
        }
    }

    private void ConfigureRollSpriteSequence(DiceRollResult result)
    {
        if (!diceMotionPlayer.TryGetTrack(ROLL_CLIP_ID, out UISpriteSequenceMotionTrack spriteSequenceTrack))
        {
            throw new InvalidOperationException(
                $"{nameof(DiceRollModal)} '{name}' requires a {nameof(UISpriteSequenceMotionTrack)} in '{ROLL_CLIP_ID}'.");
        }

        List<Sprite> frames = new(INTERMEDIATE_ROLL_FRAME_COUNT + FINAL_ROLL_FRAME_COUNT);
        int previousFaceValue = DiceRollResult.MIN_FACE_VALUE;
        for (int i = 0; i < INTERMEDIATE_ROLL_FRAME_COUNT; i++)
        {
            int nextFaceValue = GetIntermediateFaceValue(previousFaceValue, result.FaceValue);
            frames.Add(GetFaceSprite(nextFaceValue));
            previousFaceValue = nextFaceValue;
        }

        Sprite finalSprite = GetFaceSprite(result.FaceValue);
        for (int i = 0; i < FINAL_ROLL_FRAME_COUNT; i++)
        {
            frames.Add(finalSprite);
        }

        spriteSequenceTrack.SetFrames(frames);
    }

    private int GetIntermediateFaceValue(int previousFaceValue, int finalFaceValue)
    {
        int value;
        do
        {
            value = UnityEngine.Random.Range(DiceRollResult.MIN_FACE_VALUE, DiceRollResult.MAX_FACE_VALUE + 1);
        }
        while (value == previousFaceValue || value == finalFaceValue);

        return value;
    }

    private Sprite GetFaceSprite(int faceValue)
    {
        Sprite sprite = faceSprites[faceValue - DiceRollResult.MIN_FACE_VALUE];
        if (sprite == null)
        {
            throw new MissingReferenceException($"{nameof(DiceRollModal)} '{name}' is missing sprite for face {faceValue}.");
        }

        return sprite;
    }

    private UniTask PlayMotionIfConfiguredAsync(string clipId, CancellationToken cancellationToken)
    {
        return diceMotionPlayer.TryGetClip(clipId, out _)
            ? diceMotionPlayer.PlayAsync(clipId, cancellationToken)
            : UniTask.CompletedTask;
    }

    private void ResetDiceVisual()
    {
        // 预制体会被缓存复用；先显式回到标准姿态，再以它作为本轮动画的 Initial 快照。
        diceMotionPlayer.SetImmediate(READY_CLIP_ID);
        diceMotionPlayer.RefreshDefaults();
    }

    private void ValidateConfiguration()
    {
        if (diceImage == null || resultText == null || diceMotionPlayer == null)
        {
            throw new MissingReferenceException($"{nameof(DiceRollModal)} '{name}' has an incomplete UI binding.");
        }

        if (faceSprites == null || faceSprites.Length != DiceRollResult.MAX_FACE_VALUE)
        {
            throw new InvalidOperationException($"{nameof(DiceRollModal)} '{name}' requires exactly {DiceRollResult.MAX_FACE_VALUE} face sprites.");
        }
    }
}
