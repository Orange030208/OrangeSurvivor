using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ItemInfoTooltipView : TooltipBase, ITooltipChromeHandler
{
    public const string VIEW_ID = "tooltip.item_info";

    [SerializeField] private RectTransform root;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI metaText;
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
            throw new MissingComponentException($"{nameof(ItemInfoTooltipView)} '{name}' requires a RectTransform root.");
        }

        ResolveReferences();

        if (nameText == null)
        {
            throw new MissingReferenceException($"{nameof(ItemInfoTooltipView)} '{name}' is missing name text.");
        }

        if (metaText == null)
        {
            throw new MissingReferenceException($"{nameof(ItemInfoTooltipView)} '{name}' is missing meta text.");
        }

        if (bodyText == null)
        {
            throw new MissingReferenceException($"{nameof(ItemInfoTooltipView)} '{name}' is missing body text.");
        }
    }

    protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
    {
        Apply(context.Payload is ItemInfoViewData data ? data : default);
        return UniTask.CompletedTask;
    }

    public void ApplyTooltipChrome(TooltipChromeContext context)
    {
        chromeView?.ApplyTooltipChrome(context);
    }

    private void Apply(ItemInfoViewData data)
    {
        ResolveReferences();
        nameText.text = data.Name ?? string.Empty;
        metaText.text = data.GetMetaText();
        metaText.gameObject.SetActive(!string.IsNullOrWhiteSpace(metaText.text));
        bodyText.text = data.BodyRichText ?? string.Empty;
        LayoutRebuilder.ForceRebuildLayoutImmediate(root);
    }

    private void ResolveReferences()
    {
        if (nameText == null)
        {
            nameText = FindText("Name");
        }

        if (metaText == null)
        {
            metaText = FindText("Meta");
        }

        if (bodyText == null)
        {
            bodyText = FindText("Body");
        }

        if (chromeView == null)
        {
            chromeView = GetComponentInChildren<TooltipChromeView>(true);
        }
    }

    private TextMeshProUGUI FindText(string targetName)
    {
        Transform target = FindChildByName(transform, targetName);
        return target != null ? target.GetComponent<TextMeshProUGUI>() : null;
    }

    private static Transform FindChildByName(Transform rootTransform, string targetName)
    {
        if (rootTransform == null || string.IsNullOrWhiteSpace(targetName))
        {
            return null;
        }

        if (rootTransform.name == targetName)
        {
            return rootTransform;
        }

        for (int i = 0; i < rootTransform.childCount; i++)
        {
            Transform found = FindChildByName(rootTransform.GetChild(i), targetName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
