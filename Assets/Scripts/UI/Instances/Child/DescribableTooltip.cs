using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class DescribableTooltip : TooltipBase
{
    [SerializeField] private RectTransform root;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private ExtraInfoDescriber extraInfoDescriber;

    protected override void Awake()
    {
        base.Awake();

        if (root == null)
        {
            root = transform as RectTransform;
        }

        if (root == null)
        {
            throw new MissingComponentException($"{nameof(DescribableTooltip)} '{name}' requires a RectTransform root.");
        }

        if (iconImage == null)
        {
            throw new MissingReferenceException($"{nameof(DescribableTooltip)} '{name}' is missing icon image.");
        }

        if (titleText == null)
        {
            throw new MissingReferenceException($"{nameof(DescribableTooltip)} '{name}' is missing title text.");
        }

        if (extraInfoDescriber == null)
        {
            throw new MissingReferenceException($"{nameof(DescribableTooltip)} '{name}' is missing description list displayer.");
        }
    }

    protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
    {
        IDescribable describable = context.GetPayload<IDescribable>();
        ApplyDocument(describable);
        return UniTask.CompletedTask;
    }

    private void ApplyDocument(IDescribable document)
    {
        titleText.text = document != null ? document.Title : string.Empty;
        iconImage.sprite = document != null ? document.Icon : null;
        iconImage.enabled = document != null && document.Icon != null;
        extraInfoDescriber.Display(document);
        LayoutRebuilder.ForceRebuildLayoutImmediate(root);
    }
}
