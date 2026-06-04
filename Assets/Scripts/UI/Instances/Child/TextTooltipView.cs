using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class TextTooltipView : TooltipBase, ITooltipChromeHandler
{
    [SerializeField] private RectTransform root;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private TooltipChromeView chromeView;

    protected override void OnCreate()
    {
        base.OnCreate();

        if (root == null)
        {
            root = transform as RectTransform;
        }

        if (root == null)
        {
            throw new MissingComponentException($"{nameof(TextTooltipView)} '{name}' requires a RectTransform root.");
        }

        if (bodyText == null)
        {
            bodyText = GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (bodyText == null)
        {
            throw new MissingReferenceException($"{nameof(TextTooltipView)} '{name}' is missing body text.");
        }

        if (chromeView == null)
        {
            chromeView = GetComponentInChildren<TooltipChromeView>(true);
        }
    }

    protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
    {
        bodyText.text = context.Payload as string ?? string.Empty;
        LayoutRebuilder.ForceRebuildLayoutImmediate(root);
        return UniTask.CompletedTask;
    }

    public void ApplyTooltipChrome(TooltipChromeContext context)
    {
        chromeView?.ApplyTooltipChrome(context);
    }
}
