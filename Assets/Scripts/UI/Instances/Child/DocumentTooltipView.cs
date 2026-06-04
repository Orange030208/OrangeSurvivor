using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DocumentTooltipView : TooltipBase, ITooltipChromeHandler
{
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
        titleText.text = document != null ? document.Title : string.Empty;
        iconImage.sprite = document != null ? document.Icon : null;
        iconImage.enabled = document != null && document.Icon != null;
        extraInfoDescriber.Display(document);
        LayoutRebuilder.ForceRebuildLayoutImmediate(root);
    }
}
