using System.Collections.Generic;
using Orange.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuffIconItem : ViewPartBase, IDescribable
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Image borderImage;
    [SerializeField] private TextMeshProUGUI stackText;
    [SerializeField] private TextMeshProUGUI durationText;
    [SerializeField] private Color positiveColor = new(0.4f, 0.8f, 0.45f, 1f);
    [SerializeField] private Color neutralColor = new(0.8f, 0.8f, 0.8f, 1f);
    [SerializeField] private Color negativeColor = new(0.85f, 0.35f, 0.35f, 1f);

    private ActiveBuffViewData viewData;

    public void Configure(ActiveBuffViewData viewData)
    {
        this.viewData = viewData;

        if (iconImage != null)
        {
            iconImage.sprite = viewData.Icon;
            iconImage.enabled = viewData.Icon != null;
        }

        if (borderImage != null)
        {
            borderImage.color = ResolveBorderColor(viewData.Polarity);
        }

        if (stackText != null)
        {
            bool showStack = viewData.StackCount > 1;
            stackText.gameObject.SetActive(showStack);
            stackText.text = viewData.StackCount.ToString();
        }

        if (durationText != null)
        {
            durationText.gameObject.SetActive(viewData.HasDuration);
            durationText.text = viewData.HasDuration ? viewData.RemainingDurationSeconds.ToString("0.0") : string.Empty;
        }
    }

    private Color ResolveBorderColor(BuffPolarity polarity)
    {
        return polarity switch
        {
            BuffPolarity.Positive => positiveColor,
            BuffPolarity.Negative => negativeColor,
            _ => neutralColor
        };
    }

    public string Title => viewData.Describable.Title;

    public Sprite Icon => viewData.Describable.Icon;

    public string Description => viewData.Describable.Description;

    public IEnumerable<DescriptorInfo> GetExtraInfos()
    {
        return viewData.Describable.GetExtraInfos();
    }
}
