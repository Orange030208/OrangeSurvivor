using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DocumentTooltipView : TooltipBase, ITooltipChromeHandler
{
    public const string VIEW_ID = "tooltip.document";

    [SerializeField] private RectTransform root;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private ExtraInfoDescriber extraInfoDescriber;
    [SerializeField] private TooltipChromeView chromeView;

    private readonly InfoDocumentService infoDocumentService = new();

    protected override void OnCreate()
    {
        base.OnCreate();

        if (root == null)
        {
            root = transform as RectTransform;
        }

        if (root == null)
        {
            throw new MissingComponentException($"{nameof(DocumentTooltipView)} '{name}' requires a RectTransform root.");
        }

        if (iconImage == null)
        {
            throw new MissingReferenceException($"{nameof(DocumentTooltipView)} '{name}' is missing icon image.");
        }

        if (titleText == null)
        {
            throw new MissingReferenceException($"{nameof(DocumentTooltipView)} '{name}' is missing title text.");
        }

        if (extraInfoDescriber == null)
        {
            throw new MissingReferenceException($"{nameof(DocumentTooltipView)} '{name}' is missing description list displayer.");
        }

        if (chromeView == null)
        {
            chromeView = GetComponentInChildren<TooltipChromeView>(true);
        }
    }

    protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
    {
        ApplyDocument(ResolveDocument(context.Payload));
        return UniTask.CompletedTask;
    }

    public void ApplyTooltipChrome(TooltipChromeContext context)
    {
        chromeView?.ApplyTooltipChrome(context);
    }

    private InfoDocument ResolveDocument(object payload)
    {
        if (payload is InfoDocument document)
        {
            return document;
        }

        if (payload != null && infoDocumentService.TryBuild(payload, out InfoDocument builtDocument))
        {
            return builtDocument;
        }

        return null;
    }

    private void ApplyDocument(InfoDocument document)
    {
        titleText.text = ResolveTitle(document);
        Sprite icon = ResolveIcon(document);
        iconImage.sprite = icon;
        iconImage.enabled = icon != null;
        extraInfoDescriber.Display(document);
        LayoutRebuilder.ForceRebuildLayoutImmediate(root);
    }

    private static string ResolveTitle(InfoDocument document)
    {
        if (document?.Items == null)
        {
            return string.Empty;
        }

        for (int i = 0; i < document.Items.Count; i++)
        {
            InfoItem item = document.Items[i];
            if (item.Type == InfoItemType.Title && !string.IsNullOrWhiteSpace(item.Content))
            {
                return item.Decoder.DecodeText(item.Content);
            }
        }

        return string.Empty;
    }

    private static Sprite ResolveIcon(InfoDocument document)
    {
        if (document?.Items == null)
        {
            return null;
        }

        for (int i = 0; i < document.Items.Count; i++)
        {
            InfoItem item = document.Items[i];
            if (item.Type != InfoItemType.Image)
            {
                continue;
            }

            if (item.Decoder.TryDecode(item.Content, out Sprite sprite))
            {
                return sprite;
            }
        }

        return null;
    }
}
