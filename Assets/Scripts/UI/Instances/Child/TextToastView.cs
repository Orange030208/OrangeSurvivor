using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class TextToastView : ToastBase
{
    [SerializeField] private RectTransform root;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Image iconImage;

    protected override void Awake()
    {
        base.Awake();
        ValidateReferences();
    }

    protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
    {
        ApplyPayload(context.Payload);
        return UniTask.CompletedTask;
    }

    protected override void OnClosed(CloseReason reason)
    {
        messageText.text = string.Empty;
        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }
    }

    private void ApplyPayload(object payload)
    {
        if (payload is ToastPayload toastPayload)
        {
            messageText.text = toastPayload.Message;
            ApplyIcon(toastPayload.Icon);
        }
        else if (payload is string message)
        {
            messageText.text = message;
            ApplyIcon(null);
        }
        else
        {
            messageText.text = payload != null ? payload.ToString() : string.Empty;
            ApplyIcon(null);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(root);
    }

    private void ApplyIcon(Sprite icon)
    {
        if (iconImage == null)
        {
            return;
        }

        iconImage.sprite = icon;
        iconImage.enabled = icon != null;
    }

    private void ValidateReferences()
    {
        if (root == null)
        {
            root = transform as RectTransform;
        }

        if (root == null)
        {
            throw new MissingComponentException($"{nameof(TextToastView)} '{name}' requires a RectTransform root.");
        }

        if (messageText == null)
        {
            throw new MissingReferenceException($"{nameof(TextToastView)} '{name}' is missing message text.");
        }
    }
}
